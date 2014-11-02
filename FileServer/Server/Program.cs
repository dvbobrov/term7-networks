using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FileServer.Core;
using FileServer.Server.Announces;
using FileServer.Server.FileSystem;
using FileServer.Server.Messages;

namespace FileServer.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please specify network interface name");
                return;
            }
            string networkInterfaceName = args[0];
            IFileService fileService = new FileServiceImpl();
            using (var announcer = new UdpAnnouncer(new AnnounceDataProvider("Dmitry", fileService), networkInterfaceName))
            using (var handler = new MessageHandler(fileService))
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Any, Constants.Port));
                socket.Listen(20);
                while (true)
                {
                    Socket incomingConnection = socket.Accept();
                    incomingConnection.ReceiveTimeout = 5000;
                    incomingConnection.SendTimeout = 5000;
                    Debug.Print("Incoming connection from {0}", incomingConnection.RemoteEndPoint);
                    HandleMessage(handler, incomingConnection);
                }
            }
        }

        private static void HandleMessage(MessageHandler messageHandler, Socket socket)
        {
            ThreadPool.QueueUserWorkItem(_ => messageHandler.DispatchMessage(socket));
        }
    }
}