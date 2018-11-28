using System;
using System.Linq;
using System.Threading.Tasks;

namespace Stef.Communication.FileImpl
{
    internal class FileWaitItem
    {
        public FileWaitItem(Guid messageId)
        {
            CompletionSource = new TaskCompletionSource<byte[]>();
            MessageId = messageId;
        }

        public TaskCompletionSource<byte[]> CompletionSource { get; private set; }

        public Guid MessageId { get; private set; }
        public Task CancelTask { get; set; }

        public void SetResult(byte[] fileData)
        {
            CompletionSource.TrySetResult(fileData);
        }
        public void SetException(Exception ex)
        {
            CompletionSource.TrySetException(ex);
        }
    }
}
