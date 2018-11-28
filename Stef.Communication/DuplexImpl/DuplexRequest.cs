using System;
using System.Linq;
using Newtonsoft.Json;

namespace Stef.Communication.DuplexImpl
{
    internal class DuplexRequest
    {
        [JsonProperty(DuplexImpl.RequestMessageId)]
        public Guid MessageId { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string Parameter { get; set; }
    }
}
