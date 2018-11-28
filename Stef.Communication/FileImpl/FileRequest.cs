using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileRequest
    {
        public FileRequest()
        {
        }

        public Guid MessageId { get; set; }
        public string Key { get; set; }
        public int Length { get; set; }
    }
}
