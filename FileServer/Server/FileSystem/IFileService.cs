using System.Collections.Generic;

namespace FileServer.Server.FileSystem
{
    internal interface IFileService
    {
        IFile this[string name] { get; }
        uint FileCount { get; }
        IList<IFile> GetList();
        IFile Get(string name);
        IFileWriteHandle Create(string name);
        bool Exists(string name);
        bool IsValidName(string name);
    }
}