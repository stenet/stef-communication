using System;
using System.Linq;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexResponse
    {
        public Guid MessageId { get; set; }
        public ResponseType ResponseType { get; set; }
        public byte[] Result { get; set; }
    }
}
