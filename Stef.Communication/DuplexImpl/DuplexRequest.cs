using System;
using System.Linq;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexRequest
    {
        public Guid MessageId { get; set; }
        public byte[] Message { get; set; }
    }
}
