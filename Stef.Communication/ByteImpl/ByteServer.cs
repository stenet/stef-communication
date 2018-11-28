using System;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.ByteImpl
{
    public class ByteServer : ServerBase
    {
        public ByteServer(string ip = null, int? port = null) : base(ip, port)
        {
        }

        public new void SendData(byte[] data)
        {
            base.SendData(data);
        }
        public new void SendData(Session session, byte[] data)
        {
            base.SendData(session, data);
        }
    }
}
