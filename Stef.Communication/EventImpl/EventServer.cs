using System;
using System.Collections.Generic;
using System.Linq;
using Stef.Communication.Base;

namespace Stef.Communication.EventImpl
{
    public class EventServer : ServerBase
    {
        private object _SyncLock = new object();
        private Random _Random;
        private Dictionary<Guid, List<string>> _SessionGroupNameDic;

        public EventServer(string ip = null, int? port = null) : base(ip, port)
        {
            _Random = new Random();
            _SessionGroupNameDic = new Dictionary<Guid, List<string>>();
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            var request = SerializeManager.Current.Deserialize(data);

            if (request is EventRequest eventRequest)
            {
                ResendEvent(session, eventRequest, data);
            }
            else if (request is EventGroupNamesRequest eventGroupNamesRequest)
            {
                lock (_SyncLock)
                {
                    _SessionGroupNameDic[session.Id] = eventGroupNamesRequest.GroupNameList;
                }
            }

            base.OnDataReceived(session, data);
        }
        protected override void OnDisconnected(Session session)
        {
            base.OnDisconnected(session);
            
            lock (_SyncLock)
            {
                _SessionGroupNameDic.Remove(session.Id);
            }
        }

        private void ResendEvent(Session session, EventRequest request, byte[] data)
        {
            List<Session> sessionList;

            switch (request.RecipientType)
            {
                case EventRecipientType.All:
                    sessionList = GetAllSessions();
                    break;
                case EventRecipientType.Others:
                    sessionList = GetAllSessionNotMe(session);
                    break;
                case EventRecipientType.AllInGroup:
                    sessionList = GetAllSessionsInGroup(request);
                    break;
                case EventRecipientType.RandomInGroup:
                    sessionList = GetRandomSessionInGroup(request);
                    break;
                default:
                    throw new NotImplementedException();
            }

            sessionList
                .ForEach(c => SendData(c, data));
        }

        private List<Session> GetAllSessions()
        {
            return SessionList
                .ToList();
        }
        private List<Session> GetAllSessionNotMe(Session session)
        {
            return SessionList
                .Where(c => c != session)
                .ToList();
        }
        private List<Session> GetAllSessionsInGroup(EventRequest request)
        {
            var resultList = new List<Session>();

            foreach (var session in SessionList.ToList())
            {
                lock (_SyncLock)
                {
                    if (!_SessionGroupNameDic.ContainsKey(session.Id))
                        continue;
                    if (!_SessionGroupNameDic[session.Id].Contains(request.GroupName))
                        continue;
                }

                resultList.Add(session);
            }

            return resultList;
        }
        private List<Session> GetRandomSessionInGroup(EventRequest request)
        {
            var tempList = GetAllSessionsInGroup(request);
            if (!tempList.Any())
                return new List<Session>();

            var index = _Random.Next(tempList.Count);
            return new List<Session>() { tempList[index] };
        }
    }
}
