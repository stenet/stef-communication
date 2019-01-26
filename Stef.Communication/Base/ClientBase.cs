using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Stef.Communication.Exceptions;

namespace Stef.Communication.Base
{
    public abstract class ClientBase : CommunicationBase
    {
        private bool _AutoReconnectOnError;
        private object _TryReconnectLock = new object();
        private bool _IsTryingReconnect;
        private object _CreateSessionLock = new object();

        public ClientBase(string ip = null, int? port = null)
            : base(ip, port)
        {
        }

        public bool IsConnected
        {
            get
            {
                return Session != null
                    && Session.IsConnected;
            }
        }
        public Session Session { get; private set; }

        public void Connect(bool autoReconnectOnError = false)
        {
            if (Session != null)
                throw new InvalidOperationException("Session already initialized");

            _AutoReconnectOnError = autoReconnectOnError;

            try
            {
                var tcpClient = new TcpClient(IP, Port);

                lock (_CreateSessionLock)
                {
                    if (Session != null)
                    {
                        try
                        {
                            tcpClient.Dispose();
                            return;
                        }
                        catch {}
                    }

                    Session = new Session(tcpClient);
                }

                OnConnected(Session);
            }
            catch (Exception)
            {
                if (_AutoReconnectOnError)
                {
                    TryReconnect();
                }
                else
                {
                    throw;
                }
            }
        }
        public void Disconnect()
        {
            if (Session == null)
                return;

            _AutoReconnectOnError = false;
            SendData(QUIT_BYTES);
            OnDisconnected(Session);
        }

        protected void SendData(byte[] data, bool throwInvalidSessionException = true)
        {
            try
            {
                SendDataInternal(Session, data);
            }
            catch (InvalidSessionException)
            {
                if (throwInvalidSessionException)
                    throw;
            }
        }
        protected override void OnDisconnected(Session session)
        {
            base.OnDisconnected(session);
            Session = null;

            if (_AutoReconnectOnError)
                TryReconnect();
        }

        private async void TryReconnect()
        {
            if (Session != null)
                return;

            lock (_TryReconnectLock)
            {
                if (_IsTryingReconnect)
                    return;

                _IsTryingReconnect = true;
            }

            await Task.Delay(750);

            try
            {
                lock (_TryReconnectLock)
                {
                    _IsTryingReconnect = false;
                }

                Connect(true);
            }
            catch (Exception)
            {
            }
        }
    }
}
