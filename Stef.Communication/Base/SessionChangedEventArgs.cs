using System;
using System.Linq;

namespace Stef.Communication.Base
{
    public class SessionChangedEventArgs : EventArgs
    {
        public SessionChangedEventArgs(Session session)
        {
            Session = session;
        }

        public Session Session { get; private set; }
    }
}
