using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Stef.Communication.Base
{
    public abstract class ClientBase : CommunicationBase
    {
        private bool _AutoReconnectOnError;

        public ClientBase(string ip = null, int? port = null)
            : base(ip, port)
        {
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
                Session = new Session(tcpClient);

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

        protected void SendData(byte[] data)
        {
            if (Session == null)
                throw new InvalidOperationException("Session not initialized");

            SendDataEx(Session, data);
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

            await Task.Delay(750);

            try
            {
                Connect(true);
            }
            catch (Exception)
            {
            }
        }
    }
}
