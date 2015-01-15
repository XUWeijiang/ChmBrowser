using ChmBrowser.Common;
using Microsoft.Live;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ChmBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OneDriveBrowserPage : Page
    {
        public static LiveConnectSession OneDriveSession { get; set; }
        private Stack<IList<OneDriveEntry>> _bufferredHistory = new Stack<IList<OneDriveEntry>>();
        private Stack<string> _location = new Stack<string>();
        private LiveConnectClient _client;
        private OneDriveEntries _entries = new OneDriveEntries();

        public OneDriveBrowserPage()
        {
            this.InitializeComponent();
            this.DataContext = _entries;
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (_bufferredHistory.Count == 0)
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(MainPage));
                }
            }
            else
            {
                _entries.Entries = _bufferredHistory.Pop();
                _location.Pop();
                oneDrivePath.Text = "/" + string.Join("/", _location.Reverse());
            }
            e.Handled = true;
        }

        #region NavigationHelper registration

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if (_client == null)
            {
                _client = new LiveConnectClient(OneDriveSession);
                _entries.Entries.Clear();
                _bufferredHistory.Clear();
                var meResult = await _client.GetAsync("me/skydrive/files");
                var meData = meResult.Result;
                _entries.Entries = ParseEntries(meData);
            }
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }

        #endregion

        private async void ItemListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            OneDriveEntry entry = e.ClickedItem as OneDriveEntry;
            if (!entry.IsFile)
            {
                _bufferredHistory.Push(_entries.Entries);
                LiveOperationResult operationResult =
                    await _client.GetAsync(entry.Id + "/files");
                var result = operationResult.Result;
                _entries.Entries = ParseEntries(result);
                _location.Push(entry.Name);
                oneDrivePath.Text = "/" + string.Join("/", _location.Reverse());
            }
            else
            {
                try
                {
                    progressBar.Value = 0;
                    var progressHandler = new Progress<LiveOperationProgress>(
                        (progress) => 
                        {
                            progressBar.IsIndeterminate = false; 
                            this.progressBar.Value = progress.ProgressPercentage;
                        });
                    var ctsDownload = new System.Threading.CancellationToken();
                    if (await ChmFile.OpenChmFileFromOneDrive(_client, progressHandler, ctsDownload, entry.Id, entry.Name, oneDrivePath.Text))
                    {
                        Frame.GoBack();
                        Frame.Navigate(typeof(ReadingPage));
                    }
                    else
                    {
                        MessageDialog msg = new MessageDialog(string.Format("{0}: Invalid File", entry.Name));
                        await msg.ShowAsync();
                    }
                }
                finally
                {
                    progressBar.IsIndeterminate = true;
                }
            }
            progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private IList<OneDriveEntry> ParseEntries(IDictionary<string, object> data)
        {
            List<OneDriveEntry> entries = new List<OneDriveEntry>();
            if (data.ContainsKey("data"))
            {
                foreach (IDictionary<string, object> x in (dynamic)data["data"])
                {
                    if (x["type"].ToString() == "folder")
                    {
                        entries.Add(new OneDriveEntry
                            {
                                IsFile = false,
                                Name = x["name"].ToString(),
                                Id = x["id"].ToString()
                            });
                    }
                    else if (x["type"].ToString() == "file" && x["name"].ToString().EndsWith(".chm", StringComparison.OrdinalIgnoreCase))
                    {
                        entries.Add(new OneDriveEntry
                                   {
                                       IsFile = true,
                                       Name = x["name"].ToString(),
                                       Id = x["id"].ToString()
                                   });
                    }
                }
            }
            return entries;
        }
    }

    class OneDriveEntry
    {
        public bool IsFile { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public ImageSource Image 
        { 
            get 
            { 
                if (IsFile)
                {
                    return new BitmapImage(new Uri("ms-appx:///Assets/chm.png"));
                }
                else
                {
                    return new BitmapImage(new Uri("ms-appx:///Assets/folder.png"));
                }
            } 
        }
    }

    class OneDriveEntries : INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private IList<OneDriveEntry> _entries = new List<OneDriveEntry>();
        public IList<OneDriveEntry> Entries
        {
            get
            {
                return _entries;
            }
            set
            {
                if (_entries != value)
                {
                    _entries = value;
                    this.OnPropertyChanged("Entries");
                }
            }
        }
    }

}
