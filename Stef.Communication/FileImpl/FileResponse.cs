using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileResponse
    {
        public Guid MessageId { get; set; }
        public bool HasData { get; set; }
    }
}
