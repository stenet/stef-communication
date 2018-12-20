using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.EventImpl
{
    public class EventClient : ClientBase
    {
        private List<string> _GroupNameList;

        public EventClient(string ip = null, int? port = null) 
            : base(ip, port)
        {
            _GroupNameList = new List<string>();
        }

        public event EventHandler<PublishEventEventArgs> PublishEvent;

        public void AddToGroup(params string[] groupNameArr)
        {
            foreach (var groupName in groupNameArr)
            {
                if (string.IsNullOrEmpty(groupName))
                    continue;
                if (_GroupNameList.Contains(groupName))
                    continue;

                _GroupNameList.Add(groupName);
            }

            if (IsConnected)
                UpdateGroupNames();
        }

        public void SendEventToAll<T>(T args)
            where T : class
        {
            var data = new EventRequest()
            {
                Data = SerializeManager.Current.Serialize(args),
                RecipientType = EventRecipientType.All
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }
        public void SendEventToOthers<T>(T args)
            where T : class
        {
            var data = new EventRequest()
            {
                Data = SerializeManager.Current.Serialize(args),
                RecipientType = EventRecipientType.Others
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }
        public void SendEventToAllInGroup<T>(T args, string groupName)
            where T : class
        {
            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentException($"{nameof(groupName)} is null or empty.", nameof(groupName));

            var data = new EventRequest()
            {
                Data = SerializeManager.Current.Serialize(args),
                RecipientType = EventRecipientType.AllInGroup,
                GroupName = groupName
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }
        public void SendEventToRandomInGroup<T>(T args, string groupName)
            where T : class
        {
            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentException($"{nameof(groupName)} is null or empty.", nameof(groupName));

            var data = new EventRequest()
            {
                Data = SerializeManager.Current.Serialize(args),
                RecipientType = EventRecipientType.RandomInGroup,
                GroupName = groupName
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }

        protected override void OnConnected(Session session)
        {
            base.OnConnected(session);

            if (_GroupNameList.Any())
                UpdateGroupNames();
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
        private void UpdateGroupNames()
        {
            var data = new EventGroupNamesRequest()
            {
                GroupNameList = _GroupNameList
            };

            var bytes = SerializeManager.Current.Serialize(data);
            SendData(bytes);
        }
    }
}
