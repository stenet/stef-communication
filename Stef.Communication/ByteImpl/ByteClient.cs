using System;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.ByteImpl
{
    public class ByteClient : ClientBase
    {
        public ByteClient(string ip = null, int? port = null) : base(ip, port)
        {
        }

        public new void SendData(byte[] data)
        {
            base.SendData(data);
        }
    }
}
