using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FileServer.Core.Messages;
using FileServer.Server.FileSystem;
using FileServer.Server.Locking;

namespace FileServer.Server.Messages
{
    internal abstract class RequestMessageBase
    {
        protected readonly IFileService FileService;
        protected readonly LockFactory<string> LockFactory;
        protected readonly Socket Socket;

        protected RequestMessageBase(MessageCode code, LockFactory<string> lockFactory, IFileService fileService,
                                     Socket socket)
        {
            Code = code;
            LockFactory = lockFactory;
            FileService = fileService;
            Socket = socket;
        }

        public MessageCode Code { get; private set; }

        public void ProcessBase()
        {
            Debug.Print("Process started");
            Lock();
            Debug.Print("Lock acquired");
            if (LockFactory.IsDisposed)
            {
                return;
            }
            try
            {
                ResponseMessageBase response = Process();
                Debug.Print("Processed");
                response.SendBase();
                Debug.Print("Message sent");
            }
            finally
            {
                try
                {
                    Unlock();
                    Debug.Print("Unlocked");
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.ToString());
                }
            }
        }

        public abstract ResponseMessageBase Process();
        public abstract void Lock();
        public abstract void Unlock();
    }

    internal sealed class ListRequestMessage : RequestMessageBase
    {
        public ListRequestMessage(LockFactory<string> lockFactory, IFileService fileService, Socket socket)
            : base(MessageCode.ReqList, lockFactory, fileService, socket)
        {}

        public override ResponseMessageBase Process()
        {
            IList<IFile> files = FileService.GetList();
            return new ListResponseMessage(Socket, files);
        }

        public override void Lock()
        {}

        public override void Unlock()
        {}
    }

    internal sealed class GetRequestMessage : RequestMessageBase
    {
        private readonly string _fileName;

        public GetRequestMessage(LockFactory<string> lockFactory, IFileService fileService, Socket socket)
            : base(MessageCode.ReqGet, lockFactory, fileService, socket)
        {
            var buffer = new byte[256];
            int size = 0;
            int read;
            while ((read = socket.Receive(buffer, size, buffer.Length - size, SocketFlags.None)) > 0)
            {
                for (int i = size; i < size + read; i++)
                {
                    if (buffer[i] == 0)
                    {
                        size = i;
                        goto SizeFound;
                    }
                }
            }
            SizeFound:
            _fileName = Encoding.UTF8.GetString(buffer, 0, size);
            if (!FileService.IsValidName(_fileName))
            {
                throw new MessageException(ErrorCode.MalformedMessage);
            }
            if (!FileService.Exists(_fileName))
            {
                throw new MessageException(ErrorCode.FileNotFound);
            }
        }

        public override ResponseMessageBase Process()
        {
            IFile file = FileService[_fileName];
            return new GetResponseMessage(Socket, file);
        }

        public override void Lock()
        {
            LockFactory.Get(_fileName).Acquire(LockKind.Read);
        }

        public override void Unlock()
        {
            LockFactory.Get(_fileName).Release(LockKind.Read);
        }
    }

    internal sealed class PutRequestMessage : RequestMessageBase
    {
        private readonly string _fileName;
        private readonly long _fileSize;
        private byte[] _headerTail;

        public PutRequestMessage(LockFactory<string> lockFactory, IFileService fileService, Socket socket)
            : base(MessageCode.ReqPut, lockFactory, fileService, socket)
        {
            var buffer = new byte[256 + sizeof (long)];
            int gotBytes = 0;
            int strSize = 0;
            int read;
            while ((read = Socket.Receive(buffer, gotBytes, buffer.Length - gotBytes, SocketFlags.None)) > 0)
            {
                for (int i = gotBytes; i < gotBytes + read; i++)
                {
                    if (buffer[i] == 0)
                    {
                        _fileName = Encoding.UTF8.GetString(buffer, 0, i);
                        strSize = i + 1;
                        break;
                    }
                }
                gotBytes += read;
                if (gotBytes >= 256 && (strSize == 0 || strSize > 256))
                {
                    throw new MessageException(ErrorCode.MalformedMessage);
                }
                if (strSize > 0)
                {
                    break;
                }
            }
            if (!FileService.IsValidName(_fileName))
            {
                throw new MessageException(ErrorCode.MalformedMessage);
            }
            while (gotBytes < strSize + 8)
            {
                read = Socket.Receive(buffer, gotBytes, buffer.Length - gotBytes, SocketFlags.None);
                gotBytes += read;
            }
            Array.Reverse(buffer, strSize, 8);
            _fileSize = BitConverter.ToInt64(buffer, strSize);
            if (gotBytes > strSize + 8)
            {
                _headerTail = new byte[gotBytes - strSize - 8];
                Array.Copy(buffer, strSize + 8, _headerTail, 0, _headerTail.Length);
            }
        }

        public override ResponseMessageBase Process()
        {
            using (IFileWriteHandle fileWriteHandle = GetFileWriteHandle())
            {
                Stream outputStream = fileWriteHandle.OutputStream;
                var buffer = new byte[4096];
                long bytesLeft = _fileSize;
                if (_headerTail != null)
                {
                    bytesLeft = bytesLeft - _headerTail.Length;
                    outputStream.Write(_headerTail, 0, _headerTail.Length);
                    _headerTail = null;
                }

                int read;
                while (bytesLeft > 0)
                {
                    read = Socket.Receive(buffer);
                    outputStream.Write(buffer, 0, read);
                    bytesLeft = bytesLeft - read;
                }
                if (bytesLeft < 0)
                {
                    throw new MessageException(ErrorCode.MalformedMessage);
                }
                fileWriteHandle.Commit();
            }
            return new NoneResponseMessage(Socket);
        }

        private IFileWriteHandle GetFileWriteHandle()
        {
            if (FileService.Exists(_fileName))
            {
                return FileService[_fileName].OpenForWrite();
            }
            return FileService.Create(_fileName);
        }

        public override void Lock()
        {
            LockFactory.Get(_fileName).Acquire(LockKind.Write);
        }

        public override void Unlock()
        {
            LockFactory.Get(_fileName).Release(LockKind.Write);
        }
    }
}