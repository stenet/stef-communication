using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexImpl
    {
        public const string RequestMessageId = "__RequestMessageId";
        public const string ResponseMessageId = "__ResponseMessageId";

        private object _Sync = new object();
        private Dictionary<Guid, DuplexWaitItem> _WaitDic;
        private Dictionary<string, object> _InstanceDic;

        public DuplexImpl()
        {
            _WaitDic = new Dictionary<Guid, DuplexWaitItem>();
            _InstanceDic = new Dictionary<string, object>();
        }

        public Func<string, Type> MessageTypeResolverFunc { get; set; }

        public Task<K> SendDuplex<T, K>(Action<byte[]> sendData, string methodName, object parameter = null, TimeSpan? timeout = null)
            where T : class
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new DuplexRequest()
            {
                MessageId = Guid.NewGuid(),
                TypeName = typeof(T).FullName,
                MethodName = methodName,
                Parameter = parameter == null
                    ? null
                    : JsonConvert.SerializeObject(parameter)
            };

            var json = JsonConvert.SerializeObject(request);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            var waitItem = new DuplexWaitItem<K>(request.MessageId);

            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            sendData(jsonBytes);

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
            var json = Encoding.UTF8.GetString(data);

            var isRequest = json.Contains(RequestMessageId);

            if (isRequest)
                CreateResponse(sendData, json);
            else
                ResolveResponse(json);
        }
        private void CreateResponse(Action<byte[]> sendData, string json)
        {
            var message = JsonConvert.DeserializeObject<DuplexRequest>(json);

            try
            {
                var instance = GetMethodInstance(message.TypeName);
                if (instance == null)
                {
                    SendUnknownTypeResponse(sendData, message);
                    return;
                }

                var method = instance.GetType().GetMethod(message.MethodName);
                var parameter = new object[0];
                if (!string.IsNullOrEmpty(message.Parameter))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                    {
                        SendParameterMismatch(sendData, message);
                        return;
                    }

                    var parameterType = parameters[0].ParameterType;
                    var parameterData = JsonConvert.DeserializeObject(message.Parameter, parameterType);
                    parameter = new object[1] { parameterData };
                }
                else
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length > 0)
                    {
                        SendParameterMismatch(sendData, message);
                        return;
                    }
                }

                var result = method.Invoke(instance, parameter);

                var response = new DuplexResponse()
                {
                    MessageId = message.MessageId,
                    ResponseType = ResponseType.OK,
                    Result = JsonConvert.SerializeObject(result)
                };

                SendResponse(sendData, response);
            }
            catch (Exception ex)
            {
                SendExceptionResponse(sendData, message, ex);
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
        private void SendParameterMismatch(Action<byte[]> sendData, DuplexRequest message)
        {
            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.ParameterMismatch
            };

            SendResponse(sendData, response);
        }
        private void SendExceptionResponse(Action<byte[]> sendData, DuplexRequest message, Exception ex)
        {
            var response = new DuplexResponse()
            {
                MessageId = message.MessageId,
                ResponseType = ResponseType.Exception,
                Result = ex.Message
            };

            SendResponse(sendData, response);
        }
        private void SendResponse(Action<byte[]> sendData, DuplexResponse response)
        {
            var responseJson = JsonConvert.SerializeObject(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            sendData(responseBytes);
        }
        private object GetMethodInstance(string typeName)
        {
            object instance;

            if (!_InstanceDic.TryGetValue(typeName, out instance))
            {
                lock (_Sync)
                {
                    if (!_InstanceDic.TryGetValue(typeName, out instance))
                    {
                        var type = MessageTypeResolverFunc == null
                            ? Type.GetType(typeName)
                            : MessageTypeResolverFunc(typeName);

                        instance = type == null
                            ? null
                            : Activator.CreateInstance(type);
                        _InstanceDic.Add(typeName, instance);
                    }
                }
            }

            return instance;
        }
        private void ResolveResponse(string json)
        {
            var response = JsonConvert.DeserializeObject<DuplexResponse>(json);

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
                    waitItem.SetException(new ApplicationException(response.Result));
                    break;
                case ResponseType.ParameterMismatch:
                    waitItem.SetException(new ApplicationException("parameter mismatch"));
                    break;
            }
        }
    }
}
