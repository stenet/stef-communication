using System;
using System.Linq;
using System.Threading.Tasks;
using Stef.Communication.Base;

namespace Stef.Communication.DuplexImpl
{
    public class DuplexClient : ClientBase
    {
        private DuplexImpl _Impl;

        public DuplexClient(string ip = null, int? port = null) : base(ip, port)
        {
            _Impl = new DuplexImpl();
        }

        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public void RegisterMessageType<T, K>(Predicate<T> predicate, Func<T, K> action)
        {
            _Impl.RegisterMessageType(predicate, action);
        }
        public void RegisterMessageType<T, K>(Func<T, K> action)
        {
            _Impl.RegisterMessageType((obj) => obj is T, action);
        }

        public K Send<T, K>(T message, TimeSpan? timeout = null)
        {
            return SendAsync<T, K>(
                message,
                timeout: timeout)
                .Result;
        }
        public Task<K> SendAsync<T, K>(T message, TimeSpan? timeout = null)
        {
            return _Impl.Send<T, K>(SendData, message, timeout: timeout);
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            _Impl.OnDataReceived(SendData, data);

            base.OnDataReceived(session, data);
        }
    }
}
