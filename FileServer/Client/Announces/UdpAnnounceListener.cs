using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using FileServer.Core.Messages;

namespace FileServer.Client.Announces {
    public delegate void AnnounceReceivedHandler(AnnounceMessage message);

    internal class UdpAnnounceListener : IDisposable {
        private readonly int _port;
        private readonly Thread _worker;

        private volatile bool _running;

        public UdpAnnounceListener(int port) {
            _worker = new Thread(Run);
            _port = port;
            _running = true;
            _worker.Start();
        }

        public void Dispose() {
            _running = false;
            if (_worker.IsAlive) {
                _worker.Abort();
            }
        }

        public event AnnounceReceivedHandler AnnounceReceived;

        private void Run() {
            try {
                using (var listener = new UdpClient(_port)) {
                    listener.EnableBroadcast = true;
                    var endpoint = new IPEndPoint(IPAddress.Any, _port);
                    var timeToWait = TimeSpan.FromSeconds(5);

                    while (_running) {
                        var asyncResult = listener.BeginReceive(null, null);
                        while (!asyncResult.IsCompleted && _running) {
                            asyncResult.AsyncWaitHandle.WaitOne(timeToWait);
                        }
                        if (!_running) {
                            return;
                        }
                        try {
                            byte[] bytes = listener.EndReceive(asyncResult, ref endpoint);
                            Console.WriteLine("Announce received from {0}", endpoint);
                            AnnounceMessage message = AnnounceMessage.Parse(bytes);
//                            if (!endpoint.Address.Equals(message.Ip)) {
//                                Console.WriteLine("Announce from {0} is corrupted", endpoint);
//                                continue;
//                            }
                            AnnounceReceivedHandler eventHandler = AnnounceReceived;
                            if (eventHandler != null) {
                                eventHandler(message);
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine("Announce from {0} is corrupted", endpoint);
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            catch (ThreadAbortException) {}
            catch (Exception e) {
                Debug.Print(e.ToString());
                Application.Current.Dispatcher.Invoke(() => {
                    MessageBox.Show("Error in UDP listener");
                    Application.Current.Shutdown(1);
                });
            }
        }
    }
}