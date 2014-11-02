using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using FileServer.Client.Annotations;

namespace FileServer.Client.ViewModel {
    public class HostViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<FileViewModel> _files = new ObservableCollection<FileViewModel>();
        private string _name;
        private DateTime _timestamp;
        private uint _fileCount;
        private IPAddress _ipAddress;

        public string Name {
            get { return _name; }
            set {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public DateTime Timestamp {
            get { return _timestamp; }
            set {
                if (value.Equals(_timestamp)) return;
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public uint FileCount {
            get { return _fileCount; }
            set {
                if (value == _fileCount) return;
                _fileCount = value;
                OnPropertyChanged();
            }
        }

        public IPAddress IpAddress {
            get { return _ipAddress; }
            set {
                if (Equals(value, _ipAddress)) return;
                _ipAddress = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FileViewModel> Files {
            get { return _files; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
