/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChmBrowser.Common
{
    public static class FileHistory
    {
        public const string HistoryKey = "history";

        private static EntriesInfo _chmHistoryFiles = new EntriesInfo();
        public static void SetHistory(IEnumerable<string> historyList)
        {
            var setting = Windows.Storage.ApplicationData.Current.LocalSettings;
            setting.Values["history"] = string.Join(";", historyList);
        }
        public static IList<string> GetHistory()
        {
            var setting = Windows.Storage.ApplicationData.Current.LocalSettings;
            object history;
            if (!setting.Values.TryGetValue(HistoryKey, out history))
            {
                history = "";
            }
            return history.ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        public static async Task<IList<EntryInfo>> GetHistoryEntriesInfo()
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            IList<string> entriesKey = GetHistory();
            IDictionary<string, EntryInfo> oldHistory = new Dictionary<string, EntryInfo>(StringComparer.OrdinalIgnoreCase);
            if (_chmHistoryFiles != null)
            {
                foreach (var x in _chmHistoryFiles.Entries)
                {
                    oldHistory.Add(x.Key, x);
                }
            }
            else
            {
                _chmHistoryFiles = new EntriesInfo();
            }
            List<EntryInfo> newHistory = new List<EntryInfo>();
            List<string> invalidEntries = new List<string>();
            foreach (var k in entriesKey)
            {
                if (oldHistory.ContainsKey(k))
                {
                    newHistory.Add(oldHistory[k]);
                }
                else if (await ChmFile.ContainsFile(k))
                {
                    try
                    {
                        MetaInfo info = await MetaInfo.ReadMetaInfo(k);
                        EntryInfo entry = new EntryInfo();
                        entry.Key = k;
                        entry.Name = info.GetDisplayName();
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            entry.Name = "未知";
                        }
                        entry.Image = await Snapshot.LoadSnapshot(k);
                        newHistory.Add(entry);
                    }
                    catch
                    {
                        //Ignore
                        invalidEntries.Add(k);
                    }
                }
                else
                {
                    invalidEntries.Add(k);
                }
            }
            await DeleteFromHistory(invalidEntries);
            _chmHistoryFiles.Entries = newHistory;
            return newHistory;
        }

        public static async void UpdateImage(string key)
        {
            if (_chmHistoryFiles != null)
            {
                foreach (var x in _chmHistoryFiles.Entries)
                {
                    if (x.Key == key)
                    {
                        x.Image = await Snapshot.LoadSnapshot(key);
                    }
                }
            }
        }

        public static void UpdateHistoryWhenOpenFile(string name)
        {
            IList<string> historyList = GetHistory();
            historyList.Remove(name);
            historyList.Insert(0, name);
            SetHistory(historyList);
        }
        public static async Task DeleteFromHistory(string name)
        {
            IList<string> historyList = GetHistory();
            historyList.Remove(name);
            await ChmFile.DeleteFile(name);
            await MetaInfo.DeleteMetaFile(name);
            await Snapshot.DeleteSnapshotFile(name);
            SetHistory(historyList);
        }
        public static async Task DeleteFromHistory(IEnumerable<string> keys)
        {
            IList<string> historyList = GetHistory();
            foreach (string name in keys)
            {
                historyList.Remove(name);
                await ChmFile.DeleteFile(name);
                await MetaInfo.DeleteMetaFile(name);
                await Snapshot.DeleteSnapshotFile(name);
            }
            SetHistory(historyList);
        }
    }
}
