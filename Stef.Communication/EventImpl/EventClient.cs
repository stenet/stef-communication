using System;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.EventImpl
{
    public class EventClient : ClientBase
    {
        public event EventHandler<PublishEventEventArgs> PublishEvent;

        public void SendEvent<T>(T args)
            where T : class
        {
            var data = new EventRequest()
            {
                Data = SerializeManager.Current.Serialize(args)
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            PublishEventData(data);
            base.OnDataReceived(session, data);
        }

        private void PublishEventData(byte[] data)
        {
            var eventRequest = (EventRequest)SerializeManager.Current.Deserialize(data);
            var args = SerializeManager.Current.Deserialize(eventRequest.Data);

            PublishEvent?.Invoke(this, new PublishEventEventArgs(args));
        }
    }
}
