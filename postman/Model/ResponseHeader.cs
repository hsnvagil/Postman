using System.ComponentModel;
using System.Runtime.CompilerServices;
using postman.Annotations;

namespace postman.Model {
    public class ResponseHeader : INotifyPropertyChanged {
        private string _key;
        private string _value;

        public string Key {
            get => _key;
            set {
                if (value == _key) return;
                _key = value;
                OnPropertyChanged();
            }
        }

        public string Value {
            get => _value;
            set {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}