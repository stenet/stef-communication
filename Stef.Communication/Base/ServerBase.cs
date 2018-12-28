using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Stef.Communication.Base
{
    public abstract class ServerBase : CommunicationBase
    {
        private object _SyncLock = new object();
        private TcpListener _Listener;
        private List<Session> _SessionList;

        public ServerBase(string ip = null, int? port = null)
            : base(ip, port)
        {
            _SessionList = new List<Session>();
        }

        public IEnumerable<Session> SessionList
        {
            get
            {
                lock (_SyncLock)
                {
                    return _SessionList
                        .ToList();
                }
            }
        }

        public void Start()
        {
            if (_Listener != null)
                throw new InvalidOperationException("Server already initialized");

            _Listener = new TcpListener(IPAddress.Parse(IP), Port);
            _Listener.Start();

            AcceptClients();
        }
        public void Stop()
        {
            if (_Listener == null)
                return;

            SendData(QUIT_BYTES);
            CloseClients();

            _Listener.Stop();
            _Listener = null;
        }

        protected virtual Session CreateSession(TcpClient client)
        {
            return new Session(client);
        }

        protected void SendData(byte[] data)
        {
            foreach (var session in SessionList.ToList())
            {
                SendData(session, data);
            }
        }
        protected void SendData(Session session, byte[] data)
        {
            SendDataEx(session, data);
        }

        protected override void OnConnected(Session session)
        {
            lock (_SyncLock)
            {

                _SessionList.Add(session);
            }

            base.OnConnected(session);
        }
        protected override void OnDisconnected(Session session)
        {
            lock (_SyncLock)
            {
                _SessionList.Remove(session);
            }

            base.OnDisconnected(session);
        }

        private async void AcceptClients()
        {
            while (_Listener != null)
            {
                try
                {
                    var client = await _Listener.AcceptTcpClientAsync();
                    var session = CreateSession(client);

                    OnConnected(session);
                }
                catch (ObjectDisposedException) //= shutdown
                {
                    return;
                }
            }
        }
        private void CloseClients()
        {
            foreach (var session in SessionList.ToList())
            {
                OnDisconnected(session);
                session.Dispose();
            }
        }
    }
}
