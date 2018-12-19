using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileSaveRequest : FileRequestResponseBase
    {
        public FileSaveRequest()
        {
        }

        public string Key { get; set; }
        public byte[] Data { get; set; }
    }
}
