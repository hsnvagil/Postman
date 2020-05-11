using System;
using System.Windows;
using System.Windows.Controls;

namespace postman {
    public static class WebBrowserHelper {
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.RegisterAttached("Url", typeof(string), typeof(WebBrowserHelper),
                                                new PropertyMetadata(OnUrlChanged));

        public static string GetUrl(DependencyObject dependencyObject) {
            return (string) dependencyObject.GetValue(UrlProperty);
        }

        public static void SetUrl(DependencyObject dependencyObject, string body) {
            dependencyObject.SetValue(UrlProperty, body);
        }

        private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is WebBrowser browser)) return;

            Uri uri = null;

            switch (e.NewValue) {
                case string s: {
                    var uriString = s;

                    uri = string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
                    break;
                }
                case Uri value:
                    uri = value;
                    break;
            }

            browser.Source = uri;
        }
    }
}