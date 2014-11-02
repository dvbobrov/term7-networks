using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FileServer.Core.Messages;

namespace FileServer.Client.Messages
{
    internal abstract class RequestMessageBase
    {
        protected readonly Socket Socket;

        protected RequestMessageBase(MessageCode code, Socket socket)
        {
            Code = code;
            Socket = socket;
        }

        public MessageCode Code { get; private set; }

        public void Send()
        {
            Socket.Send(new[] {(byte) Code});
            SendData();
        }

        public ResponseMessageBase SendAndGetResponseBase()
        {
            Send();
            ResponseMessageBase resp = GetResponse();
            resp.ReadData(Socket);
            return resp;
        }

        public ResponseMessageBase GetResponse()
        {
            var codeByte = new byte[1];
            Socket.Receive(codeByte);
            if (!Enum.IsDefined(typeof (MessageCode), codeByte[0]))
            {
                throw new Exception("Invalid message code");
            }
            var code = (MessageCode) codeByte[0];

            if (code == MessageCode.Error)
            {
                return new ErrorResponseMessage();
            }
            if (code == ExpectedResponse())
            {
                return CreateResponseMessage();
            }
            throw new Exception("Unexpected response");
        }

        protected abstract ResponseMessageBase CreateResponseMessage();
        protected abstract void SendData();
        protected abstract MessageCode ExpectedResponse();
    }

    internal sealed class ListRequestMessage : RequestMessageBase
    {
        public ListRequestMessage(Socket socket) : base(MessageCode.ReqList, socket)
        {}

        protected override void SendData()
        {}

        protected override ResponseMessageBase CreateResponseMessage()
        {
            return new ListResponseMessage();
        }

        protected override MessageCode ExpectedResponse()
        {
            return MessageCode.RespList;
        }
    }

    internal sealed class GetRequestMessage : RequestMessageBase
    {
        private readonly string _fileName;
        private readonly string _outputFilePath;

        public GetRequestMessage(Socket socket, string fileName, string outputFilePath)
            : base(MessageCode.ReqGet, socket)
        {
            _fileName = fileName;
            _outputFilePath = outputFilePath;
        }

        protected override ResponseMessageBase CreateResponseMessage()
        {
            return new GetResponseMessage(_outputFilePath);
        }

        protected override void SendData()
        {
            Socket.Send(Encoding.UTF8.GetBytes(_fileName + '\0'));
        }

        protected override MessageCode ExpectedResponse()
        {
            return MessageCode.RespGet;
        }
    }

    internal sealed class PutRequestMessage : RequestMessageBase
    {
        private readonly string _fileName;
        private readonly string _inputFilePath;

        public PutRequestMessage(Socket socket, string fileName, string inputFilePath) : base(MessageCode.ReqPut, socket)
        {
            _fileName = fileName;
            _inputFilePath = inputFilePath;
        }

        protected override ResponseMessageBase CreateResponseMessage()
        {
            return new NoneResponseMessage();
        }

        protected override void SendData()
        {
            using (Stream fileStream = File.Open(_inputFilePath, FileMode.Open, FileAccess.Read))
            {
                Socket.Send(Encoding.UTF8.GetBytes(_fileName + '\0'));
                byte[] bytes = BitConverter.GetBytes(fileStream.Length);
                Array.Reverse(bytes);
                Socket.Send(bytes);
                var buffer = new byte[4096];
                int read;
                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Socket.Send(buffer, read, SocketFlags.None);
                }
            }
        }

        protected override MessageCode ExpectedResponse()
        {
            return MessageCode.None;
        }
    }
}