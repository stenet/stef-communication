using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Stef.Communication.Base;

namespace Stef.Communication.FileImpl
{
    public class FileServer : ServerBase
    {
        public FileServer(string ip = null, int? port = null) : base(ip, port)
        {
        }

        public event EventHandler<EvalFileEventArgs> EvalFile;
        public event EventHandler<SaveFileEventArgs> SaveFile;

        protected override void OnDataReceived(Session session, byte[] data)
        {
            ResolveRequest(session, data);

            base.OnDataReceived(session, data);
        }

        private void ResolveRequest(Session session, byte[] data)
        {
            var messageLength = BitConverter.ToInt32(data, 0);
            var json = Encoding.UTF8.GetString(data, 4, messageLength);
            var request = JsonConvert.DeserializeObject<FileRequest>(json);

            if (request.Length == 0)
            {
                EvalFileAndSend(session, request);
                
            }
            else
            {
                using (var stream = new MemoryStream())
                {
                    var pref = 4 + messageLength;
                    var length = data.Length - pref;
                    stream.Write(data, pref, length);

                    SaveFileAndSend(session, request, stream.ToArray());
                }
            }
        }
        private void EvalFileAndSend(Session session, FileRequest request)
        {
            var evalFileEventArgs = new EvalFileEventArgs(request.Key);
            EvalFile?.Invoke(this, evalFileEventArgs);

            SendResponse(session, request, evalFileEventArgs.Data);
        }
        private void SaveFileAndSend(Session session, FileRequest request, byte[] data)
        {
            SaveFile?.Invoke(
                this,
                new SaveFileEventArgs(request.Key, data));

            SendResponse(session, request, null);
        }
        private void SendResponse(Session session, FileRequest request, byte[] data)
        {
            var hasFileData = data != null
                && data.Length > 0;

            var response = new FileResponse()
            {
                MessageId = request.MessageId,
                HasData = hasFileData
            };

            var responseJson = JsonConvert.SerializeObject(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            var responseBytesLength = BitConverter.GetBytes(responseBytes.Length);

            using (var stream = new MemoryStream())
            {
                stream.Write(responseBytesLength, 0, responseBytesLength.Length);
                stream.Write(responseBytes, 0, responseBytes.Length);

                if (hasFileData)
                {
                    stream.Write(
                        data,
                        0,
                        data.Length);
                }

                SendData(session, stream.ToArray());
            }
        }
    }
}
