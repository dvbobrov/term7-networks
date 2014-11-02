using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using FileServer.Client.Annotations;
using FileServer.Client.Model;

namespace FileServer.Client.ViewModel {
    public class FileViewModel : INotifyPropertyChanged {
        private string _name;
        private string _md5;

        public FileViewModel()
        {
            
        }

        internal FileViewModel(FileModel file)
        {
            _name = file.Name;
            _md5 = ByteArrayToString(file.Md5);
        }
        
        public string Name {
            get { return _name; }
            set {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Md5 {
            get { return _md5; }
            set {
                if (value == _md5) return;
                _md5 = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string ByteArrayToString(byte[] ba) {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
