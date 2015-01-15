using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ChmCore;
using Windows.UI.Popups;
using ChmBrowser.Common;
using Microsoft.Live;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace ChmBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IFileOpenPickerContinuable
    {
        private EntriesInfo _history = new EntriesInfo();
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = _history;
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _history.Entries = await FileHistory.GetHistoryEntriesInfo();

            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".chm");
            picker.PickSingleFileAndContinue();
        }
        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files.Count > 0)
            {
                bool success = await ChmFile.OpenChmFileFromPhone(args.Files[0]);

                if (!success) // failed
                {
                    MessageDialog msg = new MessageDialog(string.Format("{0}: Invalid File", args.Files[0].Path));
                    await msg.ShowAsync();
                }
                else
                {
                    Frame.Navigate(typeof(ReadingPage));
                }
            }
        }

        private async void ItemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            EntryInfo info = e.ClickedItem as EntryInfo;
            bool success = await ChmFile.OpenLocalChmFile(info);

            if (!success) // failed
            {
                MessageDialog msg = new MessageDialog(string.Format("{0}: Invalid File", info.Key));
                await msg.ShowAsync();
            }
            else
            {
                Frame.Navigate(typeof(ReadingPage));
            }
        }

        private void ItemGridView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
        }

        private async void oneDriveButton_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage = null;
            try
            {
                var authClient = new LiveAuthClient();
                LiveLoginResult result = await authClient.LoginAsync(new string[] { "wl.signin", "wl.skydrive" });

                if (result.Status == LiveConnectSessionStatus.Connected)
                {
                    OneDriveBrowserPage.OneDriveSession = result.Session;
                    Frame.Navigate(typeof(OneDriveBrowserPage));
                }
            }
            catch (LiveAuthException ex)
            {
                errorMessage = ex.Message;
            }
            catch (LiveConnectException ex)
            {
                errorMessage = ex.Message;
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != null)
            {
                MessageDialog msg = new MessageDialog(errorMessage);
                await msg.ShowAsync();
            }
        }

        private async void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            EntryInfo info = ((FrameworkElement)sender).DataContext as EntryInfo;
            await FileHistory.DeleteFromHistory(info.Key);
            _history.Entries = await FileHistory.GetHistoryEntriesInfo();
        }
    }
}
