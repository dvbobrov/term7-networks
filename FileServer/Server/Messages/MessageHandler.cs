using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using FileServer.Core.Messages;
using FileServer.Server.FileSystem;
using FileServer.Server.Locking;

namespace FileServer.Server.Messages
{
    internal class MessageHandler : IDisposable
    {
        private readonly IFileService _fileService;
        private readonly LockFactory<string> _lockFactory;

        public MessageHandler(IFileService fileService)
        {
            _fileService = fileService;
            _lockFactory = new LockFactory<string>();
        }

        public void Dispose()
        {
            _lockFactory.Dispose();
        }

        public void DispatchMessage(Socket socket)
        {
            using (socket)
            {
                try
                {
                    var codeByte = new byte[1];
                    socket.Receive(codeByte);

                    MessageCode code;
                    if (!Enum.IsDefined(typeof (MessageCode), codeByte[0]))
                    {
                        code = MessageCode.None;
                    }
                    else
                    {
                        code = (MessageCode) codeByte[0];
                    }

                    Debug.Print("Message code {0}", code);

                    RequestMessageBase message = null;
                    switch (code)
                    {
                        case MessageCode.ReqGet:
                        {
                            message = new GetRequestMessage(_lockFactory, _fileService, socket);
                            break;
                        }
                        case MessageCode.ReqList:
                        {
                            message = new ListRequestMessage(_lockFactory, _fileService, socket);
                            break;
                        }
                        case MessageCode.ReqPut:
                        {
                            message = new PutRequestMessage(_lockFactory, _fileService, socket);
                            break;
                        }
                        default:
                        {
                            SendErrorResponse(socket, ErrorCode.MalformedMessage);
                            break;
                        }
                    }
                    if (message != null)
                    {
                        message.ProcessBase();
                    }
                }
                catch (MessageException ex)
                {
                    Debug.Print(ex.ToString());
                    SendErrorResponse(socket, ex.ErrorCode);
                }
                catch (SocketException ex)
                {
                    Debug.Print(ex.ToString());
                    // Connection is probably lost, closing it
                }
                catch (ThreadAbortException)
                {
                    Debug.Print("Thread aborted");
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.ToString());
                    SendErrorResponse(socket, ErrorCode.InternalServerError);
                }
            }
        }

        private static void SendErrorResponse(Socket socket, ErrorCode errorCode)
        {
            try
            {
                new ErrorResponseMessage(socket, errorCode).SendBase();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
    }
}