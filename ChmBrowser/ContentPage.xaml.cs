/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using ChmBrowser.Common;
using ChmCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ChmBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ContentPage : Page
    {
        private WeakReference<ChmFile> chmWeak_ = new WeakReference<ChmFile>(null);
        private TopicsInfo _topics = new TopicsInfo();

        public ContentPage()
        {
            this.InitializeComponent();

            this.DataContext = _topics;
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.Loaded += ContentPage_Loaded;  
        }

        void ContentPage_Loaded(object sender, RoutedEventArgs e)
        {
            ChmFile obj;
            if (chmWeak_.TryGetTarget(out obj))
            {
                _topics.SelectTopic(obj.CurrentPath);
                ScrollToViewForNormalView(_topics.SelectedIndex);
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ReadingPage.SharedChmFile == null || ReadingPage.SharedChmFile.Chm == null)
            {
                Frame.GoBack();
                return;
            }
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            ChmFile obj;
            if (chmWeak_.TryGetTarget(out obj) && obj == ReadingPage.SharedChmFile)
            {
                // do nothing
            }
            else
            {
                chmWeak_ = new WeakReference<ChmFile>(ReadingPage.SharedChmFile);
                _topics.LoadTopcisInfo(ReadingPage.SharedChmFile);
                bookNameBlock.Text = ReadingPage.SharedChmFile.ChmMeta.GetDisplayName();
            }
            buttonOutline.IsEnabled = _topics.OutlineTopics != null && _topics.OutlineTopics.Count > 0;
            UpdateUIForNormalView();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }
        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            if (IsOutlineView())
            {
                UpdateUIForNormalView();
                ScrollToViewForNormalView(_topics.SelectedIndex);
            }
            else
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
        }

        private void ScrollToView(ScrollViewer sv, int count, int index)
        {
            if (count == 0)
            {
                return;
            }
            if (index < 0) index = 0;
            double realVertialOffset = (double)(index + 1) / (double)count * sv.ExtentHeight;
            double expectedVerticalOffset = (double)(index > 0 ? index - 1 : 0) / (double)_topics.Topics.Count * sv.ExtentHeight;
            double currentVerticalOffset = sv.VerticalOffset;
            if (realVertialOffset > currentVerticalOffset && realVertialOffset < currentVerticalOffset + sv.ViewportHeight)
            {
                sv.ChangeView(0, null, null);
            }
            else
            {
                sv.ChangeView(0, expectedVerticalOffset, 1);
            }
        }

        private void ScrollToViewForNormalView(int index)
        {
            ScrollToView(childrenSV, _topics.Topics == null ? 0 : _topics.Topics.Count, index);
        }

        private void ScrollToViewForOutlineView(int index)
        {
            ScrollToView(OutlinechildrenSV, _topics.OutlineTopics == null ? 0 : _topics.OutlineTopics.Count, index);
        }

        private void GoTop_Click(object sender, RoutedEventArgs e)
        {
            if (IsOutlineView())
            {
                OutlinechildrenSV.ChangeView(0, 0, null);
            }
            else
            {
                childrenSV.ChangeView(0, 0, null);
            }
        }
        private void GoBottom_Click(object sender, RoutedEventArgs e)
        {
            if (IsOutlineView())
            {
                OutlinechildrenSV.ChangeView(0, OutlinechildrenSV.ScrollableHeight, null);
            }
            else
            {
                childrenSV.ChangeView(0, childrenSV.ScrollableHeight, null);
            }
        }
        private void Outline_Click(object sender, RoutedEventArgs e)
        {
            if (_topics.OutlineTopics == null ||  _topics.OutlineTopics.Count == 0)
            {
                return;
            }
            UpdateUIForOutlineView();
            ScrollToViewForOutlineView(_topics.OutlineSelectedIndex);
        }

        private async void tbTopic_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsOutlineView())
            {
                UpdateUIForNormalView();
                ScrollToViewForNormalView((((TextBlock)sender).DataContext as TopicInfo).TopicId);
            }
            else
            {
                ChmFile obj;
                if (chmWeak_.TryGetTarget(out obj))
                {
                    await obj.SetCurrent((((TextBlock)sender).DataContext as TopicInfo).TopicId);
                }
                Frame.GoBack();
            }
        }
        private bool IsOutlineView()
        {
            return OutlineRoot.Visibility == Windows.UI.Xaml.Visibility.Visible;
        }
        private bool IsNormalView()
        {
            return !IsOutlineView();
        }
        private void UpdateUIForOutlineView()
        {
            titleRoot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ContentRoot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            OutlineRoot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private void UpdateUIForNormalView()
        {
            titleRoot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            ContentRoot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            OutlineRoot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}
