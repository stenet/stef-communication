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
        }
        private void EvalFileAndSend(Session session, FileEvalRequest request)
        {
            var evalFileEventArgs = new EvalFileEventArgs(request.Key);
            var response = new FileEvalResponse()
            {
                MessageId = request.MessageId,
                ResponseType = ResponseType.OK
            };

            try
            {
                EvalFile?.Invoke(this, evalFileEventArgs);
                response.Data = evalFileEventArgs.Data;
            }
            catch (Exception ex)
            {
                OnException(session, ex, disconnect: false);
                response.ResponseType = ResponseType.Exception;
                response.Exception = ex.Message;
            }

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
        private void SaveFileAndSend(Session session, FileSaveRequest request)
        {
            var saveFileEventArgs = new SaveFileEventArgs(request.Key, request.Data);
            var response = new FileSaveResponse()
            {
                MessageId = request.MessageId,
                ResponseType = ResponseType.OK
            };

            try
            {
                SaveFile?.Invoke(
                    this,
                    saveFileEventArgs);
            }
            catch (Exception ex)
            {
                OnException(session, ex, disconnect: false);
                response.ResponseType = ResponseType.Exception;
                response.Exception = ex.Message;
            }

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
        private void DeleteFileAndSend(Session session, FileDeleteRequest request)
        {
            var deleteFileEventArgs = new DeleteFileEventArgs(request.Key);

            var response = new FileDeleteResponse()
            {
                MessageId = request.MessageId,
                ResponseType = ResponseType.OK
            };

            try
            {
                DeleteFile?.Invoke(
                    this,
                    deleteFileEventArgs);
            }
            catch (Exception ex)
            {
                OnException(session, ex, disconnect: false);
                response.ResponseType = ResponseType.Exception;
                response.Exception = ex.Message;
            }

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
        private void InitAndSend(Session session, FileInitRequest request)
        {
            var initEventArgs = new InitEventArgs(request.Data);
            var response = new FileInitResponse()
            {
                MessageId = request.MessageId,
                ResponseType = ResponseType.OK
            };

            try
            {
                Init?.Invoke(
                    this,
                    initEventArgs);
            }
            catch (Exception ex)
            {
                OnException(session, ex, disconnect: false);
                response.ResponseType = ResponseType.Exception;
                response.Exception = ex.Message;
            }

            SendData(
                session,
                SerializeManager.Current.Serialize(response));
        }
    }
}
