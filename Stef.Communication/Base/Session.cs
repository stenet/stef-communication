using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace Stef.Communication.Base
{
    public class Session : IDisposable
    {
        public Session(TcpClient client)
        {
            Id = Guid.NewGuid();
            TcpClient = client;
            Stream = client.GetStream();
        }

        public Guid Id { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public Stream Stream { get; private set; }

        public void Dispose()
        {
            if (TcpClient == null)
                return;

            TcpClient.Close();
            TcpClient.Dispose();
            TcpClient = null;

            Stream = null;
        }
    }
}
