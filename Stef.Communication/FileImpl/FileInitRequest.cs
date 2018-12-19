using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileInitRequest : FileRequestResponseBase
    {
        public FileInitRequest()
        {
        }

        public object Data { get; set; }
    }
}
