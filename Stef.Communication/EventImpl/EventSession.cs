using Stef.Communication.Base;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Stef.Communication.EventImpl
{
    public class EventSession : Session
    {
        public EventSession(TcpClient client) : base(client)
        {
        }

        public Guid Id { get; internal set; }
    }
}
