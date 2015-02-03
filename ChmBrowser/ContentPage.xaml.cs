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
        private WeakReference<ChmCore.Chm> chmWeak_ = new WeakReference<Chm>(null);
        private TopcisInfo _topics = new TopcisInfo();

        public ContentPage()
        {
            this.InitializeComponent();
            this.DataContext = _topics;
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.Loaded += ContentPage_Loaded;
            //outlineControl.SelectedNodeChanged += outlineControl_SelectedNodeChanged;   
        }

        void ContentPage_Loaded(object sender, RoutedEventArgs e)
        {
            _topics.SelectTopic(ChmFile.CurrentFile.CurrentPath);
            ScrollToView();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Chm obj;
            if (chmWeak_.TryGetTarget(out obj) && obj == ChmFile.CurrentFile.Chm)
            {
                // do nothing
            }
            else
            {
                chmWeak_ = new WeakReference<Chm>(ChmFile.CurrentFile.Chm);
                _topics.RefreshTopcisInfo();
                bookNameBlock.Text = ChmFile.CurrentFile.ChmMeta.GetDisplayName();
            }
        }

        public void ScrollToView()
        {
            if (_topics.SelectedTopic == null || _topics.Topics == null || _topics.Topics.Count == 0)
            {
                return;
            }
            double expectedVerticalOffset = (double)(_topics.SelectedIndex > 0? _topics.SelectedIndex - 1: 0) / (double)_topics.Topics.Count * childrenSV.ExtentHeight;
            double currentVerticalOffset = childrenSV.VerticalOffset;
            if (expectedVerticalOffset > currentVerticalOffset && expectedVerticalOffset < currentVerticalOffset + childrenSV.ViewportHeight)
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
            await ChmFile.CurrentFile.SetCurrent((((TextBlock)sender).DataContext as TopicInfo).TopicId);
            Frame.GoBack();
        }
    }
}
