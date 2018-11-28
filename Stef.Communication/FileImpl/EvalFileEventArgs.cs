using System;

namespace Stef.Communication.FileImpl
{
    public class EvalFileEventArgs : EventArgs
    {
        public EvalFileEventArgs(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
        public byte[] Data { get; set; }
    }
}
