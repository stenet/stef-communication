using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stef.Communication.Base;

namespace Stef.Communication.FileImpl
{
    public class FileClient : ClientBase
    {
        private object _Sync = new object();
        private Dictionary<Guid, FileWaitItem> _WaitDic;

        public FileClient(string ip = null, int? port = null) : base(ip, port)
        {
            _WaitDic = new Dictionary<Guid, FileWaitItem>();
        }

        public void Init(object data, TimeSpan? timeout = null)
        {
            InitAsync(
                data,
                timeout: timeout)
                .Wait();
        }
        public Task InitAsync(object data, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new FileInitRequest()
            {
                MessageId = Guid.NewGuid(),
                Data = data
            };

            var bytes = SerializeManager.Current.Serialize(request);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            SendData(bytes);

            return waitItem.CompletionSource.Task;
        }

        public void DeleteFile(string key, TimeSpan? timeout = null)
        {
            DeleteFileAsync(
                key,
                timeout: timeout)
                .Wait();
        }
        public Task DeleteFileAsync(string key, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new FileDeleteRequest()
            {
                MessageId = Guid.NewGuid(),
                Key = key
            };

            var bytes = SerializeManager.Current.Serialize(request);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            SendData(bytes);

            return waitItem.CompletionSource.Task;
        }

        public byte[] GetFile(string key, TimeSpan? timeout = null)
        {
            return GetFileAsync(
                key,
                timeout: timeout)
                .Result;
        }
        public Task<byte[]> GetFileAsync(string key, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new FileEvalRequest()
            {
                MessageId = Guid.NewGuid(),
                Key = key
            };

            var bytes = SerializeManager.Current.Serialize(request);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            SendData(bytes);

            return waitItem.CompletionSource.Task;
        }

        public void SaveFile(string key, byte[] data, TimeSpan? timeout = null)
        {
            var task = SaveFileAsync(
                key,
                data,
                timeout: timeout);

            task.Wait();
        }
        public Task SaveFileAsync(string key, byte[] data, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(30);

            var request = new FileSaveRequest()
            {
                MessageId = Guid.NewGuid(),
                Key = key,
                Data = data
            };

            var bytes = SerializeManager.Current.Serialize(request);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);
            SendData(bytes);

            return waitItem.CompletionSource.Task;
        }

        protected override void OnDataReceived(Session session, byte[] data)
        {
            ResolveResponse(data);
            base.OnDataReceived(session, data);
        }

        private void AttachCompletionToWaitItem(FileWaitItem waitItem, TimeSpan timeout)
        {
            var cancelTokenSource = new CancellationTokenSource();

            Action clear = () =>
            {
                lock (_Sync)
                {
                    _WaitDic.Remove(waitItem.MessageId);
                }

                cancelTokenSource.Cancel();
                waitItem.CancelTask = null;
            };

            waitItem.CancelTask = Task
                .Delay(timeout, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        return;

                    waitItem.CompletionSource?.TrySetException(new TimeoutException());
                    clear();
                });

            waitItem.CompletionSource.Task.ContinueWith(t =>
            {
                var ex = t.Exception;
                //TODO Handle Exception

                clear();
            });
        }
        private void ResolveResponse(byte[] data)
        {
            var response = (FileRequestResponseBase)SerializeManager.Current.Deserialize(data);

            FileWaitItem waitItem;
            lock (_Sync)
            {
                if (!_WaitDic.TryGetValue(response.MessageId, out waitItem))
                    return;
            }

            if (response is FileEvalResponse fileEvalResponse)
            {
                waitItem.SetResult(fileEvalResponse.Data);
            }
            else if (response is FileSaveResponse fileSaveResponse)
            {
                waitItem.SetResult(null);
            }
            else if (response is FileDeleteResponse fileDeleteResponse)
            {
                waitItem.SetResult(null);
            }
            else if (response is FileInitResponse fileInitResponse)
            {
                waitItem.SetResult(null);
            }
            else
            {
                //TODO Exception
            }
        }
    }
}
