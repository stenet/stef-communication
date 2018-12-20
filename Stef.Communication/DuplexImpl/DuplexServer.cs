using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stef.Communication.Base;

namespace Stef.Communication.DuplexImpl
{
    public class DuplexServer : ServerBase
    {
        private DuplexImpl _Impl;

        public DuplexServer(string ip = null, int? port = null) : base(ip, port)
        {
            _Impl = new DuplexImpl(this);
        }

        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public void RegisterHandler<T>(Action<Session, T> action)
        {
            _Impl.RegisterHandler<T, object>(
                obj => obj is T, 
                (session, v) => { action(session, v); return null; });
        }
        public void RegisterHandler<T, K>(Func<Session, T, K> action)
        {
            _Impl.RegisterHandler(
                obj => obj is T, 
                action);
        }
        public void RegisterHandler<T>(Predicate<T> predicate, Action<Session, T> action)
        {
            _Impl.RegisterHandler<T, object>(
                predicate, 
                (session, v) => { action(session, v); return null; });
        }
        public void RegisterHandler<T, K>(Predicate<T> predicate, Func<Session, T, K> action)
        {
            _Impl.RegisterHandler(
                predicate, 
                action);
        }

        public void Send<T>(Session session, T message, TimeSpan? timeout = null)
        {
            var task = SendAsync<T>(session, message, timeout: timeout);

            task.Wait();

            if (task.Exception != null)
                throw task.Exception;
        }
        public K Send<T, K>(Session session, T message, TimeSpan? timeout = null)
        {
            var task = SendAsync<T, K>(session, message, timeout: timeout);

            task.Wait();

            if (task.Exception != null)
                throw task.Exception;

            return task.Result;
        }
        public void SendAndForget<T>(Session session, T message, TimeSpan? timeout = null)
        {
            _Impl.Send<T, object>(session, message, timeout: timeout, wait: false);
        }
        public Task SendAsync<T>(Session session, T message, TimeSpan? timeout = null)
        {
            return _Impl.Send<T, object>(session, message, timeout: timeout);
        }
        public Task<K> SendAsync<T, K>(Session session, T message, TimeSpan? timeout = null)
        {
            return _Impl.Send<T, K>(session, message, timeout: timeout);
        }

        public void Send<T>(T message, TimeSpan? timeout = null)
        {
            var task = SendAsync<T>(message, timeout: timeout);

            task.Wait();

            if (task.Exception != null)
                throw task.Exception;
        }
        public void SendAndForget<T>(T message, TimeSpan? timeout = null)
        {
            var sessionList = SessionList
                .ToList();

            foreach (var session in sessionList)
            {
                _Impl.Send<T, object>(session, message, timeout: timeout, wait: false);
            }
        }
        public Task SendAsync<T>(T message, TimeSpan? timeout = null)
        {
            var sessionList = SessionList
                .ToList();

            var taskList = new List<Task>();
            foreach (var session in sessionList)
            {
                var task = _Impl.Send<T, object>(session, message, timeout: timeout);
                taskList.Add(task);
            }

            return Task.WhenAll(taskList);
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            _Impl.OnDataReceived(session, data);
            base.OnDataReceived(session, data);
        }
    }
}
