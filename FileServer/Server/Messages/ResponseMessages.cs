using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FileServer.Core.Messages;
using FileServer.Server.FileSystem;

namespace FileServer.Server.Messages
{
    internal abstract class ResponseMessageBase
    {
        protected readonly Socket Socket;

        protected ResponseMessageBase(MessageCode code, MessageKind kind, Socket socket)
        {
            Code = code;
            Socket = socket;
            Kind = kind;
        }

        public MessageCode Code { get; private set; }
        public MessageKind Kind { get; private set; }

        public void SendBase()
        {
            if (Kind == MessageKind.RequiresResponse)
            {
                var b = new[] {(byte) Code};
                Socket.Send(b);
                Send();
            }
        }

        public virtual void Send()
        {}
    }

    internal sealed class ListResponseMessage : ResponseMessageBase
    {
        private readonly IList<IFile> _files;

        public ListResponseMessage(Socket socket, IList<IFile> files)
            : base(MessageCode.RespList, MessageKind.RequiresResponse, socket)
        {
            _files = files;
        }

        public override void Send()
        {
            byte[] buffer = BitConverter.GetBytes(_files.Count);
            Array.Reverse(buffer);
            Socket.Send(buffer);
            foreach (IFile file in _files)
            {
                Socket.Send(file.Md5);
                Socket.Send(Encoding.UTF8.GetBytes(file.Name + '\0'));
            }
        }
    }

    internal sealed class GetResponseMessage : ResponseMessageBase
    {
        private readonly IFile _file;

        public GetResponseMessage(Socket socket, IFile file)
            : base(MessageCode.RespGet, MessageKind.RequiresResponse, socket)
        {
            _file = file;
        }

        public override void Send()
        {
            using (Stream stream = _file.OpenForRead())
            {
                byte[] bytes = BitConverter.GetBytes(stream.Length);
                Array.Reverse(bytes);
                Socket.Send(bytes);
                Socket.Send(_file.Md5);

                var buffer = new byte[4096];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Socket.Send(buffer, read, 0);
                }
            }
        }
    }

    internal sealed class NoneResponseMessage : ResponseMessageBase
    {
        public NoneResponseMessage(Socket socket) : base(MessageCode.None, MessageKind.NoResponse, socket)
        {}
    }

    internal sealed class ErrorResponseMessage : ResponseMessageBase
    {
        private readonly ErrorCode _errorCode;

        public ErrorResponseMessage(Socket socket, ErrorCode errorCode)
            : base(MessageCode.Error, MessageKind.RequiresResponse, socket)
        {
            _errorCode = errorCode;
        }

        public override void Send()
        {
            Socket.Send(new[] {(byte) _errorCode});
        }
    }
}