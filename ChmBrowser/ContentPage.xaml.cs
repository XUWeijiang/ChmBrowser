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

        public ContentPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.Loaded += ContentPage_Loaded;
            outlineControl.SelectedNodeChanged += outlineControl_SelectedNodeChanged;   
        }

        void ContentPage_Loaded(object sender, RoutedEventArgs e)
        {
            HierarchyNode node = outlineControl.LocateItem(ChmFile.CurrentFile.Current);
            ScrollToView(node);
        }

        void outlineControl_SelectedNodeChanged(object sender, HierarchyNode e)
        {
            ChmFile.CurrentFile.SetCurrent(e.Data);
            Frame.GoBack();
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
                outlineControl.ShowData(ChmFile.CurrentFile.Chm.Outline, true);
                bookNameBlock.Text = ChmFile.CurrentFile.ChmMeta.GetDisplayName();
            }
        }

        public void ScrollToView(HierarchyNode node)
        {
            var transform = node.TransformToVisual(childrenSV);
            var positionInScrollViewer = transform.TransformPoint(new Point(0, 0));

            double verticalOffset = childrenSV.VerticalOffset;
            double horizontalOffset = childrenSV.HorizontalOffset;

            if (positionInScrollViewer.Y < 0 || positionInScrollViewer.Y > childrenSV.ViewportHeight)
            {
                verticalOffset = childrenSV.VerticalOffset + positionInScrollViewer.Y - 10;
            }

            if (positionInScrollViewer.X < 0 || positionInScrollViewer.X > childrenSV.ViewportWidth)
            {
                horizontalOffset = childrenSV.HorizontalOffset + positionInScrollViewer.X - 10;
            }

            childrenSV.ChangeView(horizontalOffset, verticalOffset, 1);
        }

        private void GoTop_Click(object sender, RoutedEventArgs e)
        {
            childrenSV.ChangeView(0, 0, null);
        }
        private void GoBottom_Click(object sender, RoutedEventArgs e)
        {
            childrenSV.ChangeView(0, childrenSV.ScrollableHeight, null);
        }
    }
}
