using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileRequestResponseBase
    {
        public FileRequestResponseBase()
        {
        }

        public Guid MessageId { get; set; }
        public ResponseType ResponseType { get; set; }
        public string Exception { get; set; }
    }
}
