using System;
using System.Configuration;
using System.IO;

namespace FileServer.Server
{
    internal class ServerConfiguration
    {
        private static readonly Lazy<ServerConfiguration> _instance =
            new Lazy<ServerConfiguration>(() => new ServerConfiguration());

        public readonly string TempPath = ConfigurationManager.AppSettings["TmpDir"];
        public readonly string WorkingPath = ConfigurationManager.AppSettings["FilesDir"];

        public static ServerConfiguration Instance
        {
            get { return _instance.Value; }
        }

        public string GetTempFilePath(string name)
        {
            return Path.Combine(TempPath, name);
        }

        public string GetWorkFilePath(string name)
        {
            return Path.Combine(WorkingPath, name);
        }
    }
}