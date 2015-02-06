/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

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
using Windows.UI.StartScreen;
using System.Threading.Tasks;
using Windows.UI;

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
            Color defaultBackground = (Color)App.Current.Resources["PhoneBackgroundColor"];
            progressBarGrid.Background = new SolidColorBrush(Color.FromArgb(0x30, defaultBackground.R, defaultBackground.G, defaultBackground.B));
            LoadIconListView();
            Frame.BackStack.Clear();

            if (e.NavigationMode == NavigationMode.New && e.Parameter != null && !string.IsNullOrEmpty(e.Parameter.ToString()))
            {
                await OpenLocalChmFile(e.Parameter.ToString()); // Navigate to ReadingPage
            }
            else
            {
                _history.Entries = await FileHistory.GetHistoryEntriesInfo(); // stay in this page.
            }
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
                StartProcessing();
                try
                {
                    SetProgressString(App.Localizer.GetString("Copying"));
                    string key = await ChmFile.SetupChmFileFromPhone(args.Files[0]);
                    if (key == null) // failed
                    {
                        MessageDialog msg = new MessageDialog(string.Format(App.Localizer.GetString("InvalidFile"), args.Files[0].Path));
                        await msg.ShowAsync();
                    }
                    else
                    {
                        Frame.Navigate(typeof(ReadingPage), key);
                    }
                }
                finally
                {
                    StopProcessing();
                }
            }
        }

        private async void ItemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            EntryInfo info = e.ClickedItem as EntryInfo;
            await OpenLocalChmFile(info.Key);
        }

        private async Task OpenLocalChmFile(string key)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                ()=>Frame.Navigate(typeof(ReadingPage), key));
        }

        private void ItemGridView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                EntryInfo info = ((FrameworkElement)sender).DataContext as EntryInfo;
                if (Windows.UI.StartScreen.SecondaryTile.Exists(info.Key))
                {
                    pinItem.Text = App.Localizer.GetString("Unpin/Text");
                }
                else
                {
                    pinItem.Text = App.Localizer.GetString("Pin/Text");
                }
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
            if (Windows.UI.StartScreen.SecondaryTile.Exists(info.Key))
            {
                SecondaryTile st = new SecondaryTile(info.Key);
                await st.RequestDeleteAsync();
            }
            _history.Entries = await FileHistory.GetHistoryEntriesInfo();
        }

        private async void pinItem_Click(object sender, RoutedEventArgs e)
        {
            EntryInfo info = ((FrameworkElement)sender).DataContext as EntryInfo;
            if (Windows.UI.StartScreen.SecondaryTile.Exists(info.Key))
            {
                SecondaryTile st = new SecondaryTile(info.Key);
                await st.RequestDeleteAsync();
            }
            else
            {
                Uri square150x150Logo = new Uri("ms-appx:///Assets/tile.png");
                SecondaryTile st = new SecondaryTile(info.Key, info.Name, info.Key, square150x150Logo, TileSize.Square150x150);
                st.VisualElements.ShowNameOnSquare150x150Logo = true;
                st.RoamingEnabled = false;
                await st.RequestCreateAsync();
            }
        }

        private void GoAbout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private void IconListSwitch_Click(object sender, RoutedEventArgs e)
        {
            SetIconListView(sender == listButton);
            SaveIconListView();
        }

        private void SaveIconListView()
        {
            var setting = Windows.Storage.ApplicationData.Current.LocalSettings;
            setting.Values["item_view"] = listButton.Visibility == Windows.UI.Xaml.Visibility.Collapsed?"list":"icon";
        }

        private void LoadIconListView()
        {
            var setting = Windows.Storage.ApplicationData.Current.LocalSettings;
            object value;
            bool isIcon = !setting.Values.TryGetValue("item_view", out value) || value.ToString() == "icon";
            SetIconListView(!isIcon);
        }

        private void StartProcessing()
        {
            SetProgressString("");
            progressBarGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //LayoutRoot.IsHitTestVisible = false;
        }

        private void StopProcessing()
        {
            progressBarGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //LayoutRoot.IsHitTestVisible = true;
        }

        private void SetProgressString(string value)
        {
            progressMessage.Text = value;
        }

        private void SetIconListView(bool isList)
        {
            if (isList)
            {
                listButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                iconButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ItemGridView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                ItemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                listButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                iconButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                ItemGridView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ItemListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
