using System;

namespace Stef.Communication.FileImpl
{
    public class DeleteFileEventArgs : EventArgs
    {
        public DeleteFileEventArgs(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
    }
}
