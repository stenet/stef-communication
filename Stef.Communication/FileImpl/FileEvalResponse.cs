using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileEvalResponse : FileRequestResponseBase
    {
        public byte[] Data { get; set; }
    }
}
