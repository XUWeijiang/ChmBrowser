/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HtmlAgilityPack;
using ChmBrowser.Common;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ChmBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        private WeakReference<ChmFile> _chmWeak = new WeakReference<ChmFile>(null);
        private IList<string> _files = new List<string>();
        private ObservableCollection<HtmlPageInfo> _searchResult = new ObservableCollection<HtmlPageInfo>();
        private Task _readingCotentsTask;
        private NavigationMode _mode;

        private volatile bool _cancelToken = false;
        private Task _searchTask = null;

        public SearchPage()
        {
            this.InitializeComponent();
            this.Loaded += SearchPage_Loaded;
            
            this.DataContext = _searchResult;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
        }

        void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mode == NavigationMode.Back)
            {
                ItemListView.Focus(FocusState.Programmatic);
            }
            else
            {
                textBox.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ReadingPage.SharedChmFile == null || ReadingPage.SharedChmFile.Chm == null)
            {
                Frame.GoBack();
                return;
            }
            _mode = e.NavigationMode;
            ChmFile obj;
            HideProgress();
            if (_chmWeak.TryGetTarget(out obj) && obj == ReadingPage.SharedChmFile)
            {
                // do nothing
                await CancelSearchTask();
            }
            else
            {
                _chmWeak = new WeakReference<ChmFile>(ReadingPage.SharedChmFile);
                _searchResult.Clear();
                _files.Clear();
                _readingCotentsTask = ReadingPage.SharedChmFile.EnumerateFiles().ContinueWith(t=>
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                        ()=>_files=t.Result.ToList()).AsTask().Wait()
                    );
                await _readingCotentsTask;
            }
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CancelSearchTask();
        }

        private async void ItemListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as HtmlPageInfo;
            ChmFile obj;
            if (_chmWeak.TryGetTarget(out obj))
            {
                await obj.SetCurrent(item.Path);
                Frame.Navigate(typeof(ReadingPage), obj.Key);
            }
            else
            {
                Frame.GoBack();
            }
            
        }

        private async void textBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _searchResult.Clear();
                Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().CoreWindow.IsInputEnabled = false;
                Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().CoreWindow.IsInputEnabled = true;
                await Search(textBox.Text);
            }
        }
        private async Task Search(string key)
        {
            ChmFile obj;
            if (!_chmWeak.TryGetTarget(out obj))
            {
                return;
            }
            await _readingCotentsTask;
            await CancelSearchTask();

            ShowProgress();
            _cancelToken = false;
            _searchTask = Task.Run(() =>
                    {
                        for (int i = 0; i < _files.Count; ++i)
                        {
                            if (_cancelToken)
                            {
                                break;
                            }
                            string file = _files[i];
                            try
                            {
                                var htmlData = obj.GetData(file).Result;
                                string title;
                                string text = Html2Text.ToText(htmlData, out title);
                                int score = GetScore(text, key);
                                if (score > 0)
                                {
                                    AddToList(title, file, score).Wait();
                                }
                            }
                            catch (Exception ex)// IGNORE
                            {
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    System.Diagnostics.Debug.WriteLine(string.Format("Loading Html Error: {0}", ex.ToString()));
                                }
                            }
                        }
                    });
            await _searchTask;
            HideProgress();
        }

        private async Task CancelSearchTask()
        {
            HideProgress();
            if (_searchTask != null)
            {
                _cancelToken = true;
                await _searchTask;
            }
        }

        private void ShowProgress()
        {
            progressGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            progressRing.IsActive = true;
        }

        private void HideProgress()
        {
            progressRing.IsActive = false;
            progressGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async Task AddToList(string title, string path, int score)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    _searchResult.Add(new HtmlPageInfo()
                        {
                            Title = title,
                            Path = path,
                            Score = score
                        });
                });
        }

        private int GetScore(string text, string key)
        {
            int count = 0;
            int index = -1;
            while (index + 1 + key.Length <= text.Length)
            {
                index = text.IndexOf(key, index + 1, StringComparison.CurrentCultureIgnoreCase);
                if (index < 0)
                {
                    break;
                }
                count++;
            }
            return count;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await CancelSearchTask();
        }

    }
}
