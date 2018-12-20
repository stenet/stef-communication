using Stef.Communication.Base;
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
        private readonly CommunicationBase _CommunicationBase;
        private Dictionary<Guid, DuplexWaitItem> _WaitDic;
        private Dictionary<string, object> _InstanceDic;

        private List<DuplexHandler> _HandlerList;

        public DuplexImpl(CommunicationBase communicationBase)
        {
            _WaitDic = new Dictionary<Guid, DuplexWaitItem>();
            _InstanceDic = new Dictionary<string, object>();
            _HandlerList = new List<DuplexHandler>();

            _CommunicationBase = communicationBase;
        }

        public void RegisterHandler<T, K>(Predicate<T> predicate, Func<Session, T, K> action)
        {
            Func<Session, object, Action<object>, bool> func = (session, obj, responseAction) =>
            {
                if (!(obj is T))
                    return false;

                var t = (T)obj;

                if (!predicate(t))
                    return false;

                var result = action(session, t);
                responseAction(result);
                return true;
            };

            _HandlerList.Add(new DuplexHandler(func));
        }

        public Func<string, Type> MessageTypeResolverFunc { get; set; }

        public Task<K> Send<T, K>(Session session, T message, TimeSpan? timeout = null, bool wait = true)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new DuplexRequest()
            {
                MessageId = Guid.NewGuid(),
                Message = SerializeManager.Current.Serialize(message)
            };

            var bytes = SerializeManager.Current.Serialize(request);

            if (wait)
            {
                var waitItem = new DuplexWaitItem<K>(request.MessageId);

                lock (_Sync)
                {
                    _WaitDic.Add(waitItem.MessageId, waitItem);
                }

                AttachCompletionToWaitItem(session, waitItem, timeout.Value);
                _CommunicationBase.SendDataEx(session, bytes);

                return waitItem.CompletionSource.Task;
            }
            else
            {
                _CommunicationBase.SendDataEx(session, bytes);
                return null;
            }
        }
        private void AttachCompletionToWaitItem<K>(Session session, DuplexWaitItem<K> waitItem, TimeSpan timeout)
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
                _CommunicationBase.OnException(session, ex, disconnect: false);

                clear();
            });
        }

        public void OnDataReceived(Session session, byte[] data)
        {
            var received = SerializeManager.Current.Deserialize(data);

            if (received is DuplexRequest request)
            {
                CreateResponse(session, request);
            }
            else if (received is DuplexResponse response)
            {
                ResolveResponse(response);
            }
        }

        private void CreateResponse(Session session, DuplexRequest request)
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

                    SendResponse(session, response);
                };

                var isHandled = _HandlerList
                    .Any(c => c.Handle(session, message, sendResponse));

                if (isHandled)
                    return;

                SendUnknownTypeResponse(session, request);
            }
            catch (Exception ex)
            {
                SendExceptionResponse(session, request, ex);
            }
        }
        private void SendUnknownTypeResponse(Session session, DuplexRequest message)
        {
            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.UnknownMethodType
            };

            SendResponse(session, response);
        }
        private void SendExceptionResponse(Session session, DuplexRequest message, Exception ex)
        {
            _CommunicationBase.OnException(session, ex, disconnect: false);

            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.Exception,
                Exception = ex.Message
            };

            SendResponse(session, response);
        }
        private void SendResponse(Session session, DuplexResponse response)
        {
            var bytes = SerializeManager.Current.Serialize(response);
            _CommunicationBase.SendDataEx(session, bytes);
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
                    waitItem.SetException(new ApplicationException(response.Exception));
                    break;
            }
        }
    }
}
