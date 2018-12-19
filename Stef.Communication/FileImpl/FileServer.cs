using System;
using System.IO;
using System.Linq;
using System.Text;
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
            var request = (FileRequestResponseBase)SerializeManager.Current.Deserialize(data);

            if (request is FileEvalRequest fileEvalRequest)
            {
                EvalFileAndSend(session, fileEvalRequest);
            }
            else if (request is FileSaveRequest fileSaveRequest)
            {
                SaveFileAndSend(session, fileSaveRequest);
            }
            else
            {
                //TODO Exception
            }
        }
        private void EvalFileAndSend(Session session, FileEvalRequest request)
        {
            var evalFileEventArgs = new EvalFileEventArgs(request.Key);
            EvalFile?.Invoke(this, evalFileEventArgs);

            var response = new FileEvalResponse()
            {
                MessageId = request.MessageId,
                Data = evalFileEventArgs.Data
            };

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
        private void SaveFileAndSend(Session session, FileSaveRequest request)
        {
            SaveFile?.Invoke(
                this,
                new SaveFileEventArgs(request.Key, request.Data));


            var response = new FileSaveResponse()
            {
                MessageId = request.MessageId
            };

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
    }
}
