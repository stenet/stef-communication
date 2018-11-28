using System;
using System.Linq;

namespace Stef.Communication.DuplexImpl
{
    public enum ResponseType
    {
        OK = 0,
        UnknownMethodType = 1,
        Exception = 2,
        ParameterMismatch = 3
    }
}
