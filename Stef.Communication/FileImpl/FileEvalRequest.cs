using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileEvalRequest : FileRequestResponseBase
    {
        public FileEvalRequest()
        {
        }

        public string Key { get; set; }
    }
}
