using System;
using System.Threading.Tasks;
using Stef.Communication.DuplexImpl;

namespace Stef.Communication.Test
{
    public class SampleDuplexClient : DuplexClientBase
    {
        public SampleDuplexClient(string ip = null, int? port = null) : base(ip, port)
        {
        }

        public Task<int> HowMany(int x)
        {
            return SendDuplexAsync<FuncClass, int>(nameof(FuncClass.HowMany), parameter: x);
        }
        public Task<string> TellMe(CustomEventData data)
        {
            return SendDuplexAsync<FuncClass, string>(nameof(FuncClass.TellMe), parameter: data);
        }
        public Task<CustomEventData> TellMe2(CustomEventData data)
        {
            return SendDuplexAsync<FuncClass, CustomEventData>(nameof(FuncClass.TellMe2), parameter: data);
        }
    }
}
