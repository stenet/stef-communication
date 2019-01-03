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

        public new void SendData(byte[] data, bool throwInvalidSessionException = true)
        {
            base.SendData(data, throwInvalidSessionException);
        }
        public new void SendData(Session session, byte[] data, bool throwInvalidSessionException = true)
        {
            base.SendData(session, data, throwInvalidSessionException);
        }
    }
}
