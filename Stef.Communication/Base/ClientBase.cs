using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Stef.Communication.Base
{
    public abstract class ClientBase : CommunicationBase
    {
        private Session _Session;
        private bool _AutoReconnectOnError;

        public ClientBase(string ip = null, int? port = null)
            : base(ip, port)
        {
        }

        public void Connect(bool autoReconnectOnError = false)
        {
            if (_Session != null)
                throw new InvalidOperationException("Session already initialized");

            _AutoReconnectOnError = autoReconnectOnError;

            try
            {
                var tcpClient = new TcpClient(IP, Port);
                _Session = new Session(tcpClient);

                OnConnected(_Session);
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
            if (_Session == null)
                return;

            _AutoReconnectOnError = false;
            SendData(QUIT_BYTES);
            OnDisconnected(_Session);
        }

        protected void SendData(byte[] data)
        {
            if (_Session == null)
                throw new InvalidOperationException("Session not initialized");

            SendDataEx(_Session, data);
        }

        protected override void OnDisconnected(Session session)
        {
            base.OnDisconnected(session);
            _Session = null;

            if (_AutoReconnectOnError)
                TryReconnect();
        }

        private async void TryReconnect()
        {
            if (_Session != null)
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
