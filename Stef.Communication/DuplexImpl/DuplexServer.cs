using System;
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
            _Impl = new DuplexImpl();
        }

        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public Func<string, Type> MessageTypeResolverFunc
        {
            get
            {
                return _Impl.MessageTypeResolverFunc;
            }
            set
            {

                _Impl.MessageTypeResolverFunc = value;
            }
        }

        public K SendDuplex<T, K>(string methodName, object parameter = null, TimeSpan? timeout = null)
            where T : class
        {
            return SendDuplexAsync<T, K>(
                methodName,
                parameter: parameter,
                timeout: timeout)
                .Result;
        }
        public Task<K> SendDuplexAsync<T, K>(string methodName, object parameter = null, TimeSpan? timeout = null)
            where T : class
        {
            return _Impl.SendDuplex<T, K>(SendData, methodName, parameter: parameter, timeout: timeout);
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            _Impl.OnDataReceived(response => SendData(session, response), data);

            base.OnDataReceived(session, data);
        }
    }
}
