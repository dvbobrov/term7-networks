using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using FileServer.Core;

namespace FileServer.Server.Announces {
    internal class UdpAnnouncer : IDisposable {
        private readonly Thread _worker;
        private volatile bool _running;
        private readonly IAnnounceDataProvider _dataProvider;
        private readonly string _networkInterfaceName;

        public UdpAnnouncer(IAnnounceDataProvider dataProvider, string networkInterfaceName) {
            _dataProvider = dataProvider;
            _networkInterfaceName = networkInterfaceName;
            _worker = new Thread(this.Run);
            _running = true;
            _worker.Start();
        }

        private void Run() {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.Name == _networkInterfaceName);
            IPAddress address = null;
            if (networkInterface != null) {
                address = networkInterface
                    .GetIPProperties()
                    .UnicastAddresses
                    .Select(ua => ua.Address)
                    .FirstOrDefault(addr => addr != null && 
                        addr.AddressFamily == AddressFamily.InterNetwork);
            }

            try {
                if (address == null) {
                    throw new Exception("Address not found");
                }

                var targetEp = new IPEndPoint(IPAddress.Broadcast, Constants.Port);
                using (var client = new UdpClient(new IPEndPoint(address, 0))) {
                    client.EnableBroadcast = true;
                    var announceData = new byte[512];
                    while (_running) {
                        int size = _dataProvider.GetAnnounceMessage(address).Write(announceData);
                        client.Send(announceData, size, targetEp);
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch (ThreadAbortException) {}
            catch (Exception e) {
                Debug.Print(e.ToString());
                Environment.Exit(1);
            }
        }

        public void Dispose() {
            _running = false;
            if (_worker.IsAlive) {
                _worker.Abort();
            }
        }
    }
}
