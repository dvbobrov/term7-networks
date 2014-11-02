using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using FileServer.Client.Announces;
using FileServer.Client.Messages;
using FileServer.Client.Model;
using FileServer.Client.ViewModel;
using FileServer.Core;
using FileServer.Core.Messages;
using Microsoft.Win32;

namespace FileServer.Client
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<HostViewModel> _hosts = new ObservableCollection<HostViewModel>();
        private readonly UdpAnnounceListener _udpListener;

        public MainWindow()
        {
            _udpListener = new UdpAnnounceListener(Constants.Port);
            Closing += (sender, args) => _udpListener.Dispose();
            InitializeComponent();
            _udpListener.AnnounceReceived += UdpListener_AnnounceReceived;
        }

        public ObservableCollection<HostViewModel> Hosts
        {
            get { return _hosts; }
        }

        private async void UpdateBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = ListClients.SelectedItem as HostViewModel;
            if (selectedItem == null)
            {
                return;
            }

            CurrentProgress.Visibility = Visibility.Visible;
            ResponseMessageBase response = await TaskFactory.CreateListTask(selectedItem.IpAddress);

            if (!CheckError(response))
            {
                selectedItem.Files.Clear();
                foreach (FileModel file in ((ListResponseMessage) response).GetData())
                {
                    selectedItem.Files.Add(new FileViewModel(file));
                }
            }
            CurrentProgress.Visibility = Visibility.Hidden;
        }

        private bool CheckError(ResponseMessageBase response)
        {
            if (response == null)
            {
                MessageBox.Show(this, "Error");
                return true;
            }
            if (response.Code == MessageCode.Error)
            {
                MessageBox.Show(this, "Error: " + ((ErrorResponseMessage) response).ErrorCode);
                return true;
            }
            return false;
        }

        private async void UploadBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = ListClients.SelectedItem as HostViewModel;
            if (selectedItem == null) {
                return;
            } 

            CurrentProgress.Visibility = Visibility.Visible;
            var dlg = new SaveFileDialog { CheckFileExists = true, CheckPathExists = true, OverwritePrompt = false };
            bool? result = dlg.ShowDialog(this);
            if (result != true) {
                return;
            }
            string name = Path.GetFileName(dlg.FileName);
            
            var resp = await TaskFactory.CreatePutTask(selectedItem.IpAddress, name, dlg.FileName);
            CheckError(resp);

            CurrentProgress.Visibility = Visibility.Hidden;
        }

        private async void DownloadBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListFiles.SelectedItem == null ||
                ListClients.SelectedItem == null)
            {
                return;
            }

            CurrentProgress.Visibility = Visibility.Visible;
            var selectedHost = (HostViewModel) ListClients.SelectedItem;
            var selectedFile = (FileViewModel) ListFiles.SelectedItem;
            var dlg = new SaveFileDialog {FileName = selectedFile.Name, OverwritePrompt = true, CheckPathExists = true};
            bool? result = dlg.ShowDialog(this);
            if (result != true)
            {
                return;
            }

            string outFile = dlg.FileName;
            ResponseMessageBase resp =
                await TaskFactory.CreateGetTask(selectedHost.IpAddress, selectedFile.Name, outFile);

            CurrentProgress.Visibility = Visibility.Hidden;
            CheckError(resp);
        }

        private async void UdpListener_AnnounceReceived(AnnounceMessage message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                HostViewModel hostViewModel = Hosts.FirstOrDefault(hvm => hvm.IpAddress.Equals(message.Ip));
                if (hostViewModel != null)
                {
                    hostViewModel.Name = message.Name;
                    hostViewModel.FileCount = message.FileCount;
                    hostViewModel.Timestamp = message.Timestamp;
                }
                else
                {
                    Hosts.Add(new HostViewModel {
                        Name = message.Name,
                        FileCount = message.FileCount,
                        IpAddress = message.Ip,
                        Timestamp = message.Timestamp
                    });
                }
            });
        }
    }
}