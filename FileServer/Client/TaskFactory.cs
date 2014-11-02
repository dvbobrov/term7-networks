using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FileServer.Client.Messages;
using FileServer.Core;

namespace FileServer.Client
{
    internal static class TaskFactory
    {
        public static Task<ResponseMessageBase> CreateListTask(IPAddress ip)
        {
            return DoCreateTask(ip, socket => new ListRequestMessage(socket));
        }

        public static Task<ResponseMessageBase> CreateGetTask(IPAddress ip, string fileName, string outputFilePath) {
            return DoCreateTask(ip, socket => new GetRequestMessage(socket, fileName, outputFilePath));
        }

        public static Task<ResponseMessageBase> CreatePutTask(IPAddress ip, string fileName, string inputFilePath)
        {
            return Task.Run(() => {
                 try {
                     using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                     {
                         socket.ReceiveTimeout = 2000;
                         socket.SendTimeout = 2000;
                         socket.Connect(ip, Constants.Port);
                         RequestMessageBase msg = new PutRequestMessage(socket, fileName, inputFilePath);
                         msg.Send();
                         if (socket.Connected)
                         {
                             return msg.GetResponse();
                         }
                         return new NoneResponseMessage();
                     }
                 } catch (Exception ex) {
                     Console.Error.WriteLine(ex);
                 }
                 return null;
             }); 
        }

        private static Task<ResponseMessageBase> DoCreateTask(IPAddress ip,
                                                              Func<Socket, RequestMessageBase> messageCreator)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.ReceiveTimeout = 2000;
                         socket.SendTimeout = 2000;
                        socket.Connect(ip, Constants.Port);
                        RequestMessageBase msg = messageCreator(socket);
                        return msg.SendAndGetResponseBase();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                return null;
            });
        }
    }
}