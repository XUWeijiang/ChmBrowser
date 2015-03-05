/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
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
                return new Thickness(Level * 30, 6, 0, 0);
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

        public FontWeight FontWeight
        {
            get
            {
                if (Level == 0)
                {
                    return FontWeights.SemiBold;
                }
                else if (Level == 1)
                {
                    return FontWeights.Medium;
                }
                else
                {
                    return FontWeights.Normal;
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

    public class TopicsInfo :NotifyPropertyChangedBased
    {
        private IList<TopicInfo> _entries = new List<TopicInfo>();
        private int _maxLevel = 0;

        private IList<TopicInfo> _outlineEntries = new List<TopicInfo>();
        private int _outlineSeletedIndex = -1;
        private int _outineLevel = 0;

        private TopicInfo _selectedTopic = null;
        private int _selectedIndex = -1;

        public TopicsInfo()
        {
        }

        public IList<TopicInfo> OutlineTopics
        {
            get
            {
                return _outlineEntries;
            }
            private set
            {
                if (_outlineEntries != value)
                {
                    _outlineEntries = value;
                    this.OnPropertyChanged("OutlineTopics");
                }
            }
        }
        public IList<TopicInfo> Topics
        {
            get
            {
                return _entries;
            }
            private set
            {
                if (_entries != value)
                {
                    _selectedIndex = -1;
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
                return _selectedIndex >= 0 ? _entries[_selectedIndex] : null;
            }
        }
        // The index in Topics
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            private set
            {
                if (_selectedIndex >= 0 && _selectedIndex != value)
                {
                    _entries[_selectedIndex].IsSelected = false;
                }
                _selectedIndex = value;
                if (_selectedIndex >= 0)
                {
                    _entries[_selectedIndex].IsSelected = true;
                }
            }
        }

        public int OutlineSelectedIndex
        {
            get
            {
                return _outlineSeletedIndex;
            }
            private set
            {
                if (_outlineSeletedIndex >= 0 && _outlineSeletedIndex != value)
                {
                    _outlineEntries[_outlineSeletedIndex].IsSelected = false;
                }
                _outlineSeletedIndex = value;
                if (_outlineSeletedIndex >= 0)
                {
                    _outlineEntries[_outlineSeletedIndex].IsSelected = true;
                }
            }
        }

        public TopicInfo OutlineSelectedTopic
        {
            get
            {
                return _outlineSeletedIndex >= 0 ? _outlineEntries[_outlineSeletedIndex] : null;
            }
        }

        //public void ResetZoom()
        //{
        //    if (_currentTopicPath.Count == 0)
        //    {
        //        return;
        //    }
        //    OutlineTopics = _entries;
        //    _selectedIndex = _currentTopicPath[_currentTopicPath.Count - 1];
        //    _outineLevel = _maxLevel;
        //}

        //public void ZoomTo(int level)
        //{
        //    if (level < 0 || level > _maxLevel)
        //    {
        //        return;
        //    }
        //    if (level == _maxLevel)
        //    {
        //        ResetZoom();
        //    }
        //    else
        //    {
        //        _outineLevel = level;
        //        OutlineTopics = _entries.Where(x => x.Level <= _outineLevel).ToArray();
        //        UpdateSelection();
        //    }
        //}

        //public bool CanZoomIn()
        //{
        //    return _currentTopicPath.Count > 0 && _outineLevel > 0;
        //}

        //public void ZoomIn()
        //{
        //    if (!CanZoomIn()) return;
        //    ZoomTo(_outineLevel - 1);
        //}
        //public bool CanZoomOut()
        //{
        //    return _currentTopicPath.Count > 0 && _outineLevel < _maxLevel;
        //}
        //public void ZoomOut()
        //{
        //    if (!CanZoomOut()) return;
        //    ZoomTo(_outineLevel + 1);
        //}
        //private void UpdateSelection()
        //{
        //    _selectedIndex = 0;
        //    _selectedTopic = OutlineTopics[_selectedIndex];
        //    var refTopic = _currentTopicPath.Count > _outineLevel ? 
        //        _entries[_currentTopicPath[_outineLevel]] : 
        //        _entries[_currentTopicPath[_currentTopicPath.Count - 1]];
        //    for (int i = 0; i < OutlineTopics.Count; ++i)
        //    {
        //        if (object.ReferenceEquals(OutlineTopics[i], refTopic))
        //        {
        //            _selectedIndex = i;
        //            break;
        //        }
        //    }
        //}
        public void SelectTopic(string url)
        {
            if (_selectedTopic != null && string.Compare(url, _selectedTopic.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return;
            }
            var currentTopicPath = new List<int>(Enumerable.Repeat(0, _maxLevel + 1));
            bool found = false;

            for(int i = 0; i < _entries.Count; ++i)
            {
                var t = _entries[i];
                currentTopicPath[_entries[i].Level] = i;
                if (string.Compare(url, t.Url, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    found = true;
                    SelectedIndex = i;
                    if (_outineLevel >= 0)
                    {
                        if (t.Level >= _outineLevel)
                        {
                            SetOutlineIndex(_entries[currentTopicPath[_outineLevel]].TopicId);
                        }
                        else
                        {
                            SetOutlineIndex(t.TopicId);
                        }
                    }
                    break;
                }
            }
            if (!found)
            {
                SelectedIndex = -1;
                OutlineSelectedIndex = -1;
            }
        }
        private class TopicComparer : IComparer<TopicInfo>
        {
            public int Compare(TopicInfo x, TopicInfo y)
            {
                return x.TopicId.CompareTo(y.TopicId);
            }
        }

        private void SetOutlineIndex(int topicId)
        {
            if (_outineLevel < 0) return;
            OutlineSelectedIndex = ((List<TopicInfo>)_outlineEntries).BinarySearch(new TopicInfo(null, topicId), new TopicComparer()); 
        }

        public void LoadTopcisInfo(ChmFile chmFile)
        {
            SelectedIndex = -1;
            OutlineSelectedIndex = -1;
            Topics = new List<TopicInfo>();
            OutlineTopics = new List<TopicInfo>();
            
            if (chmFile == null || !chmFile.HasOutline)
            {
                return;
            }
            _maxLevel = 0;
            IList<TopicInfo> entries = new List<TopicInfo>();
            Dictionary<int, int> levelCounts = new Dictionary<int, int>();
            for (int i = 0; i < chmFile.Chm.Contents.Count; ++i)
            {
                int level = chmFile.Chm.Contents[i].Level;
                _maxLevel = Math.Max(_maxLevel, level);
                if (!levelCounts.ContainsKey(level))
                {
                    levelCounts[level] = 1;
                }
                else
                {
                    levelCounts[level]++;
                }
                entries.Add(new TopicInfo(chmFile, i));
            }
            Topics = entries;
            _outineLevel = GetOutlineLevel(levelCounts, _maxLevel);

            if (_outineLevel >= 0)
            {
                OutlineTopics = _entries.Where(x => x.Level <= _outineLevel).Select(x => new TopicInfo(chmFile, x.TopicId)).ToList();
                if (OutlineTopics.Count <= 1)
                {
                    _outineLevel = -1;
                    OutlineTopics = new List<TopicInfo>();
                }
            }
            else
            {
                OutlineTopics = new List<TopicInfo>();
            }
            
        }

        private static int GetOutlineLevel(Dictionary<int, int> levelCounts, int maxLevel)
        {
            int targetOutlineNumber = 35;

            if (maxLevel == 0)
            {
                return -1; // no outline.
            }

            int minGap = int.MaxValue;
            int minLevel = -1;
            int currentNumber = 0;
            for (int i = 0; i < maxLevel; ++i)
            {
                currentNumber += levelCounts.ContainsKey(i) ? levelCounts[i] : 0;
                int gap = Math.Abs(targetOutlineNumber - currentNumber);
                if (gap < minGap)
                {
                    minGap = gap;
                    minLevel = i;
                }
                else
                {
                    break;
                }
            }
            return minLevel;
        }
    }
}
