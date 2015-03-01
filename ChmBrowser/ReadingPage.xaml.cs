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
using Windows.UI.Xaml.Media.Animation;
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

        private int _pressDownX;
        private DateTime _pressDownTime;
        private double _swipeThreshouldRatio = 0.5;
        private double _swipeThreshouldInPixel = 40;
        private double _swipeThreshouldInMS = 300;

        public ReadingPage()
        {
            this.InitializeComponent();
            HideSetting();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.Loaded += ReadingPage_Loaded;
            _lastWebViewUrl = null;
            
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.NavigationStarting += webView_NavigationStarting;
            webView.ScriptNotify += webView_ScriptNotify;
            webView.ContentLoading += webView_ContentLoading;

            AttachEventToSetting();
        }

        private void AttachEventToSetting()
        {
            scaleSlider.ValueChanged += scaleSlider_ValueChanged;
            isAutoZoom.Toggled += isAutoZoom_Toggled;
            isNightMode.Toggled += isNightMode_Toggled;
            isWrapMode.Toggled += isWrapMode_Toggled;
            isSwipeMode.Toggled += isSwipeMode_Toggled;
        }

        private void DetachEventFromSetting()
        {
            scaleSlider.ValueChanged -= scaleSlider_ValueChanged;
            isAutoZoom.Toggled -= isAutoZoom_Toggled;
            isNightMode.Toggled -= isNightMode_Toggled;
            isWrapMode.Toggled -= isWrapMode_Toggled;
            isSwipeMode.Toggled -= isSwipeMode_Toggled;
        }

        void ReadingPage_Loaded(object sender, RoutedEventArgs e)
        {
            // work around for binding in command bar
            commandBar.DataContext = this;
            foreach (var x in commandBar.PrimaryCommands.Concat(commandBar.SecondaryCommands))
            {
                var button = x as AppBarButton;
                var binding = button.GetBindingExpression(AppBarButton.IsEnabledProperty);
                if (binding != null && !string.IsNullOrEmpty(binding.ParentBinding.ElementName))
                {
                    button.SetBinding(AppBarButton.IsEnabledProperty, new Binding()
                        {
                            Source = FindName(binding.ParentBinding.ElementName),
                            Path = binding.ParentBinding.Path
                        });
                }
            }
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
        void webView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            webViewTranslate.X = 0;
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
        
        async void webView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            if (!_chmFile.HasOutline)
            {
                return;
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("mm:ss:fff") + ":" + e.Value);
            }

            string[] data = e.Value.Split(':');
            int x = Convert.ToInt32(data[1]);
            if (Windows.Graphics.Display.DisplayInformation.GetForCurrentView().CurrentOrientation == Windows.Graphics.Display.DisplayOrientations.Landscape
                || Windows.Graphics.Display.DisplayInformation.GetForCurrentView().CurrentOrientation == Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped)
            {
                x = Convert.ToInt32(data[2]);
            }
            char type = data[0][0];
            switch (type)
            {
                case 'd':
                    {
                        _pressDownX = x;
                        _pressDownTime = DateTime.Now;
                        break;
                    }
                case 'u':
                    {
                        double diffInMS = (DateTime.Now - _pressDownTime).TotalMilliseconds;
                        if ((x - _pressDownX >= _swipeThreshouldRatio * webView.ActualWidth 
                            || (x - _pressDownX >= _swipeThreshouldInPixel && diffInMS < _swipeThreshouldInMS))
                            && await _chmFile.CanGoPrevious())
                        {
                            CreateRightLeaveStoryboard().Begin();
                        }
                        else if ((_pressDownX - x >= _swipeThreshouldRatio * webView.ActualWidth
                            || (_pressDownX - x >= _swipeThreshouldInPixel && diffInMS < _swipeThreshouldInMS))
                            && await _chmFile.CanGoNext())
                        {
                            CreateLeftLeaveStoryboard().Begin();
                        }
                        else
                        {
                            webViewBack.Begin();
                        }
                        break; 
                    }
                case 'm':
                    {
                        webViewTranslate.X = x - _pressDownX;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        private Storyboard CreateRightLeaveStoryboard()
        {
            Storyboard s = new Storyboard();
            DoubleAnimation da = new DoubleAnimation();
            Storyboard.SetTarget(da, webViewTranslate);
            Storyboard.SetTargetProperty(da, "X");
            da.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 100));
            da.To = webView.ActualWidth;
            s.Children.Add(da);
            s.Completed += (a, e) => Previous_Click(null, null);
            return s;            
        }

        private Storyboard CreateLeftLeaveStoryboard()
        {
            Storyboard s = new Storyboard();
            DoubleAnimation da = new DoubleAnimation();
            Storyboard.SetTarget(da, webViewTranslate);
            Storyboard.SetTargetProperty(da, "X");
            da.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 100));
            da.To = -webView.ActualWidth;
            s.Children.Add(da);
            s.Completed += (a, e) => Next_Click(null, null);
            return s;
        }

        async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            if (commandBar.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                HideSetting();
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
            webViewTranslate.X = 0;
            SharedChmFile = null;
            if (e.Parameter == null)
            {
                LeavePage();
            }
            if (_chmFile == null || _chmFile.Key != e.Parameter.ToString())
            {
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                _chmFile = await ChmFile.OpenLocalChmFile(e.Parameter.ToString());
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            if (_chmFile == null)
            {
                LeavePage();
            }
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Application.Current.Suspending += Current_Suspending;

            DetachEventFromSetting();
            ResetSetting();
            _uriResolver = new ChmStreamUriTResolver(_chmFile, zoomIndicator.Text, isWrapMode.IsOn, isNightMode.IsOn, isSwipeMode.IsOn);
            AttachEventToSetting();
            
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();

            commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            HideSetting();

            int mask = _chmFile.HasOutline ? 1 : 2;
            foreach (var x in commandBar.PrimaryCommands.Concat(commandBar.SecondaryCommands))
            {
                int tag = Convert.ToInt32((x as AppBarButton).Tag);
                (x as AppBarButton).Visibility = ((tag & mask) != 0) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
            if (_chmFile.HasOutline)
            {
                if (!settingRoot.Items.Contains(pageControl))
                {
                    settingRoot.Items.Add(pageControl);
                }
            }
            else
            {
                if (settingRoot.Items.Contains(pageControl))
                {
                    settingRoot.Items.Remove(pageControl);
                }
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
            if (_chmFile != null)
            {
                await _chmFile.Save();
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
                await UpdateReading(true);
            }
            else
            {
                webViewTranslate.X = 0;
            }
        }
        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (await _chmFile.SetPrevious())
            {
                await UpdateReading(true);
            }
            else
            {
                webViewTranslate.X = 0;
            }
        }
        private async Task UpdateReading(bool force = false)
        {
            if (!string.IsNullOrEmpty(_chmFile.CurrentPath))
            {
                Uri url = webView.BuildLocalStreamUri("MyTag", _chmFile.CurrentPath);
                if (force || _lastWebViewUrl != url || _lastWebViewUrl.Fragment != url.Fragment)
                {
                    webView.NavigateToLocalStreamUri(url, _uriResolver);
                    await _chmFile.Save();
                }
                else
                {
                    webViewTranslate.X = 0;
                }
            }
            else
            {
                webViewTranslate.X = 0;
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
        private async void Home_Click(object sender, RoutedEventArgs e)
        {
            await _chmFile.SetCurrent(_chmFile.Home);
            await UpdateReading(true);
        }
        
        private void GoSetting_Click(object sender, RoutedEventArgs e)
        {
            ShowSetting();
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;     
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private void ShowSetting()
        {
            //settingRoot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            root.Children.Add(settingRoot);
            settingRoot.UpdateLayout();
        }
        private void HideSetting()
        {
            //settingRoot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            root.Children.Remove(settingRoot);
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
                await webView.InvokeScriptAsync(ChmStreamUriTResolver.SetScaleFuncName, new string[]{value});
            }
            catch
            {
                // ignore error
            }
        }
        private async Task SetNightMode(bool on)
        {
            try
            {
                _uriResolver.SetIsNightMode(on);
                await webView.InvokeScriptAsync(ChmStreamUriTResolver.SetNightModeFuncName, new string[] { on?"on":"off"});
            }
            catch
            {
                // ignore error
            }
        }
        private async Task SetWrapMode(bool on)
        {
            try
            {
                _uriResolver.SetIsWrapMode(on);
                await webView.InvokeScriptAsync(ChmStreamUriTResolver.SetWrapModeFuncName, new string[] { on ? "on" : "off" });
            }
            catch
            {
                // ignore error
            }
        }
        private async Task SetSwipeMode(bool on)
        {
            try
            {
                _uriResolver.SetIsSwipeMode(on);
                await webView.InvokeScriptAsync(ChmStreamUriTResolver.SetSwipeModeFuncName, new string[] { on ? "on" : "off" });
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
            isNightMode.IsOn = _chmFile.ChmMeta.GetIsNightMode();
            isWrapMode.IsOn = _chmFile.ChmMeta.GetIsWrapMode();
            isSwipeMode.IsOn = _chmFile.ChmMeta.GetIsSwipeMode();
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


        async void isWrapMode_Toggled(object sender, RoutedEventArgs e)
        {
            await SetWrapMode(isWrapMode.IsOn);
            _chmFile.ChmMeta.SetIsWrapMode(isWrapMode.IsOn);
            await _chmFile.Save();
        }

        async void isNightMode_Toggled(object sender, RoutedEventArgs e)
        {
            await SetNightMode(isNightMode.IsOn);
            _chmFile.ChmMeta.SetIsNightMode(isNightMode.IsOn);
            await _chmFile.Save();
        }
        async void isSwipeMode_Toggled(object sender, RoutedEventArgs e)
        {
            await SetSwipeMode(isSwipeMode.IsOn);
            _chmFile.ChmMeta.SetIsSWipeMode(isSwipeMode.IsOn);
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
        public const string SetScaleFuncName = "DE3A90B588894290AEDD485D8FE1E6AD_setScale";
        public const string SetWrapModeFuncName = "DE3A90B588894290AEDD485D8FE1E6AD_setWrapMode";
        public const string SetNightModeFuncName = "DE3A90B588894290AEDD485D8FE1E6AD_setNightMode";
        public const string SetSwipeModeFuncName = "DE3A90B588894290AEDD485D8FE1E6AD_setSwipeMode";

        private const string SwipeModeCss = " html {touch-action:pan-y pinch-zoom double-tap-zoom;}";
        private const string WrapModeCss = "*{word-wrap:break-word !important;} pre {white-space:pre-wrap !important;}";
        private const string NightModeCss = "* {background-color:black !important;color:white !important;} a {text-decoration: underline !important;}";
        private const string NotifyScript = 
            "window.addEventListener('MSPointerDown',function(event){window.external.notify('d:'+event.screenX+':'+event.screenY);});" +
            "window.addEventListener('MSPointerUp',function(event){window.external.notify('u:'+event.screenX+':'+event.screenY);});" +
            "window.addEventListener('MSPointerMove',function(event){window.external.notify('m:'+event.screenX+':'+event.screenY);});";

        private volatile string _scale;
        private volatile bool _isWrapMode;
        private volatile bool _isNightMode;
        private volatile bool _isSwipeMode;

        private WeakReference<ChmFile> _chmFile;

        public ChmStreamUriTResolver(ChmFile chmFile, string scale, bool isWrap, bool isNight, bool isSwipe)
        {
            _chmFile = new WeakReference<ChmFile>(chmFile);
            _scale = scale;
            _isWrapMode = isWrap;
            _isNightMode = isNight;
            _isSwipeMode = isSwipe;
        }

        public void SetIsWrapMode(bool isWrap)
        {
            _isWrapMode = isWrap;
        }

        public void SetIsNightMode(bool isNight)
        {
            _isNightMode = isNight;
        }

        public void SetIsSwipeMode(bool isSwipe)
        {
            _isSwipeMode = isSwipe;
        }

        public void SetScale(string scale)
        {
            _scale = scale;
        }

        private byte[] GetInjectedContent()
        {
            string injectedWrapModeCss = string.Empty;
            if (_isWrapMode)
            {
                injectedWrapModeCss = WrapModeCss;
            }
            string injectedNightModeCss = string.Empty;
            if (_isNightMode)
            {
                injectedNightModeCss = NightModeCss;
            }
            string injectedSwipeModeCss = string.Empty;
            if (_isSwipeMode)
            {
                injectedSwipeModeCss = SwipeModeCss;
            }
            return Encoding.UTF8.GetBytes
            (
            "<style type='text/css'>" + injectedSwipeModeCss + "</style>" +  // styleSheets for swipe mode.
            "<style type='text/css'>" + injectedNightModeCss + "</style>" +  // styleSheets for night mode.
            "<style type='text/css'>" + injectedWrapModeCss + "</style>" +  // styleSheets for word wrapping.
            "<style type='text/css'>*{-ms-text-size-adjust:"+ _scale + ";}</style>" +
            "<script type='text/javascript'>" +
            "function DE3A90B588894290AEDD485D8FE1E6AD_setScale(scale){document.styleSheets[document.styleSheets.length - 1].rules[0].style.cssText='-ms-text-size-adjust:'+scale +';';" +
            "var i,frames;frames=document.getElementsByTagName('iframe');for(i=0;i<frames.length; ++i){if(frames[i].contentWindow&&frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setScale){frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setScale(scale);}}" + 
            "}" +
            "function DE3A90B588894290AEDD485D8FE1E6AD_setWrapMode(flag){var wrapsheet=document.styleSheets[document.styleSheets.length - 2]; if (flag != 'on') wrapsheet.cssText=''; else wrapsheet.cssText='"+ WrapModeCss + "';"  +
            "var i,frames;frames=document.getElementsByTagName('iframe');for(i=0;i<frames.length; ++i){if(frames[i].contentWindow&&frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setWrapMode){frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setWrapMode(flag);}}" + 
            "}" +
            "function DE3A90B588894290AEDD485D8FE1E6AD_setNightMode(flag){var nightsheet=document.styleSheets[document.styleSheets.length - 3]; if (flag != 'on') nightsheet.cssText=''; else nightsheet.cssText='" + NightModeCss + "';" +
            "var i,frames;frames=document.getElementsByTagName('iframe');for(i=0;i<frames.length; ++i){if(frames[i].contentWindow&&frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setNightMode){frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setNightMode(flag);}}" +
            "}" +
            "function DE3A90B588894290AEDD485D8FE1E6AD_setSwipeMode(flag){var swipesheet=document.styleSheets[document.styleSheets.length - 4]; if (flag != 'on') swipesheet.cssText=''; else swipesheet.cssText='" + SwipeModeCss + "';" +
            "var i,frames;frames=document.getElementsByTagName('iframe');for(i=0;i<frames.length; ++i){if(frames[i].contentWindow&&frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setSwipeMode){frames[i].contentWindow.DE3A90B588894290AEDD485D8FE1E6AD_setSwipeMode(flag);}}" +
            "}" +
            NotifyScript + 
            "</script>"
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
