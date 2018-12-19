using System;

namespace Stef.Communication.FileImpl
{
    public class InitEventArgs : EventArgs
    {
        public InitEventArgs(object data)
        {
            Data = data;
        }

        public object Data { get; private set; }
    }
}
