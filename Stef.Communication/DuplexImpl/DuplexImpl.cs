using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexImpl
    {
        private object _Sync = new object();
        private Dictionary<Guid, DuplexWaitItem> _WaitDic;
        private Dictionary<string, object> _InstanceDic;

        private List<DuplexMessageResolver> _MessageTypeResolveList;

        public DuplexImpl()
        {
            _WaitDic = new Dictionary<Guid, DuplexWaitItem>();
            _InstanceDic = new Dictionary<string, object>();
            _MessageTypeResolveList = new List<DuplexMessageResolver>();
        }

        public void RegisterMessageType<T, K>(Predicate<T> predicate, Func<T, K> action)
        {
            Func<object, Action<object>, bool> func = (obj, responseAction) =>
            {
                if (!(obj is T))
                    return false;

                var t = (T)obj;

                if (!predicate(t))
                    return false;

                var result = action(t);
                responseAction(result);
                return true;
            };

            _MessageTypeResolveList.Add(new DuplexMessageResolver(func));
        }

        public Func<string, Type> MessageTypeResolverFunc { get; set; }

        public Task<K> Send<T, K>(Action<byte[]> sendData, T message, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new DuplexRequest()
            {
                MessageId = Guid.NewGuid(),
                Message = SerializeManager.Current.Serialize(message)
            };

            var bytes = SerializeManager.Current.Serialize(request);

            var waitItem = new DuplexWaitItem<K>(request.MessageId);

            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            sendData(bytes);

            return waitItem.CompletionSource.Task;
        }
        private void AttachCompletionToWaitItem<K>(DuplexWaitItem<K> waitItem, TimeSpan timeout)
        {
            var cancelTokenSource = new CancellationTokenSource();

            Action clear = () =>
            {
                lock (_Sync)
                {
                    _WaitDic.Remove(waitItem.MessageId);
                }

                cancelTokenSource.Cancel();
                waitItem.CancelTask = null;
            };

            waitItem.CancelTask = Task
                .Delay(timeout, cancelTokenSource.Token)
                .ContinueWith(t =>
            {
                if (t.IsCanceled)
                    return;

                waitItem.CompletionSource?.TrySetException(new TimeoutException());
                clear();
            });

            waitItem.CompletionSource.Task.ContinueWith(t =>
            {
                var ex = t.Exception;
                //TODO Handle Exception

                clear();
            });
        }

        public void OnDataReceived(Action<byte[]> sendData, byte[] data)
        {
            var received = SerializeManager.Current.Deserialize(data);

            if (received is DuplexRequest request)
            {
                CreateResponse(sendData, request);
            }
            else if (received is DuplexResponse response)
            {
                ResolveResponse(response);
            }
            else
            {
                //TODO Exception
            }
        }
        private void CreateResponse(Action<byte[]> sendData, DuplexRequest request)
        {
            try
            {
                var message = SerializeManager.Current.Deserialize(request.Message);

                Action<object> sendResponse = (obj) =>
                {
                    var response = new DuplexResponse()
                    {
                        MessageId = request.MessageId,
                        ResponseType = ResponseType.OK,
                        Result = SerializeManager.Current.Serialize(obj)
                    };

                    SendResponse(sendData, response);
                };

                var isHandled = _MessageTypeResolveList
                    .Any(c => c.Handle(message, sendResponse));

                if (isHandled)
                    return;

                SendUnknownTypeResponse(sendData, request);
            }
            catch (Exception ex)
            {
                SendExceptionResponse(sendData, request, ex);
            }
        }
        private void SendUnknownTypeResponse(Action<byte[]> sendData, DuplexRequest message)
        {
            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.UnknownMethodType
            };

            SendResponse(sendData, response);
        }
        private void SendExceptionResponse(Action<byte[]> sendData, DuplexRequest message, Exception ex)
        {
            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.Exception,
                Result = SerializeManager.Current.Serialize(ex.Message)
            };

            SendResponse(sendData, response);
        }
        private void SendResponse(Action<byte[]> sendData, DuplexResponse response)
        {
            var bytes = SerializeManager.Current.Serialize(response);
            sendData(bytes);
        }
        private void ResolveResponse(DuplexResponse response)
        {
            DuplexWaitItem waitItem;
            lock (_Sync)
            {
                if (!_WaitDic.TryGetValue(response.MessageId, out waitItem))
                    return;
            }

            switch (response.ResponseType)
            {
                case ResponseType.OK:
                    waitItem.SetResult(response.Result);
                    break;
                case ResponseType.UnknownMethodType:
                    waitItem.SetException(new ApplicationException("unknown message type"));
                    break;
                case ResponseType.Exception:
                    waitItem.SetException(new ApplicationException(SerializeManager.Current.Deserialize(response.Result)?.ToString()));
                    break;
            }
        }
    }
}
