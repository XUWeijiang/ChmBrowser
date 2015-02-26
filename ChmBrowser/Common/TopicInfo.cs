﻿/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace ChmBrowser.Common
{
    public class TopicInfo: NotifyPropertyChangedBased
    {
        private bool _isSelected = false;

        public string Topic 
        { 
            get 
            {
                ChmFile obj;
                if (chmWeak_.TryGetTarget(out obj))
                {
                    return obj.Chm.Contents[TopicId].Name;
                }
                return string.Empty;
            } 
        }
        public int Level 
        { 
            get 
            {
                ChmFile obj;
                if (chmWeak_.TryGetTarget(out obj))
                {
                    return obj.Chm.Contents[TopicId].Level;
                }
                return 0;
            } 
        }
        public string Url 
        { 
            get 
            {
                ChmFile obj;
                if (chmWeak_.TryGetTarget(out obj))
                {
                    return obj.Chm.Contents[TopicId].Url;
                }
                return string.Empty;
            } 
        }
        public bool IsSelected 
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("Fore");
                }
            }
        }

        public int TopicId { get; private set; }

        public Thickness Margin {
            get
            {
                return new Thickness((Level - 1) * 30, 6, 0, 0);
            }
        }

        public Brush Fore
        {
            get
            {
                if (IsSelected)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                else
                {
                    return (Brush)App.Current.Resources["ApplicationForegroundThemeBrush"];
                }
            }
        }
        private WeakReference<ChmFile> chmWeak_;

        public TopicInfo(ChmFile chmFile, int topicId)
        {
            chmWeak_ = new WeakReference<ChmFile>(chmFile);
            IsSelected = false;
            TopicId = topicId;
        }
    }

    public class TopcisInfo :NotifyPropertyChangedBased
    {
        private TopicInfo _selectedTopic = null;
        private int _selectedIndex = 0;

        public TopcisInfo()
        {
        }

        private IList<TopicInfo> _entries = new List<TopicInfo>();
        public IList<TopicInfo> Topics
        {
            get
            {
                return _entries;
            }
            set
            {
                if (_entries != value)
                {
                    _selectedIndex = 0;
                    _selectedTopic = null;
                    _entries = value;
                    this.OnPropertyChanged("Topics");
                }
            }
        }

        public TopicInfo SelectedTopic 
        { 
            get
            {
                return _selectedTopic;
            }
            private set
            {
                if (_selectedTopic != null && _selectedTopic != value)
                {
                    _selectedTopic.IsSelected = false;
                }
                _selectedTopic = value;
                if (_selectedTopic != null)
                {
                    _selectedTopic.IsSelected = true;
                }
            }
        }

        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
        }

        public void SelectTopic(string url)
        {
            if (_selectedTopic != null && string.Compare(url, _selectedTopic.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return;
            }
            for(int i = 0; i < _entries.Count; ++i)
            {
                var t = _entries[i];
                if (string.Compare(url, t.Url, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    SelectedTopic = t;
                    _selectedIndex = i;
                    break;
                }
            }
        }

        public void RefreshTopcisInfo(ChmFile chmFile)
        {
            if (chmFile == null || !chmFile.HasOutline)
            {
                Topics = new List<TopicInfo>();
                return;
            }
            IList<TopicInfo> entries = new List<TopicInfo>();
            for (int i = 0; i < chmFile.Chm.Contents.Count; ++i)
            {
                entries.Add(new TopicInfo(chmFile, i));
            }
            Topics = entries;
        }
    }
}
