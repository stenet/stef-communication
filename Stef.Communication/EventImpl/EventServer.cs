using System;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.EventImpl
{
    public class EventServer : ServerBase
    {
        public EventServer(string ip = null, int? port = null) : base(ip, port)
        {
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            //TODO - Erweitern um wer die Information bekommen soll
            //zB ein spezieller Service oder einfach alle
            SessionList
                .Where(c => c != session)
                .ToList()
                .ForEach(c => SendData(c, data));

            base.OnDataReceived(session, data);
        }
    }
}
