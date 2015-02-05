/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChmCore;
using ChmBrowser.Common;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Live;

namespace ChmBrowser.Common
{
    public class ChmFile
    {
        public const string ChmFileExtension = ".chm";
        

        private static ChmFile _currentFile;
        public static ChmFile CurrentFile 
        { 
            get
            {
                return _currentFile;
            }
            private set
            {
                if (value != null)
                {
                    FileHistory.UpdateHistoryWhenOpenFile(value.Key);
                    if (_currentFile != null)
                    {
                        if (_currentFile.Chm != null)
                        {
                            _currentFile.Chm.Dispose();
                            _currentFile.Chm = null;
                        }
                    }
                }
                _currentFile = value;
            }
        }
        public static async Task<bool> ContainsFile(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmFileExtension);
                return true;
            }
            catch
            {
                return false;
            } 
        }
        public static async Task<bool> DeleteFile(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmFileExtension);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void CloseChmFile()
        {
            CurrentFile = null;
        }

        public static async Task<bool> OpenChmFileFromOneDrive(LiveConnectClient client, 
            IProgress<LiveOperationProgress> progressHandler,
            System.Threading.CancellationToken ctoken,
            string id, string name, string path)
        {
            ChmFile ret = new ChmFile();
            ret.Key = Guid.NewGuid().ToString("N");
            ret.HasThumbnail = false;
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            StorageFile file = await localFolder.CreateFileAsync(ret.Key + ChmFileExtension, CreationCollisionOption.ReplaceExisting);
            LiveDownloadOperationResult result = await client.BackgroundDownloadAsync(id + "/content", file, ctoken, progressHandler);
            
            try
            {
                ret.Chm = await LoadChm(file.Path);
                MetaInfo meta = new MetaInfo();
                meta.SetOriginalPath(path);
                if (ret.Chm.Title != null)
                {
                    meta.SetDisplayName(ret.Chm.Title);
                }
                else
                {
                    meta.SetDisplayName(System.IO.Path.GetFileNameWithoutExtension(name));
                }
                ret.ChmMeta = meta;
                await ret.SetCurrent(ret.Chm.Home);
                await ret.Save();
            }
            catch
            {
                if (ret.Chm != null)
                {
                    ret.Chm.Dispose();
                    ret.Chm = null;
                }
            }
            if (ret.Chm == null)
            {
                await MetaInfo.DeleteMetaFile(ret.Key);
                await DeleteFile(ret.Key);
                return false;
            }
            CurrentFile = ret;
            return true;
        }

        public static async Task<bool> OpenChmFileFromPhone(IStorageFile storageFile)
        {
            ChmFile ret = new ChmFile();
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            ret.Key = Guid.NewGuid().ToString("N");
            ret.HasThumbnail = false;
            var file = await storageFile.CopyAsync(localFolder, ret.Key + ChmFileExtension);
            try
            {
                ret.Chm = await LoadChm(file.Path);
                MetaInfo meta = new MetaInfo();
                meta.SetOriginalPath(storageFile.Path);
                if (ret.Chm.Title != null)
                {
                    meta.SetDisplayName(ret.Chm.Title);
                }
                else
                {
                    meta.SetDisplayName(System.IO.Path.GetFileNameWithoutExtension(storageFile.Name));
                }
                ret.ChmMeta = meta;
                await ret.SetCurrent(ret.Chm.Home);
                await ret.Save();
            }
            catch
            {
                if (ret.Chm != null)
                {
                    ret.Chm.Dispose();
                    ret.Chm = null;
                }
            }
            if (ret.Chm == null)
            {
                await MetaInfo.DeleteMetaFile(ret.Key);
                await DeleteFile(ret.Key);
                return false;
            }
            CurrentFile = ret;
            return true;
        }

        public static async Task<bool> OpenLocalChmFile(EntryInfo entry)
        {
            return await OpenLocalChmFile(entry.Key);
        }
        public static async Task<bool> OpenLocalChmFile(string key)
        {
            try
            {
                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                ChmFile ret = new ChmFile();
                ret.Key = key;
                var file = await localFolder.GetFileAsync(key + ChmFileExtension);
                ret.Chm = await LoadChm(file.Path);
                ret.ChmMeta = await MetaInfo.ReadMetaInfo(key);
                ret.HasThumbnail = await Snapshot.HasSnapshot(key);
                if (ret.Chm.Title != null)
                {
                    ret.ChmMeta.SetDisplayName(ret.Chm.Title);
                }
                //try
                //{
                //    Windows.UI.Xaml.Media.Imaging.BitmapImage image = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                //    using (var fileStream = await (await folder.GetFileAsync("0.png")).OpenReadAsync())
                //    {
                //        await image.SetSourceAsync(fileStream);
                //    }
                //}
                //catch
                //{
                //    ret.HasThumbnail = false;
                //}
                if (!ret.ChmMeta.ContainsLast())
                {
                    await ret.SetCurrent(ret.Chm.Home);
                    //await ret.ChmMeta.SaveMetaInfo(key);
                }
                else
                {
                    await ret.SetCurrent(ret.ChmMeta.GetLast());
                }
                CurrentFile = ret;
                return true;
            }
            catch
            {
            }
            await FileHistory.DeleteFromHistory(key);
            return false;
        }

        private ChmFile()
        {
            Current = 0;
        }

        public Chm Chm { get; private set; }
        private int Current { get; set; }
        public bool HasOutline { get { return Chm.Contents != null && Chm.Contents.Count > 0; } }
        public string CurrentPath { get; private set; }
        public MetaInfo ChmMeta { get; private set; }
        public string Key { get; private set; }
        public bool HasThumbnail { get; private set; }

        private bool _isSaving = false;

        public static async Task<Chm> LoadChm(string path)
        {
            return await Task.Run(() =>
                {
                    Chm chm = new Chm(path); 
                    return chm;
                });
        }

        public async Task<byte[]> GetData(string path)
        {
            return await Task.Run(() =>
            {
                return Chm.GetData(path);
            });
        }

        public async Task<bool> HasData(string path)
        {
            return await Task.Run(() =>
            {
                return Chm.HasData(path);
            });
        }

        public async Task CreateThumbnailFile(Func<IRandomAccessStream, Task> create)
        {
            if (await Snapshot.CreateSnapshot(Key, create))
            {
                HasThumbnail = true;
                FileHistory.UpdateImage(Key);
            }
        }

        public async Task Save()
        {
            if (_isSaving) { return; } // cancel save
            _isSaving = true;
            await ChmMeta.SaveMetaInfo(Key);
            _isSaving = false;
        }

        public async Task<bool> SetNext()
        {
            if (HasOutline && Current + 1< Chm.Contents.Count)
            {
                int i = 1;
                while (Current + i < Chm.Contents.Count)
                {
                    if (await SetCurrent(Current + i))
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }

        public async Task<bool> SetPrevious()
        {
            if (HasOutline && Current > 0)
            {
                int i = 1;
                while (Current - i >= 0)
                {
                    if (await SetCurrent(Current - i))
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }

        public async Task<bool> SetCurrent(int index)
        { 
            if (index >= Chm.Contents.Count || index < 0
                || string.IsNullOrWhiteSpace(Chm.Contents[index].Url)) 
            { 
                return false; 
            }
            string url = Chm.Contents[index].Url;
            if (!await HasData(url))
            {
                return false;
            }

            Current = index;
            CurrentPath = url;
            ChmMeta.SetLast(CurrentPath);
            return true;
        }

        public async Task<bool> SetCurrent(string url)
        {
            if (!await HasData(url))
            {
                return false;
            }
            url = url.TrimStart('/');
            CurrentPath = url;
            if (!HasOutline)
            {
                ChmMeta.SetLast(CurrentPath);
                return false;
            }
            if (Current >= 0 && Current < Chm.Contents.Count 
                && string.Compare(url, Chm.Contents[Current].Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            for (int i = 0; i < Chm.Contents.Count; ++i)
            {
                if (string.Compare(url, Chm.Contents[i].Url, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    await SetCurrent(i);
                    return true;
                }
            }
            return false;
        }
    }
}
