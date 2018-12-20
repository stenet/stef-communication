using System;
using System.Collections.Generic;
using System.Text;

namespace Stef.Communication.EventImpl
{
    public class EventRequest
    {
        public EventRequest()
        {
        }

        public EventRecipientType RecipientType { get; set; }
        public string GroupName { get; set; }
        public byte[] Data { get; set; }
    }
}
