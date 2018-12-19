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

        public event EventHandler<InitEventArgs> Init;
        public event EventHandler<DeleteFileEventArgs> DeleteFile;
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
            else if (request is FileDeleteRequest fileDeleteRequest)
            {
                DeleteFileAndSend(session, fileDeleteRequest);
            }
            else if (request is FileInitRequest fileInitRequest)
            {
                InitAndSend(session, fileInitRequest);
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
        private void DeleteFileAndSend(Session session, FileDeleteRequest request)
        {
            DeleteFile?.Invoke(
                this,
                new DeleteFileEventArgs(request.Key));

            var response = new FileDeleteResponse()
            {
                MessageId = request.MessageId
            };

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
        private void InitAndSend(Session session, FileInitRequest request)
        {
            Init?.Invoke(
                this,
                new InitEventArgs(request.Data));

            var response = new FileInitResponse()
            {
                MessageId = request.MessageId
            };

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
    }
}
