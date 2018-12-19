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
            _Impl = new DuplexImpl(this);
        }

        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public void RegisterMessageHandler<T>(Action<Session, T> action)
        {
            _Impl.RegisterMessageHandler<T, object>(
                obj => obj is T,
                (session, v) => { action(session, v); return null; });
        }
        public void RegisterMessageHandler<T, K>(Func<Session, T, K> action)
        {
            _Impl.RegisterMessageHandler(
                obj => obj is T,
                action);
        }
        public void RegisterMessageHandler<T>(Predicate<T> predicate, Action<Session, T> action)
        {
            _Impl.RegisterMessageHandler<T, object>(
                predicate,
                (session, v) => { action(session, v); return null; });
        }
        public void RegisterMessageHandler<T, K>(Predicate<T> predicate, Func<Session, T, K> action)
        {
            _Impl.RegisterMessageHandler(
                predicate,
                action);
        }

        public void Send<T>(T message, TimeSpan? timeout = null)
        {
            SendAsync<T>(message, timeout: timeout)
                .Wait();
        }
        public K Send<T, K>(T message, TimeSpan? timeout = null)
        {
            return SendAsync<T, K>(message, timeout: timeout)
                .Result;
        }
        public void SendAndForget<T>(T message, TimeSpan? timeout = null)
        {
            _Impl.Send<T, object>(Session, message, timeout: timeout, wait: false);
        }
        public Task SendAsync<T>(T message, TimeSpan? timeout = null)
        {
            return _Impl.Send<T, object>(Session, message, timeout: timeout);
        }
        public Task<K> SendAsync<T, K>(T message, TimeSpan? timeout = null)
        {
            return _Impl.Send<T, K>(Session, message, timeout: timeout);
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            _Impl.OnDataReceived(session, data);

            base.OnDataReceived(session, data);
        }
    }
}
