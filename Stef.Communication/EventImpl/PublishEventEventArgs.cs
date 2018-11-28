using System;
using System.Linq;

namespace Stef.Communication.EventImpl
{
    public class PublishEventEventArgs : EventArgs
    {
        public PublishEventEventArgs(object arguments)
        {
            Arguments = arguments;
        }

        public object Arguments { get; private set; }
    }
}
