using System;
using System.Linq;

namespace Stef.Communication.FileImpl
{
    internal class FileDeleteRequest : FileRequestResponseBase
    {
        public FileDeleteRequest()
        {
        }

        public string Key { get; set; }
    }
}
