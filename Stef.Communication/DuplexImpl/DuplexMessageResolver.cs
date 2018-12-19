using System;
using System.Linq;

namespace Stef.Communication.DuplexImpl
{
    public class DuplexMessageResolver
    {
        public DuplexMessageResolver(Func<object, Action<object>, bool> handle)
        {
            Handle = handle;
        }

        public Func<object, Action<object>, bool> Handle { get; private set; }
    }
}
