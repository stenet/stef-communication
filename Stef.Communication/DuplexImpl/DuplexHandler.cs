﻿using Stef.Communication.Base;
using System;
using System.Linq;

namespace Stef.Communication.DuplexImpl
{
    public class DuplexHandler
    {
        public DuplexHandler(Func<Session, object, Action<object>, bool> handle)
        {
            Handle = handle;
        }

        public Func<Session, object, Action<object>, bool> Handle { get; private set; }
    }
}
