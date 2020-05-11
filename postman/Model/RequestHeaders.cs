using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using postman.Annotations;

namespace postman.Model {
    public class RequestHeaders : INotifyPropertyChanged {
        private bool _active;
        private string _key;
        private string _value;
        private Visibility _visibility = Visibility.Hidden;

        public bool Active {
            get => _active;
            set {
                if (value == _active) return;
                _active = value;
                OnPropertyChanged();
            }
        }

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

        public Visibility Visibility {
            get => _visibility;
            set {
                if (value == _visibility) return;
                _visibility = value;
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