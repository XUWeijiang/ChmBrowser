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
        private TopcisInfo _topics = new TopcisInfo();

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
                ScrollToView();
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ReadingPage.SharedChmFile == null || ReadingPage.SharedChmFile.Chm == null)
            {
                Frame.GoBack();
                return;
            }
            ChmFile obj;
            if (chmWeak_.TryGetTarget(out obj) && obj == ReadingPage.SharedChmFile)
            {
                // do nothing
            }
            else
            {
                chmWeak_ = new WeakReference<ChmFile>(ReadingPage.SharedChmFile);
                _topics.RefreshTopcisInfo(ReadingPage.SharedChmFile);
                bookNameBlock.Text = ReadingPage.SharedChmFile.ChmMeta.GetDisplayName();
            }
        }

        public void ScrollToView()
        {
            if (_topics.SelectedTopic == null || _topics.Topics == null || _topics.Topics.Count == 0)
            {
                return;
            }
            double realVertialOffset = (double)(_topics.SelectedIndex + 1) / (double)_topics.Topics.Count * childrenSV.ExtentHeight;
            double expectedVerticalOffset = (double)(_topics.SelectedIndex > 0? _topics.SelectedIndex - 1: 0) / (double)_topics.Topics.Count * childrenSV.ExtentHeight;
            double currentVerticalOffset = childrenSV.VerticalOffset;
            if (realVertialOffset > currentVerticalOffset && realVertialOffset < currentVerticalOffset + childrenSV.ViewportHeight)
            {
                childrenSV.ChangeView(0, null, null);
            }
            else
            {
                childrenSV.ChangeView(0, expectedVerticalOffset, 1);
            }
        }

        private void GoTop_Click(object sender, RoutedEventArgs e)
        {
            childrenSV.ChangeView(0, 0, null);
        }
        private void GoBottom_Click(object sender, RoutedEventArgs e)
        {
            childrenSV.ChangeView(0, childrenSV.ScrollableHeight, null);
        }

        private async void tbTopic_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ChmFile obj;
            if (chmWeak_.TryGetTarget(out obj))
            {
                await obj.SetCurrent((((TextBlock)sender).DataContext as TopicInfo).TopicId);
            }
            Frame.GoBack();
        }
    }
}
