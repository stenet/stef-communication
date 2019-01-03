using Stef.Communication.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Stef.Communication.Base
{
    public class CommunicationBase
    {
        private const int BUFFER_LENGTH = 1024;
        private object _SendLock = new object();
        private object _ExceptionLock = new object();
        private int _ExceptionCounter = 0;

        public CommunicationBase(string ip = null, int? port = null)
        {
            if (ip == null)
                ip = IPAddress.Loopback.ToString();

            if (port == null)
                port = 8015;

            IP = ip;
            Port = port.Value;
        }

        public string IP { get; private set; }
        public int Port { get; private set; }

        public TimeSpan CheckAliveTimespan { get; set; } = TimeSpan.FromSeconds(1);

        protected byte[] QUIT_BYTES { get; } = new byte[0];

        public event EventHandler<SessionChangedEventArgs> Connected;
        public event EventHandler<SessionChangedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ExceptionEventArgs> Exception;

        protected void CheckAlive(Session session)
        {
            Task.Factory.StartNew(() =>
            {
                var isDeadCount = 0;

                while (session.TcpClient != null)
                {
                    Thread.Sleep(CheckAliveTimespan);

                    if (session.TcpClient == null)
                        break;

                    var socket = session.TcpClient.Client;
                    var isAlive = !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);

                    isDeadCount = isAlive
                        ? 0
                        : isDeadCount + 1;

                    if (isAlive)
                        continue;

                    if (isDeadCount < 4)
                        continue;

                    OnException(session, new TimeoutException());
                    break;
                }
            });            
        }

        protected internal void SendDataInternal(Session session, byte[] data)
        {
            if (session == null)
                throw new InvalidSessionException("Session not initialized");

            if (session.TcpClient == null)
                throw new InvalidSessionException("TcpClient in Session has been disposed");

            var lengthBuffer = BitConverter.GetBytes(data.Length);

            try
            {
                lock (_SendLock)
                {
                    session
                        .Stream
                        .Write(lengthBuffer, 0, 4);

                    session
                        .Stream
                        .Write(data, 0, data.Length);

                    session.Stream.Flush();
                }
            }
            catch (IOException ex)
            {
                if (session.TcpClient != null)
                    OnDisconnected(session);

                throw new InvalidSessionException(ex.Message, ex);
            }
            catch (ObjectDisposedException ex)
            {
                if (session.TcpClient != null)
                    OnDisconnected(session);

                throw new InvalidSessionException(ex.Message, ex);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void OnConnected(Session session)
        {
            InitializeReader(session);
            CheckAlive(session);
            Connected?.Invoke(this, new SessionChangedEventArgs(session));
        }
        protected virtual void OnDisconnected(Session session)
        {
            Disconnected?.Invoke(this, new SessionChangedEventArgs(session));
            session.Dispose();
        }
        protected internal virtual void OnException(Session session, Exception exception, bool disconnect = true)
        {
            if (BeginException())
            {
                try
                {
                    Exception?.Invoke(this, new ExceptionEventArgs(exception));
                }
                catch (Exception)
                {
                    EndException();
                }
            }

            if (!disconnect)
                return;

            lock (_ExceptionLock)
            {
                if (session.TcpClient == null)
                    return;

                OnDisconnected(session);
            }
        }
        protected virtual void OnDataReceived(Session session, byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(session, data));
        }

        private bool BeginException()
        {
            lock (_ExceptionLock)
            {
                if (_ExceptionCounter > 0)
                    return false;

                _ExceptionCounter++;
                return true;
            }
        }
        private void EndException()
        {
            lock (_ExceptionLock)
            {
                _ExceptionCounter--;
            }
        }

        private async void InitializeReader(Session session)
        {
            var stream = session.Stream;
            var memoryStream = new MemoryStream();
            var length = -1;
            var skipReadStream = false;

            try
            {
                while (session.TcpClient != null)
                {
                    var buffer = new byte[BUFFER_LENGTH];

                    if (!skipReadStream)
                    {
                        var read = await stream.ReadAsync(buffer, 0, BUFFER_LENGTH);
                        memoryStream.Write(buffer, 0, read);
                    }
                    skipReadStream = false;

                    if (length < 0 && memoryStream.Length < 4)
                        continue;

                    if (length < 0)
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var lengthBuffer = new byte[4];
                        memoryStream.Read(lengthBuffer, 0, 4);

                        length = BitConverter.ToInt32(lengthBuffer, 0);

                        if (memoryStream.Length < 4 + length)
                        {
                            memoryStream.Seek(0, SeekOrigin.End);
                            continue;
                        }
                    }

                    if (length == 0)
                    {
                        OnDisconnected(session);
                        return;
                    }

                    if (memoryStream.Length < 4 + length)
                        continue;

                    memoryStream.Seek(4, SeekOrigin.Begin);

                    buffer = new byte[length];
                    memoryStream.Read(buffer, 0, length);

                    CallDataReceived(session, buffer);

                    buffer = new byte[memoryStream.Length - 4 - length];
                    memoryStream.Read(buffer, 0, buffer.Length);

                    memoryStream = new MemoryStream();
                    memoryStream.Write(buffer, 0, buffer.Length);
                    length = -1;
                    skipReadStream = true;
                }
            }
            catch (ObjectDisposedException) //= shutdown
            {
                OnDisconnected(session);
            }
            catch (IOException ex)
            {
                OnException(session, ex);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                memoryStream.Dispose();
            }
        }

        private void CallDataReceived(Session session, byte[] buffer)
        {
            Task.Run(() =>
            {
                try
                {
                    OnDataReceived(session, buffer);
                }
                catch (Exception ex)
                {
                    OnException(session, ex, disconnect: false);
                }
            });
        }
    }
}
