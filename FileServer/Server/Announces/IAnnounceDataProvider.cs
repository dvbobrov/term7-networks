using System.Net;
using FileServer.Core.Messages;

namespace FileServer.Server.Announces {
    internal interface IAnnounceDataProvider {
        AnnounceMessage GetAnnounceMessage(IPAddress sourceAddress);
    }
}
