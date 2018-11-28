using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Stef.Communication.Base;

namespace Stef.Communication.EventImpl
{
    public class EventClient : ClientBase
    {
        public void SendEvent<T>(T args)
            where T : class
        {
            var nameBytes = Encoding.UTF8.GetBytes(typeof(T).FullName);
            var json = JsonConvert.SerializeObject(args);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var stream = new MemoryStream())
            {
                var nameBytesLength = BitConverter.GetBytes(nameBytes.Length);
                stream.Write(nameBytesLength, 0, nameBytesLength.Length);
                stream.Write(nameBytes, 0, nameBytes.Length);

                var jsonBytesLength = BitConverter.GetBytes(jsonBytes.Length);
                stream.Write(jsonBytesLength, 0, jsonBytesLength.Length);
                stream.Write(jsonBytes, 0, jsonBytes.Length);

                SendData(stream.ToArray());
            }
        }

        public event EventHandler<PublishEventEventArgs> PublishEvent;

        public Func<string, Type> TypeResolverFunc { get; set; }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            PublishEventData(data);
            base.OnDataReceived(session, data);
        }

        private void PublishEventData(byte[] data)
        {
            var nameLength = BitConverter.ToInt32(data, 0);
            var name = Encoding.UTF8.GetString(data, 4, nameLength);
            var jsonLength = BitConverter.ToInt32(data, nameLength + 4);
            var json = Encoding.UTF8.GetString(data, nameLength + 8, jsonLength);

            var type = TypeResolverFunc == null
                ? Type.GetType(name)
                : TypeResolverFunc(name);

            if (type == null)
                return;

            var eventData = JsonConvert.DeserializeObject(json, type);
            PublishEvent?.Invoke(this, new PublishEventEventArgs(eventData));
        }
    }
}
