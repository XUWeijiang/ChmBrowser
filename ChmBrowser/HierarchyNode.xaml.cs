using ChmBrowser.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ChmBrowser
{
    public sealed partial class HierarchyNode : UserControl
    {
        private HierarchyNode _parent;
        private bool _hilighted = false;
        private ChmCore.ChmOutline _data;

        public event EventHandler<HierarchyNode> SelectedNodeChanged;

        public HierarchyNode()
        {
            this.InitializeComponent();
        }

        public HierarchyNode(HierarchyNode parent)
            :this()
        {
            _parent = parent;
        }

        public void ShowData(ChmCore.ChmOutline outline, bool onlyChildren)
        {
            _data = outline;
            if (onlyChildren)
            {
                //collapseImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //expandImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                titleTextBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                childrenSP.Margin = new Thickness(0);
            }
            else
            {
                //expandImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //if (outline.SubSections.Count == 0)
                //{
                //    collapseImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //}
                //else
                //{
                //    collapseImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //}

                titleTextBlock.Text = outline.Name;
                titleTextBlock.Tag = outline.Url;
            }
            childrenSP.Children.Clear();
            for (int i = 0; i < outline.SubSections.Count; ++i)
            {
                HierarchyNode node = new HierarchyNode(this);
                node.ShowData(outline.SubSections[i], false);
                childrenSP.Children.Add(node);
            }
        }

        public ChmCore.ChmOutline Data {get {return _data;}}
        public HierarchyNode ParentNode {get {return _parent;}}

        public HierarchyNode LocateItem(string url)
        {
            if (string.Compare(url, _data.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                Select(false);
                return this;
            }
            else
            {
                foreach (var x in childrenSP.Children)
                {
                    HierarchyNode r = ((HierarchyNode)x).LocateItem(url);
                    if (r != null) return r;
                }
            }
            return null;
        }

        //public HierarchyNode LocateNextItem()
        //{
        //    if (childrenSP.Children.Count > 0)
        //    {
        //        return (HierarchyNode)childrenSP.Children[0];
        //    }
        //    HierarchyNode p = _parent;
        //    HierarchyNode c = this;
        //    while (p != null)
        //    {
        //        int i = p.childrenSP.Children.IndexOf(c);
        //        if (i != p.childrenSP.Children.Count - 1)
        //        {
        //            HierarchyNode nextNode = ((HierarchyNode)p.childrenSP.Children[i + 1]);
        //            nextNode.Select(true);
        //            return nextNode;
        //        }
        //        else
        //        {
        //            c = p;
        //            p = p._parent;
        //        }
        //    }
        //    return null;
        //}

        //public HierarchyNode LocatePrevItem()
        //{
        //    HierarchyNode p = _parent;
        //    HierarchyNode c = this;
        //    while (p != null)
        //    {
        //        int i = p.childrenSP.Children.IndexOf(c);
        //        if (i != 0)
        //        {
        //            HierarchyNode prevNode = ((HierarchyNode)p.childrenSP.Children[i - 1]);
        //            while (prevNode.childrenSP.Children.Count > 0)
        //            {
        //                prevNode = (HierarchyNode)prevNode.childrenSP.Children.Last();
        //            }
        //            prevNode.Select(true);
        //            return prevNode;
        //        }
        //        else
        //        {
        //            c = p;
        //            p = p._parent;
        //        }
        //    }
        //    return null;
        //}

        private void titleGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Select(true);
        }

        private void Select(bool triggerSelectedChangedEvent)
        {
            List<HierarchyNode> nodes = new List<HierarchyNode>();
            nodes.Add(this);
            HierarchyNode c = this;
            while (c._parent != null)
            {
                c = c._parent;
                nodes.Add(c);
            }
            if (triggerSelectedChangedEvent)
            {
                foreach (var n in nodes)
                {
                    n.OnSelectedNodeChanged(this);
                }
            }
            Unhilight(nodes[nodes.Count - 1]);
            Highlight();
            ChmFile.CurrentFile.SetCurrent(_data);
        }


        private void Unhilight(HierarchyNode node)
        {
            foreach (var x in node.childrenSP.Children)
            {
                Unhilight((HierarchyNode)x);
            }
            node.Unhighlight();
        }
        
        private void Highlight()
        {
            if (_hilighted) return;
            _hilighted = true;
            //titleGrid.Background = new SolidColorBrush(Colors.Blue);
            titleTextBlock.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void Unhighlight()
        {
            if (!_hilighted) return;
            _hilighted = false;
            //titleGrid.Background = new SolidColorBrush(Colors.White);
            titleTextBlock.Foreground = (Brush)App.Current.Resources["ApplicationForegroundThemeBrush"];
        }

        private void OnSelectedNodeChanged(HierarchyNode node)
        {
            if (SelectedNodeChanged != null)
            {
                SelectedNodeChanged(this, node);
            }
        }

        //private void UnhilightAndCollapse(HierarchyNode node)
        //{
        //    foreach (var x in node.childrenSP.Children)
        //    {
        //        UnhilightAndCollapse((HierarchyNode)x);
        //    }
        //    node.Unhighlight();
        //    node.Collapse();
        //}

        //private void Expand()
        //{
        //    collapseImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    expandImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //    childrenSP.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //}
        //private void Collapse()
        //{
        //    collapseImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //    expandImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    childrenSP.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //}
    }
}
