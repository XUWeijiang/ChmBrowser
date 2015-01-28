/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ChmBrowser.Common
{
    public class MetaInfo
    {
        public const string ChmMetaFileExtension = ".meta";
        private IDictionary<string, string> _meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public MetaInfo()
        { }

        // return null if not existing
        public string GetMeta(string metaName)
        {
            if (_meta.ContainsKey(metaName))
            {
                return _meta[metaName];
            }
            return null;
        }
        public void SetMeta(string metaName, string value)
        {
            _meta[metaName] = value;
        }

        public bool Contains(string metaName)
        {
            return _meta.ContainsKey(metaName);
        }

        public bool ContainsLast()
        {
            return Contains("last");
        }

        public void SetLast(string lastUrl)
        {
            SetMeta("last", lastUrl);
        }

        public void SetDisplayName(string name)
        {
            SetMeta("name", name);
        }

        public void SetOriginalPath(string path)
        {
            SetMeta("path", path);
        }

        public void SetScale(string scale)
        {
            SetMeta("scale", scale);
        }

        public string GetLast()
        {
            return GetMeta("last");
        }

        public string GetDisplayName()
        {
            return GetMeta("name");
        }

        public string GetOriginalPath()
        {
            return GetMeta("path");
        }

        // Negtive means auto
        public string GetScale()
        {
            string scale = GetMeta("scale");
            if (string.IsNullOrEmpty(scale))
            {
                scale = "-1";
            }
            return scale;
        }

        public async Task SaveMetaInfo(IStorageFile file)
        {
            await FileIO.WriteLinesAsync(file, _meta.Select(x => x.Key + "=" + x.Value));
        }
        public async Task SaveMetaInfo(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync(key + ChmMetaFileExtension, CreationCollisionOption.OpenIfExists);
            await SaveMetaInfo(file);
        }
        public static async Task<MetaInfo> ReadMetaInfo(IStorageFile file)
        {
            MetaInfo info = new MetaInfo();
            try
            {
                IList<string> data = await FileIO.ReadLinesAsync(file);
                foreach (var d in data)
                {
                    string[] kv = d.Split('=');
                    if (kv.Length != 2) continue;
                    info._meta[kv[0]] = kv[1];
                }
            }
            catch
            { }
            return info;
        }
        public static async Task<MetaInfo> ReadMetaInfo(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmMetaFileExtension);
                return await ReadMetaInfo(file);
            }
            catch
            { }
            return new MetaInfo();;
        }
        public static async Task<bool> DeleteMetaFile(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmMetaFileExtension);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
