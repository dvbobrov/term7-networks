using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileServer.Server.FileSystem
{
    internal class FileServiceImpl : IFileService
    {
        private readonly IDictionary<string, IFile> _files = new ConcurrentDictionary<string, IFile>();

        public FileServiceImpl()
        {
            string workPath = ServerConfiguration.Instance.WorkingPath;
            if (!Directory.Exists(workPath))
            {
                Directory.CreateDirectory(workPath);
            }
            foreach (string filePath in Directory.GetFiles(workPath))
            {
                IFile file = new FileImpl(Path.GetFileName(filePath));
                _files.Add(file.Name, file);
            }
            string tempPath = ServerConfiguration.Instance.TempPath;
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        public IList<IFile> GetList()
        {
            return _files.Values.ToList();
        }

        public IFile Get(string name)
        {
            if (_files.ContainsKey(name))
            {
                return _files[name];
            }
            return null;
        }

        public IFileWriteHandle Create(string name)
        {
            return new NewFileHandle(this, name);
        }

        public bool Exists(string name)
        {
            return _files.ContainsKey(name);
        }

        public IFile this[string name]
        {
            get { return Get(name); }
        }

        public uint FileCount
        {
            get { return (uint) _files.Count; }
        }

        public bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            char[] invaldChars = Path.GetInvalidFileNameChars();
            return !name.Any(invaldChars.Contains);
        }

        private sealed class NewFileHandle : IFileWriteHandle
        {
            private readonly string _name;
            private readonly FileServiceImpl _outerInstance;
            private readonly Stream _stream;
            private bool _commited;
            private bool _disposed;

            public NewFileHandle(FileServiceImpl outerInstance, string name)
            {
                _outerInstance = outerInstance;
                _name = name;
                _stream = File.Open(ServerConfiguration.Instance.GetWorkFilePath(name), FileMode.CreateNew,
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
                if (_commited)
                {
                    IFile file = new FileImpl(_name);
                    _outerInstance._files.Add(_name, file);
                }
                else
                {
                    File.Delete(ServerConfiguration.Instance.GetWorkFilePath(_name));
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