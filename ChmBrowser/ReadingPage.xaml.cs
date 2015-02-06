/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using ChmBrowser.Common;
using ChmCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ChmBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage : Page
    {
        private Mutex _mutex = new Mutex();
        private Uri _lastWebViewUrl;
        private ChmStreamUriTResolver _uriResolver;
        private ChmFile _chmFile;

        public ReadingPage()
        {
            this.InitializeComponent();
            root.Children.Remove(settingRoot);
            this.NavigationCacheMode = NavigationCacheMode.Required;
            _lastWebViewUrl = null;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.NavigationStarting += webView_NavigationStarting;
            scaleSlider.ValueChanged += scaleSlider_ValueChanged;
            isAutoZoom.Toggled += isAutoZoom_Toggled;
        }

        async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (_chmFile != null)
            {
                await _chmFile.Save();
            }
        }

        async void webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.Scheme != "ms-local-stream")
            {
                args.Cancel = true;
                await Windows.System.Launcher.LaunchUriAsync(args.Uri);
            }
            else
            {
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
        async void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _lastWebViewUrl = args.Uri;
            if (args.Uri.Scheme == "ms-local-stream")
            {
                string path = args.Uri.AbsolutePath + args.Uri.Fragment;
                await _chmFile.SetCurrent(path);

                // Uncomment to capture thumbnail.
                //if (!ChmFile.CurrentFile.HasThumbnail)
                //{
                //    _mutex.WaitOne();
                //    try
                //    {
                //        await ChmFile.CurrentFile.CreateThumbnailFile(async (s) => await webView.CapturePreviewToStreamAsync(s));
                //    }
                //    finally
                //    {
                //        _mutex.ReleaseMutex();
                //    }
                //}
            }
        }

        async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            if (commandBar.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                root.Children.Remove(settingRoot);
                await _chmFile.Save();
            }
            else
            {
                LeavePage();
            }
        }

        private void LeavePage()
        {
            if (Frame.CanGoBack && Frame.BackStack[Frame.BackStack.Count - 1].SourcePageType == typeof(MainPage))
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        // A contract between ReadingPage & ContentPage to feed data to from ReadingPage to ContentPage.
        public static ChmFile SharedChmFile { get; private set; }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SharedChmFile = null;
            if (e.Parameter == null)
            {
                LeavePage();
            }
            if (_chmFile == null || _chmFile.Key != e.Parameter.ToString())
            {
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                _chmFile = await ChmFile.OpenLocalChmFile(e.Parameter.ToString());
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            if (_chmFile == null)
            {
                LeavePage();
            }
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Application.Current.Suspending += Current_Suspending;

            scaleSlider.ValueChanged -= scaleSlider_ValueChanged;
            isAutoZoom.Toggled -= isAutoZoom_Toggled;
            ResetSetting();
            _uriResolver = new ChmStreamUriTResolver(_chmFile, zoomIndicator.Text);
            scaleSlider.ValueChanged += scaleSlider_ValueChanged;
            isAutoZoom.Toggled += isAutoZoom_Toggled;
            
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();

            commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            root.Children.Remove(settingRoot);

            if (!_chmFile.HasOutline)
            {
                foreach(var x in commandBar.PrimaryCommands)
                {
                    (x as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                commandBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
            }
            else
            {
                foreach (var x in commandBar.PrimaryCommands)
                {
                    (x as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                commandBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
            }
            await UpdateReading();
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            Application.Current.Suspending -= Current_Suspending;

            if (e.SourcePageType == typeof(ContentPage))
            {
                SharedChmFile = _chmFile;
                NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            }
            else
            {
                SharedChmFile = null;
                webView.Stop();
                NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
            }
            
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ShowAsync();
            base.OnNavigatedFrom(e);
        }

        private void Contents_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ContentPage));
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (await _chmFile.SetNext())
            {
                await UpdateReading();
            }
        }
        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (await _chmFile.SetPrevious())
            {
                await UpdateReading();
            }
        }
        private async Task UpdateReading()
        {
            if (!string.IsNullOrEmpty(_chmFile.CurrentPath))
            {
                Uri url = webView.BuildLocalStreamUri("MyTag", _chmFile.CurrentPath);
                if (_lastWebViewUrl != url || _lastWebViewUrl.Fragment != url.Fragment)
                {
                    webView.Stop();
                    webView.NavigateToLocalStreamUri(url, _uriResolver);
                    await _chmFile.Save();
                }
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoForward)
            {
                webView.GoForward();
            }
        }
        private void GoSetting_Click(object sender, RoutedEventArgs e)
        {
            root.Children.Add(settingRoot);
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;     
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private async Task SetScale(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value) || value[0] == '-') // treat negtive value as auto.
                {
                    value = "auto";
                }
                _uriResolver.SetScale(value);
                await webView.InvokeScriptAsync("setScale", new string[]{value});
            }
            catch
            {
                // ignore error
            }
        }
        private static string GetStandardScale(ChmFile chmFile, out double v)
        {
            string scale = chmFile.ChmMeta.GetScale();
            if (string.IsNullOrEmpty(scale))
            {
                v = 1;
                return "auto";
            }
            else if (scale[0] == '-')
            {
                if (!double.TryParse(scale.Substring(1), out v))
                {
                    v = 1;
                }
                return "auto";
            }
            else
            {
                if (!double.TryParse(scale, out v))
                {
                    v = 1;
                    return "auto";
                }
                return (v * 100).ToString("0") + "%";
            }
        }
        private void ResetSetting()
        {
            double v;
            string scale = GetStandardScale(_chmFile, out v);
            
            if (scale == "auto")
            {
                isAutoZoom.IsOn = true;
                scaleSlider.IsEnabled = false;
                scaleSlider.Value = v;
            }
            else
            {
                isAutoZoom.IsOn = false;
                scaleSlider.IsEnabled = true;
                scaleSlider.Value = v;
            }
            zoomIndicator.Text = scale;
        }
        private async void isAutoZoom_Toggled(object sender, RoutedEventArgs e)
        {
            if (isAutoZoom.IsOn)
            {
                scaleSlider.IsEnabled = false;
                _chmFile.ChmMeta.SetScale("-" + scaleSlider.Value.ToString());
                zoomIndicator.Text = "auto";
            }
            else
            {
                scaleSlider.IsEnabled = true;
                _chmFile.ChmMeta.SetScale(scaleSlider.Value.ToString());
                zoomIndicator.Text = (scaleSlider.Value * 100).ToString("0") + "%";
            }
            await SetScale(zoomIndicator.Text);
            await _chmFile.Save();
        }

        private async void scaleSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (scaleSlider.IsEnabled)
            {
                _chmFile.ChmMeta.SetScale(scaleSlider.Value.ToString());
                zoomIndicator.Text = (scaleSlider.Value * 100).ToString("0") + "%";
                await SetScale(zoomIndicator.Text);
                await _chmFile.Save();
            }
        }
    }

    /// <summary>
    /// Sample URI resolver object for use with NavigateToLocalStreamUri
    /// This sample uses the local storage of the package as an example of how to write a resolver.
    /// The object needs to implement the IUriToStreamResolver interface
    /// 
    /// Note: If you really want to browse the package content, the ms-appx-web:// protocol demonstrated
    /// in scenario 3, is the simpler way to do that.
    /// </summary>
    public sealed class ChmStreamUriTResolver : IUriToStreamResolver
    {
        private volatile string _scale;
        private WeakReference<ChmFile> _chmFile;

        public ChmStreamUriTResolver(ChmFile chmFile, string scale = "auto")
        {
            _chmFile = new WeakReference<ChmFile>(chmFile);
            _scale = scale;
        }

        public void SetScale(string scale)
        {
            _scale = scale;
        }

        private byte[] GetInjectedContent()
        {
            // *{word-wrap:break-word;} pre {white-space:pre-wrap;}
            return Encoding.UTF8.GetBytes
            ("<style type='text/css'>*{-ms-text-size-adjust:"+ _scale + ";}</style>" +
            "<script type='text/javascript'>function setScale(scale){document.styleSheets[document.styleSheets.length - 1].rules[0].style.cssText='-ms-text-size-adjust:'+scale +';';" + 
            "var i,frames;frames=document.getElementsByTagName('iframe');for(i=0;i<frames.length; ++i){if(frames[i].contentWindow&&frames[i].contentWindow.setScale){frames[i].contentWindow.setScale(scale);}}" + 
            "}</script>"
            );
        }

        /// <summary>
        /// The entry point for resolving a Uri to a stream.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new Exception();
            }
            string path = uri.AbsolutePath; 
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Stream Requested: {0}", uri.ToString()));
            }
            // Because of the signature of the this method, it can't use await, so we 
            // call into a seperate helper method that can use the C# await pattern.
            return getContent(path).AsAsyncOperation();
        }
        /// <summary>
        /// Helper that cracks the path and resolves the Uri
        /// Uses the C# await pattern to coordinate async operations
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<IInputStream> getContent(string path)
        {
            ChmFile obj;
            if (!_chmFile.TryGetTarget(out obj))
            {
                throw new Exception();
            }
            byte[] data = await obj.GetData(path);
            if (data == null || data.Length == 0)
            {
                throw new Exception();
            }
            using (var memoryStream = new InMemoryRandomAccessStream())
            {
                using (var dataWriter = new DataWriter(memoryStream))
                {
                    dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    dataWriter.ByteOrder = ByteOrder.LittleEndian;
                    dataWriter.WriteBytes(data);
                    if (IsHtml(path))
                    {
                        dataWriter.WriteBytes(GetInjectedContent());
                    }
                    await dataWriter.StoreAsync();
                    await dataWriter.FlushAsync();
                    dataWriter.DetachStream();
                }
                return memoryStream.GetInputStreamAt(0);
            }
        }
        private static bool IsHtml(string path)
        {
            return path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) 
                || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
        }
    }
}
