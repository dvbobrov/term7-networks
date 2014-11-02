using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FileServer.Client.Model;
using FileServer.Core.Messages;

namespace FileServer.Client.Messages
{
    internal abstract class ResponseMessageBase
    {
        protected ResponseMessageBase(MessageCode code)
        {
            Code = code;
        }

        public MessageCode Code { get; private set; }

        public abstract void ReadData(Socket socket);
    }

    internal sealed class ErrorResponseMessage : ResponseMessageBase
    {
        public ErrorResponseMessage() : base(MessageCode.Error)
        {}

        public ErrorCode ErrorCode { get; private set; }

        public override void ReadData(Socket socket)
        {
            var b = new byte[1];
            socket.Receive(b);
            if (!Enum.IsDefined(typeof (ErrorCode), b[0]))
            {
                ErrorCode = ErrorCode.InternalServerError;
                return;
            }
            ErrorCode = (ErrorCode) b[0];
        }
    }

    internal sealed class ListResponseMessage : ResponseMessageBase
    {
        private FileModel[] _files;

        public ListResponseMessage() : base(MessageCode.RespList)
        {}

        public override void ReadData(Socket socket)
        {
            var countBytes = new byte[4];
            int read = socket.Receive(countBytes);
            while (read < 4)
            {
                read += socket.Receive(countBytes, read, 4 - read, SocketFlags.None);
            }
            Array.Reverse(countBytes);
            uint count = BitConverter.ToUInt32(countBytes, 0);
            _files = new FileModel[count];
            if (count == 0)
            {
                return;
            }

            var stream = new MemoryStream();
            var buffer = new byte[4096];
            int md5BytesLeft = 16;
            var breakPositions = new int[count];
            int total = 0;

            while (count > 0)
            {
                read = socket.Receive(buffer);
                for (int i = 0; i < read; i++)
                {
                    if (md5BytesLeft == 0 &&
                        buffer[i] == 0)
                    {
                        breakPositions[breakPositions.Length - count] = total + i;
                        count--;
                        md5BytesLeft = 16;
                    }
                    else if (md5BytesLeft > 0)
                    {
                        md5BytesLeft--;
                    }
                }
                total += read;
                stream.Write(buffer, 0, read);
            }

            stream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < _files.Length; i++)
            {
                var file = new FileModel {
                    Md5 = new byte[16]
                };
                stream.Read(file.Md5, 0, 16);
                int strLen = breakPositions[i] - (int) stream.Position;
                var strData = new byte[strLen + 1];
                stream.Read(strData, 0, strLen + 1);
                file.Name = Encoding.UTF8.GetString(strData, 0, strLen);
                _files[i] = file;
            }
        }

        public FileModel[] GetData()
        {
            return _files;
        }
    }

    internal sealed class GetResponseMessage : ResponseMessageBase
    {
        private readonly string _outputFile;

        public GetResponseMessage(string outputFile) : base(MessageCode.RespGet)
        {
            _outputFile = outputFile;
        }

        public override void ReadData(Socket socket)
        {
            long size;
            var buffer = new byte[4096];
            int read = 0;
            while (read < 16 + sizeof(long))
            {
                read += socket.Receive(buffer, read, buffer.Length - read, SocketFlags.None);
            }
            Array.Reverse(buffer, 0, 8);
            size = BitConverter.ToInt64(buffer, 0);

            using (Stream fileStream = File.Open(_outputFile, FileMode.Create, FileAccess.Write))
            {
                int tail = read - 16 - sizeof(long);
                if (tail > 0)
                {
                    fileStream.Write(buffer, 16 + sizeof (long), tail);
                    size = size - tail;
                }
                while (size > 0)
                {
                    read = socket.Receive(buffer);
                    fileStream.Write(buffer, 0, read);
                    size = size - read;
                }
            }
        }
    }

    internal sealed class NoneResponseMessage : ResponseMessageBase
    {
        public NoneResponseMessage() : base(MessageCode.None)
        {}

        public override void ReadData(Socket socket)
        { }
    }
}