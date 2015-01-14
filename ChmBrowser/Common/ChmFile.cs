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
                    if (_currentFile != null && _currentFile.Chm != null)
                    {
                        _currentFile.Chm.Dispose();
                        _currentFile.Chm = null;
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

        public static async Task<bool> OpenChmFileFromPhone(IStorageFile storageFile)
        {
            ChmFile ret = new ChmFile();
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            ret.Key = Guid.NewGuid().ToString("N");
            ret.HasThumbnail = false;
            var file = await storageFile.CopyAsync(localFolder, ret.Key + ChmFileExtension);
            try
            {
                ret.Chm = new ChmCore.Chm(file.Path);
                MetaInfo meta = new MetaInfo();
                meta.SetOriginalPath(storageFile.Path);
                meta.SetDisplayName(System.IO.Path.GetFileNameWithoutExtension(storageFile.Name));
                ret.ChmMeta = meta;
                ret.SetCurrent(ret.Chm.Outline.Next);
                //await meta.SaveMetaInfo(ret.Key);
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
                ret.Chm = new ChmCore.Chm(file.Path);
                ret.ChmMeta = await MetaInfo.ReadMetaInfo(key);
                ret.HasThumbnail = await Snapshot.HasSnapshot(key);
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
                    ret.SetCurrent(ret.Chm.Outline.Next);
                    //await ret.ChmMeta.SaveMetaInfo(key);
                }
                else
                {
                    ret.SetCurrent(ret.ChmMeta.GetLast());
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
        }

        public Chm Chm { get; private set; }
        public ChmOutline Current { get; private set; }
        public MetaInfo ChmMeta { get; private set; }
        public string Key { get; private set; }
        public bool HasThumbnail { get; private set; }

        public async Task CreateThumbnailFile(Func<IRandomAccessStream, Task> create)
        {
            if (await Snapshot.CreateSnapshot(Key, create))
            {
                HasThumbnail = true;
                FileHistory.UpdateImage(Key);
            }
        }

        public async void Save()
        {
            await ChmMeta.SaveMetaInfo(Key);
        }

        public void SetCurrent(ChmOutline outline)
        {
            Current = outline;
            ChmMeta.SetLast(outline.Url);
        }

        public bool SetCurrent(string url)
        {
            url = url.Trim('/');
            if (Current != null && string.Compare(url, Current.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return SetCurrent(Chm.Outline, url);
        }

        private bool SetCurrent(ChmOutline node, string url)
        {
            if (string.Compare(url, node.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                SetCurrent(node);
                return true;
            }
            for (int i = 0; i < node.SubSections.Count; ++i)
            {
                if (SetCurrent(node.SubSections[i], url))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
