using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using postman.Model;
using Serilog;

namespace postman.ViewModel {
    public class MainViewModel : ViewModelBase {
        #region Constructor

        public MainViewModel() {
            Initialize();
        }

        #endregion

        #region Field

        private string _webUrl;
        private HttpClient _client;
        private bool _urlTextChangedActive = true;
        private HttpResponseMessage _httpResponseMessage;
        private HttpRequestMessage _httpRequestMessage;
        private RequestHeaders _requestHeader;
        private Param _requestParam;
        private bool _mainGridEnable;
        private Visibility _informationVisibility;
        private bool _responsePanelVisibility;
        private string _statusCode;
        private string _time;
        private string _responseContentSize;
        private string _requestText;
        private string _method;
        private string _content;
        private string _rawText;
        private string _previewText;
        private string _previewTextFormat;
        private string _queryText;
        private string _url;
        private string _responseInfoText;

        #endregion

        #region Property

        public string WebUrl {
            get => _webUrl;
            set => Set(ref _webUrl, value);
        }

        public bool MainGridEnable {
            get => _mainGridEnable;
            set => Set(ref _mainGridEnable, value);
        }

        public string ResponseInfoText {
            get => _responseInfoText;
            set => Set(ref _responseInfoText, value);
        }

        public bool ResponsePanelVisibility {
            get => _responsePanelVisibility;
            set => Set(ref _responsePanelVisibility, value);
        }

        public string StatusCode {
            get => _statusCode;
            set => Set(ref _statusCode, value);
        }

        public string Time {
            get => _time;
            set => Set(ref _time, value);
        }

        public string ResponseContentSize {
            get => _responseContentSize;
            set => Set(ref _responseContentSize, value);
        }

        public Visibility InformationVisibility {
            get => _informationVisibility;
            set => Set(ref _informationVisibility, value);
        }

        public string PreviewText {
            get => _previewText;
            set => Set(ref _previewText, value);
        }

        public string PreviewTextFormat {
            get => _previewTextFormat;
            set => Set(ref _previewTextFormat, value);
        }

        public string RawText {
            get => _rawText;
            set => Set(ref _rawText, value);
        }

        public Param RequestParam {
            get => _requestParam;
            set => Set(ref _requestParam, value);
        }

        public RequestHeaders RequestHeader {
            get => _requestHeader;
            set => Set(ref _requestHeader, value);
        }

        public string Content {
            get => _content;
            set => Set(ref _content, value);
        }

        public string Method {
            get => _method;
            set => Set(ref _method, value);
        }

        public string RequestText {
            get => _requestText;
            set => Set(ref _requestText, value);
        }

        public string Url {
            get => _url;
            set => Set(ref _url, value);
        }

        #endregion

        #region Collections

        public ObservableCollection<string> MethodList { get; set; }
        public ObservableCollection<string> ContentList { get; set; }
        public ObservableCollection<Param> RequestParamsList { get; set; }
        public ObservableCollection<RequestHeaders> RequestHeadersList { get; set; }
        public ObservableCollection<ResponseHeader> ResponseHeadersList { get; set; }

        #endregion

        #region Command

        public RelayCommand SaveResponseCommand => new RelayCommand(SaveResponse);
        public RelayCommand RequestParamTextChanged => new RelayCommand(ParamTextChanged);
        public RelayCommand UrlChanged => new RelayCommand(UrlTextChanged);
        public RelayCommand CheckedCommand => new RelayCommand(SetQueryText);

        public RelayCommand RemoveRequestHeader =>
            new RelayCommand(() => { RequestHeadersList.Remove(RequestHeader); });

        public RelayCommand RemoveRequestParam =>
            new RelayCommand(() => {
                RequestParamsList.Remove(RequestParam);
                SetQueryText();
            });

        public RelayCommand SendRequestCommand => new RelayCommand(SendRequest, () => true);
        public RelayCommand RequestHeaderTextChanged => new RelayCommand(HeaderTextChanged);

        #endregion

        #region Method

        private void Initialize() {
            RequestParamsList = new ObservableCollection<Param> {new Param {Active = false}};
            RequestHeadersList = new ObservableCollection<RequestHeaders> {new RequestHeaders {Active = false}};
            ResponseHeadersList = new ObservableCollection<ResponseHeader>();
            MethodList = new ObservableCollection<string> {"GET", "POST", "PUT", "DELETE"};
            ContentList = new ObservableCollection<string> {"Text", "JSON", "XML"};
            Method = MethodList.First();
            Content = ContentList.First();
            Url = string.Empty;
            _queryText = string.Empty;
            InformationVisibility = Visibility.Hidden;
            ResponseInfoText = "Hit send to get a response";
            MainGridEnable = true;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        private void UrlTextChanged() {
            if (_urlTextChangedActive) {
                if (!Url.Contains('?')) {
                    RequestParamsList.Clear();
                    RequestParamsList.Add(new Param());
                    return;
                }

                _queryText = Url.Substring(Url.IndexOf('?'));
                SetParamList(_queryText);
            }
        }

        private string HTMLBeautifier(string text) {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(text);
            StringWriter writer;
            using (writer = new StringWriter()) {
                document.ToHtml(writer, new PrettyMarkupFormatter {
                    Indentation = "\t",
                    NewLine = "\n"
                });
            }

            return writer.ToString();
        }

        private string XMLBeautifier(string text) {
            var result = "";

            var mStream = new MemoryStream();
            var writer = new XmlTextWriter(mStream, Encoding.Unicode);
            var document = new XmlDocument();

            try {
                document.LoadXml(text);

                writer.Formatting = Formatting.Indented;

                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();
                mStream.Position = 0;

                var sReader = new StreamReader(mStream);

                var formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            } catch (XmlException) { }

            mStream.Close();
            writer.Close();

            return result;
        }

        private void SetQueryText() {
            _urlTextChangedActive = false;
            if (_queryText.Length > 0) Url = Url?.Replace(_queryText, "");

            _queryText = string.Empty;
            var i = 0;
            foreach (var t in RequestParamsList)
                if (t.Active) {
                    if (i == 0)
                        _queryText += "?";
                    else if (i != RequestParamsList.Count - 1) _queryText += "&";

                    _queryText += t.Key + "=" + t.Value;
                    i++;
                }

            Url += _queryText;
            _urlTextChangedActive = true;
        }

        private void SetParamList(string query) {
            RequestParamsList.Clear();
            if (query.Length < 1) RequestParamsList.Add(new Param {Active = true, Visibility = Visibility.Visible});
            var text = query.Contains("&")
                ? query.Substring(query.IndexOf("?") + 1, query.IndexOf("&") - 1)
                : query.Substring(query.IndexOf("?") + 1);

            string key;
            var value = string.Empty;
            if (text.Contains("=")) {
                key = text.Substring(0, query.IndexOf("=") - 1);
                value = text.Substring(text.IndexOf("=") + 1);
            } else {
                key = text;
            }

            RequestParamsList.Add(new Param {
                Active = true,
                Visibility = Visibility.Visible,
                Key = key,
                Value = value
            });

            while (query.Contains("&")) {
                query = query.Substring(query.IndexOf("&") + 1);
                value = string.Empty;

                text = query.Contains("&") ? query.Substring(0, query.IndexOf("&")) : query;
                if (text.Contains("=")) {
                    key = text.Substring(0, query.IndexOf("="));
                    value = text.Substring(text.IndexOf("=") + 1);
                } else {
                    key = text;
                }

                RequestParamsList.Add(new Param {
                    Active = true,
                    Visibility = Visibility.Visible,
                    Key = key,
                    Value = value
                });
            }

            RequestParamsList.Add(new Param());
        }

        private void ParamTextChanged() {
            if (RequestParam.Visibility == Visibility.Hidden) {
                RequestParam.Visibility = Visibility.Visible;
                RequestParam.Active = true;
                RequestParamsList.Add(new Param());
            }

            SetQueryText();
        }

        private void HeaderTextChanged() {
            if (RequestHeader.Visibility == Visibility.Hidden) {
                RequestHeader.Active = true;
                RequestHeader.Visibility = Visibility.Visible;
                RequestHeadersList.Add(new RequestHeaders());
            }
        }

        private string LengthConverter(long? length) {
            if (length > 1048576) {
                length /= 1048576;
                return $"{length:0.00} MB";
            }

            if (length > 1024) {
                length /= 1024;
                return $"{length:0.00} KB";
            }

            return $"{length:0.00} bytes";
        }

        private HttpMethod GetHttpMethod(string type) {
            var methodType = type switch {
                "get" => HttpMethod.Get,
                "put" => HttpMethod.Put,
                "post" => HttpMethod.Post,
                "delete" => HttpMethod.Delete,
                _ => null
            };

            return methodType;
        }

        private string GetMediaType(string type) {
            var mediaType = type switch {
                "text" => "text/plain",
                "xml" => "application/xml",
                "json" => "application/json",
                _ => string.Empty
            };

            return mediaType;
        }

        private Dictionary<string, IEnumerable<string>>
            GetHeaders() {
            var headers = new Dictionary<string, IEnumerable<string>>();
            foreach (var header in RequestHeadersList.Where(c => c.Active)) {
                if (headers.ContainsKey(header.Key)) {
                    headers[header.Key].ToList().Add(header.Value);
                    continue;
                }

                headers.Add(header.Key, new List<string> {header.Value});
            }

            return headers;
        }

        private void SaveResponse() {
            var saveDialog = new SaveFileDialog {FileName = $"response.{PreviewTextFormat.ToLower()}"};
            if (saveDialog.ShowDialog() == true) File.WriteAllText(saveDialog.FileName, PreviewText);
        }

        private void SendRequest() {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                ResponseInfoText = "You're not connected.. Checking your network cables,modem and routers";
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var completed = false;
            MainGridEnable = false;
            ResponseInfoText = "Sending request..";
            HttpMethod methodType = null;
            Task.Run(() => {
                _client = new HttpClient();
                methodType = GetHttpMethod(Method.ToLower());
                var headers = GetHeaders();
                var content = RequestText;
                var mediaType = GetMediaType(Content.ToLower());

                _httpRequestMessage = new HttpRequestMessage(methodType, Url);

                if (!string.IsNullOrEmpty(content) && (methodType == HttpMethod.Post || methodType == HttpMethod.Put))
                    _httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, mediaType);

                foreach (var header in headers) _httpRequestMessage.Headers.Add(header.Key, header.Value);

                _httpResponseMessage = null;
                try {
                    var task = _client.SendAsync(_httpRequestMessage);
                    _httpResponseMessage = task.Result;
                    task.ContinueWith(t => {
                        stopWatch.Stop();
                        Time = $"{stopWatch.ElapsedMilliseconds} ms";
                    });
                } catch (Exception e) {
                    Log.Error(new HttpRequestException(e.Message), $"THERE WAS AN ERROR CONNECTING TO {Url}.");
                    ResponseInfoText = $"There was an error connecting to {Url}.";
                    ResponsePanelVisibility = false;
                    return;
                }

                if (_httpResponseMessage != null) {
                    WebUrl = Url;
                    ResponsePanelVisibility = true;
                    InformationVisibility = Visibility.Visible;
                    var b = _httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                    RawText = Encoding.Default.GetString(b);
                    var statusCodeNumber = (int) _httpResponseMessage.StatusCode;
                    StatusCode = $"{statusCodeNumber} {_httpResponseMessage.StatusCode}";
                    ResponseContentSize = LengthConverter(_httpResponseMessage.Content?.Headers.ContentLength);
                    mediaType = _httpResponseMessage.Content.Headers.ContentType?.MediaType;
                    if (mediaType != null)
                        switch (mediaType) {
                            case "text/html":
                                PreviewTextFormat = "HTML";
                                PreviewText = HTMLBeautifier(RawText);
                                break;
                            case "application/xml":
                                PreviewTextFormat = "XML";
                                PreviewText = XMLBeautifier(RawText);
                                break;
                            case "application/json":
                                PreviewTextFormat = "Json";
                                PreviewText = JToken.Parse(RawText)
                                    .ToString((Newtonsoft.Json.Formatting) Formatting.Indented);
                                break;
                            default:
                                PreviewText = RawText;
                                break;
                        }


                    Application.Current.Dispatcher.Invoke(() => ResponseHeadersList.Clear());
                    var h = _httpResponseMessage.Headers;
                    foreach (var httpResponseHeader in h) {
                        var subs = false;
                        var key = httpResponseHeader.Key;
                        var val = string.Empty;
                        foreach (var value in httpResponseHeader.Value) {
                            val += value;
                            if (httpResponseHeader.Value.Count() <= 1) continue;
                            val += " , ";
                            subs = true;
                        }

                        if (subs) val = val.Substring(0, val.Length - 2);

                        Application.Current.Dispatcher.Invoke(() => {
                            ResponseHeadersList.Add(new ResponseHeader {
                                Key = key,
                                Value = val
                            });
                        });
                    }

                    completed = true;
                }
            }).ContinueWith(task => {
                MainGridEnable = true;
                if (completed) Log.Debug($"SEND {methodType} REQUEST TO {Url} | STATUS CODE {StatusCode}");
            });
        }

        #endregion
    }
}