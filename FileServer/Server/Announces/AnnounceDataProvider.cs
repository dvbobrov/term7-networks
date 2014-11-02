using System;
using System.Net;
using FileServer.Core.Messages;
using FileServer.Server.FileSystem;

namespace FileServer.Server.Announces {
    internal sealed class AnnounceDataProvider : IAnnounceDataProvider {
        private readonly ChangeableAnnounceMessage _message;
        private readonly IFileService _fileService;
        private readonly string _name;

        public AnnounceDataProvider(string name, IFileService fileService)
        {
            _name = name;
            _fileService = fileService;
            _message = new ChangeableAnnounceMessage(name, IPAddress.Any, DateTime.UtcNow, _fileService.FileCount);
        }

        public AnnounceMessage GetAnnounceMessage(IPAddress sourceAddress) {
            return new AnnounceMessage(_name, sourceAddress, DateTime.UtcNow, _fileService.FileCount);
        }
    }

    internal sealed class ChangeableAnnounceMessage : AnnounceMessage {
        public ChangeableAnnounceMessage(string name, IPAddress ip, DateTime timestamp, uint fileCount) : 
            base(name, ip, timestamp, fileCount) {}

        public void UpdateInfo(IPAddress ip, uint fileCount) {
            Ip = ip;
            FileCount = fileCount;
            Timestamp = DateTime.UtcNow;
        }
    }
}