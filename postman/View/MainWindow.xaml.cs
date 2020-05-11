using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace postman.View {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void EventSetter_OnHandlerList(object sender, KeyboardFocusChangedEventArgs e) {
            var item = (ListViewItem) sender;
            item.IsSelected = true;
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (e.Text == "&" || e.Text == "=") e.Handled = true;
        }
    }
}