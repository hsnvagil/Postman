using System;
using System.Windows;
using System.Windows.Interactivity;
using ICSharpCode.AvalonEdit;

namespace postman {
    public sealed class AvalonEditBehaviour : Behavior<TextEditor> {
        public static readonly DependencyProperty GiveMeTheTextProperty =
            DependencyProperty.Register("GiveMeTheText", typeof(string), typeof(AvalonEditBehaviour),
                                        new FrameworkPropertyMetadata(default(string),
                                                                      FrameworkPropertyMetadataOptions
                                                                          .BindsTwoWayByDefault,
                                                                      PropertyChangedCallback));

        public string GiveMeTheText {
            get => (string) GetValue(GiveMeTheTextProperty);
            set => SetValue(GiveMeTheTextProperty, value);
        }

        protected override void OnAttached() {
            base.OnAttached();
            if (AssociatedObject != null) AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            if (AssociatedObject != null) AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
        }

        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs) {
            if (sender is TextEditor textEditor)
                if (textEditor.Document != null)
                    GiveMeTheText = textEditor.Document.Text;
        }

        private static void PropertyChangedCallback(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
            var behavior = dependencyObject as AvalonEditBehaviour;
            var editor = behavior?.AssociatedObject;
            if (editor?.Document != null) {
                var caretOffset = editor.CaretOffset;
                editor.Document.Text = dependencyPropertyChangedEventArgs.NewValue.ToString();
                editor.CaretOffset = caretOffset;
            }
        }
    }
}