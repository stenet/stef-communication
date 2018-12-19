using System;
using System.Linq;
using System.Threading.Tasks;

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

        public abstract void SetResult(byte[] data);
        public abstract void SetException(Exception ex);
    }

    internal class DuplexWaitItem<K> : DuplexWaitItem
    {
        public DuplexWaitItem(Guid messageId) : base(messageId)
        {
            CompletionSource = new TaskCompletionSource<K>();
        }

        public TaskCompletionSource<K> CompletionSource { get; private set; }

        public override void SetResult(byte[] data)
        {
            var obj = (K)SerializeManager.Current.Deserialize(data);
            CompletionSource.TrySetResult(obj);
        }
        public override void SetException(Exception ex)
        {
            CompletionSource.TrySetException(ex);
        }
    }
}
