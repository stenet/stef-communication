using System;

namespace Stef.Communication.FileImpl
{
    public class SaveFileEventArgs : EventArgs
    {
        public SaveFileEventArgs(string key, byte[] data)
        {
            Key = key;
            Data = data;
        }

        public string Key { get; private set; }
        public byte[] Data { get; set; }
    }
}
