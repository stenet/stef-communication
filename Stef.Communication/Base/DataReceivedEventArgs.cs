using System;
using System.Linq;

namespace Stef.Communication.Base
{
    public class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(Session session, byte[] data)
        {
            Session = session;
            Data = data;
        }
        public Session Session { get; private set; }
        public byte[] Data { get; private set; }
    }
}
