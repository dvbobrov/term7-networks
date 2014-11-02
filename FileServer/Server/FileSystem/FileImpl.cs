using System;
using System.IO;
using System.Security.Cryptography;
using FileServer.Core;

namespace FileServer.Server.FileSystem
{
    internal class FileImpl : IFile
    {
        private byte[] _md5;

        public FileImpl(string name)
        {
            string workDir = ServerConfiguration.Instance.WorkingPath;
            if (!File.Exists(Path.Combine(workDir, name)))
            {
                throw new ArgumentException("File does not exist");
            }
            Name = name;
            RecalculateContentParams();
        }

        public string Name { get; private set; }

        public byte[] Md5
        {
            get
            {
                var copy = new byte[_md5.Length];
                Array.Copy(_md5, copy, copy.Length);
                return copy;
            }
        }

        public long Size { get; private set; }

        public Stream OpenForRead()
        {
            return File.Open(ServerConfiguration.Instance.GetWorkFilePath(Name), FileMode.Open, FileAccess.Read,
                             FileShare.Read);
        }

        public IFileWriteHandle OpenForWrite()
        {
            return new OverwriteHandle(this);
        }

        private void RecalculateContentParams()
        {
            using (FileStream stream = File.OpenRead(Path.Combine(ServerConfiguration.Instance.WorkingPath, Name)))
            {
                Size = stream.Length;
                using (MD5 md5 = MD5.Create())
                {
                    _md5 = md5.ComputeHash(stream);
                }
            }
        }

        private sealed class OverwriteHandle : IFileWriteHandle
        {
            private readonly FileImpl _outerInstance;
            private readonly Stream _stream;
            private bool _commited;
            private bool _disposed;

            public OverwriteHandle(FileImpl outerInstance)
            {
                _outerInstance = outerInstance;
                _stream = File.Open(ServerConfiguration.Instance.GetTempFilePath(outerInstance.Name), FileMode.CreateNew,
                                    FileAccess.Write, FileShare.Write);
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _stream.Dispose();
                string fileName = _outerInstance.Name;
                ServerConfiguration serverConfiguration = ServerConfiguration.Instance;
                if (!_commited)
                {
                    File.Delete(serverConfiguration.GetTempFilePath(fileName));
                }
                else
                {
                    string destFilePath = serverConfiguration.GetWorkFilePath(fileName);
                    File.Delete(destFilePath);
                    File.Move(serverConfiguration.GetTempFilePath(fileName),
                              destFilePath);
                    _outerInstance.RecalculateContentParams();
                }
            }

            public Stream OutputStream
            {
                get
                {
                    Check();
                    return _stream;
                }
            }

            public void Commit()
            {
                Check();
                _commited = true;
            }

            private void Check()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(ToString());
                }
                if (_commited)
                {
                    throw new InvalidOperationException("Already commited");
                }
            }
        }
    }
}