using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

            var request = new FileRequest()
            {
                MessageId = Guid.NewGuid(),
                Key = key
            };

            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            var bytesLength = BitConverter.GetBytes(bytes.Length);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);

            using (var stream = new MemoryStream())
            {
                stream.Write(bytesLength, 0, bytesLength.Length);
                stream.Write(bytes, 0, bytes.Length);

                SendData(stream.ToArray());
            }

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

            var request = new FileRequest()
            {
                MessageId = Guid.NewGuid(),
                Key = key,
                Length = data.Length
            };

            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            var bytesLength = BitConverter.GetBytes(bytes.Length);

            var waitItem = new FileWaitItem(request.MessageId);
            lock (_Sync)
            {
                _WaitDic.Add(waitItem.MessageId, waitItem);
            }

            AttachCompletionToWaitItem(waitItem, timeout.Value);

            using (var stream = new MemoryStream())
            {
                stream.Write(bytesLength, 0, bytesLength.Length);
                stream.Write(bytes, 0, bytes.Length);
                stream.Write(data, 0, data.Length);

                SendData(stream.ToArray());
            }

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
            var messageLength = BitConverter.ToInt32(data, 0);
            var json = Encoding.UTF8.GetString(data, 4, messageLength);
            var response = JsonConvert.DeserializeObject<FileResponse>(json);

            FileWaitItem waitItem;
            lock (_Sync)
            {
                if (!_WaitDic.TryGetValue(response.MessageId, out waitItem))
                    return;
            }

            if (response.HasData)
            {
                using (var stream = new MemoryStream())
                {
                    var pref = 4 + messageLength;
                    var length = data.Length - pref;
                    stream.Write(data, pref, length);

                    waitItem.SetResult(stream.ToArray());
                }
            }
            else
            {
                waitItem.SetResult(null);
            }
        }
    }
}
