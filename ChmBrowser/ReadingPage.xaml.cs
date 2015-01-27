﻿using ChmBrowser.Common;
using ChmCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        public ReadingPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            _lastWebViewUrl = null;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.NavigationStarting += webView_NavigationStarting;
        }

        async void webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.Scheme != "ms-local-stream")
            {
                args.Cancel = true;
                await Windows.System.Launcher.LaunchUriAsync(args.Uri);
            }
            else if (!args.Uri.AbsolutePath.StartsWith("/" + ChmFile.CurrentFile.Key))
            {
                args.Cancel = true;
            }
            else
            {
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
        void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _lastWebViewUrl = args.Uri;
            if (args.Uri.Scheme == "ms-local-stream")
            {
                string path = args.Uri.AbsolutePath.Substring(ChmFile.CurrentFile.Key.Length + 1);
                ChmFile.CurrentFile.SetCurrent(path);
                
                if (!ChmFile.CurrentFile.HasThumbnail)
                {
                    _mutex.WaitOne();
                    try
                    {
                        // Uncomment to capture thumbnail.
                        // await ChmFile.CurrentFile.CreateThumbnailFile(async (s) => await webView.CapturePreviewToStreamAsync(s));
                    }
                    finally
                    {
                        _mutex.ReleaseMutex();
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
            if (!ChmFile.CurrentFile.HasOutline)
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
            UpdateReading();
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ShowAsync();
            base.OnNavigatedFrom(e);
        }

        private void Contents_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ContentPage));
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ChmFile.CurrentFile.SetNext())
            {
                UpdateReading();
            }
        }
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (ChmFile.CurrentFile.SetPrevious())
            {
                UpdateReading();
            }
        }
        private void UpdateReading()
        {
            if (!string.IsNullOrEmpty(ChmFile.CurrentFile.CurrentPath))
            {
                try
                {
                    _mutex.WaitOne();
                    Uri url = webView.BuildLocalStreamUri("MyTag", ChmFile.CurrentFile.Key + "/" +  ChmFile.CurrentFile.CurrentPath);
                    if (_lastWebViewUrl != url)
                    {
                        webView.Stop();
                        webView.NavigateToLocalStreamUri(url, new ChmStreamUriTResolver());
                        ChmFile.CurrentFile.Save();
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
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
            string path = uri.AbsolutePath.Substring(ChmFile.CurrentFile.Key.Length + 1); 
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
            return await Task.Run(async () =>
                {
                    byte[] data = ChmFile.CurrentFile.Chm.GetData(path);
                    using (var memoryStream = new InMemoryRandomAccessStream())
                    {
                        using (var dataWriter = new DataWriter(memoryStream))
                        {
                            dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                            dataWriter.ByteOrder = ByteOrder.LittleEndian;
                            dataWriter.WriteBytes(data);
                            //if (path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    dataWriter.WriteBytes(Encoding.UTF8.GetBytes(
                            //        "<style type='text/css'>pre,ul,table tr td:first-child{font-size: 250%;}</style>")
                            //        );
                            //}
                            await dataWriter.StoreAsync();
                            await dataWriter.FlushAsync();
                            dataWriter.DetachStream();
                        }
                        return memoryStream.GetInputStreamAt(0);
                    }
                });
        }
        //private static bool IsHtml(string path)
        //{
        //    return path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
        //}
    }
}
