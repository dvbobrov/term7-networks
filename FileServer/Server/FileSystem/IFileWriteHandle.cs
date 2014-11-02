using System;
using System.IO;

namespace FileServer.Server.FileSystem
{
    internal interface IFileWriteHandle : IDisposable
    {
        Stream OutputStream { get; }
        void Commit();
    }
}