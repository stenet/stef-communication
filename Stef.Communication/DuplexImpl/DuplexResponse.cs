using System;
using System.Linq;
using Newtonsoft.Json;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexResponse
    {
        [JsonProperty(DuplexImpl.ResponseMessageId)]
        public Guid MessageId { get; set; }
        public ResponseType ResponseType { get; set; }
        public string Result { get; set; }
    }
}
