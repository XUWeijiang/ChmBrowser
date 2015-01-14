﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace ChmBrowser.Common
{
    public class EntryInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public ImageSource Image { get; set; }
    }
    public class EntriesInfo : INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private IList<EntryInfo> _entries = new List<EntryInfo>();
        public IList<EntryInfo> Entries
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
