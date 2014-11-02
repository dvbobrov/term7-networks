using System.IO;
using FileServer.Core;

namespace FileServer.Server.FileSystem
{
    internal interface IFile
    {
        string Name { get; }
        byte[] Md5 { get; }
        long Size { get; }

        Stream OpenForRead();
        IFileWriteHandle OpenForWrite();
    }
}