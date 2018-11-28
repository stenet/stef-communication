using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stef.Communication.DuplexImpl
{
    internal abstract class DuplexWaitItem
    {
        public DuplexWaitItem(Guid messageId)
        {
            MessageId = messageId;
        }

        public Guid MessageId { get; private set; }
        public Task CancelTask { get; set; }

        public abstract void SetResult(string val);
        public abstract void SetException(Exception ex);
    }

    internal class DuplexWaitItem<K> : DuplexWaitItem
    {
        public DuplexWaitItem(Guid messageId) : base(messageId)
        {
            CompletionSource = new TaskCompletionSource<K>();
        }

        public TaskCompletionSource<K> CompletionSource { get; private set; }

        public override void SetResult(string val)
        {
            var obj = JsonConvert.DeserializeObject<K>(val);
            CompletionSource.TrySetResult(obj);
        }
        public override void SetException(Exception ex)
        {
            CompletionSource.TrySetException(ex);
        }
    }
}
