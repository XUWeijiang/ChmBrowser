/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ChmBrowser.Common
{
    public static class Snapshot
    {
        public const string ChmSnapshotEntension = ".png";

        public static async Task<bool> HasSnapshot(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmSnapshotEntension);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<BitmapImage> LoadSnapshot(string key)
        {
            try
            {
                var uri = new System.Uri(string.Format("ms-appdata:///local/{0}", key + ChmSnapshotEntension));
                var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                Windows.UI.Xaml.Media.Imaging.BitmapImage image = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                using (var fileStream = await file.OpenReadAsync())
                {
                    await image.SetSourceAsync(fileStream);
                    return image;
                }
            }
            catch
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/chm.png"));
            }
        }
        public static async Task<bool> DeleteSnapshotFile(string key)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await localFolder.GetFileAsync(key + ChmSnapshotEntension);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> CreateSnapshot(string key, Func<IRandomAccessStream, Task> create)
        {
            try
            {
                InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
                await create(ms);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(ms);

                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);
                byte[] oldPixelData = pixelData.DetachPixelData();
                ImageCropper cropper = new ImageCropper(oldPixelData, decoder.PixelWidth, decoder.PixelHeight, BitmapPixelFormat.Bgra8);
                uint newWidth, newHeight;
                byte[] newPixelData = cropper.Crop(out newWidth, out newHeight);


                //InMemoryRandomAccessStream newMemroyBitmap = new InMemoryRandomAccessStream();
                //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, newMemroyBitmap);
                //encoder.SetPixelData(
                //    BitmapPixelFormat.Bgra8,
                //    BitmapAlphaMode.Straight,
                //    (uint)newWidth,
                //    (uint)newHeight,
                //    decoder.DpiX,
                //    decoder.DpiY,
                //    newPixelData);
                //await encoder.FlushAsync();
                //newMemroyBitmap.Seek(0);

                //decoder = await BitmapDecoder.CreateAsync(newMemroyBitmap);
                //// Create a small thumbnail.
                //int longlength = 96, width = 0, height = 0;
                //double srcwidth = decoder.PixelWidth, srcheight = decoder.PixelHeight;
                //double factor = srcwidth / srcheight;
                //if (factor < 1)
                //{
                //    height = longlength;
                //    width = (int)(longlength * factor);
                //}
                //else
                //{
                //    width = longlength;
                //    height = (int)(longlength / factor);
                //}
                //BitmapTransform transform = new BitmapTransform();
                //transform.ScaledHeight = (uint)height;
                //transform.ScaledWidth = (uint)width;
                //pixelData = await decoder.GetPixelDataAsync(
                //    BitmapPixelFormat.Bgra8,
                //    BitmapAlphaMode.Straight,
                //    transform,
                //    ExifOrientationMode.RespectExifOrientation,
                //    ColorManagementMode.DoNotColorManage);

                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(key + ChmSnapshotEntension, CreationCollisionOption.ReplaceExisting);
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        newWidth,
                        newHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        newPixelData);
                    await encoder.FlushAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }


    class ImageCropper
    {
        private byte[] _pixelData;
        private int _width;
        private int _height;
        private int _bytesPerPixel;
        private int _bytesPerLine;

        public ImageCropper(byte[] pixelData, uint width, uint height, BitmapPixelFormat format)
        {
            if (format != BitmapPixelFormat.Bgra8
                && format != BitmapPixelFormat.Rgba8)
            {
                throw new ArgumentException("Invalid pixel format.");
            }
            _pixelData = pixelData;
            _width = (int)width;
            _height = (int)height;

            _bytesPerPixel = sizeof(byte) * 4;
            _bytesPerLine = _width * _bytesPerPixel;
        }

        public byte[] Crop(out uint newWidth, out uint newHeight)
        {
            int rightCropped = DeleteRightBorder();
            if (rightCropped == _width)
            {
                newWidth = (uint)_width;
                newHeight = (uint)_height;
                return _pixelData;
            }
            int bottomCropped = DeleteBottonBorder();
            int nWidth = _width - rightCropped;
            int nHeight = _height - bottomCropped;
            newWidth = (uint)nWidth;
            newHeight = (uint)nHeight;

            byte[] ret = new byte[nWidth * nHeight * _bytesPerPixel];

            int desIndex = 0;
            for (int j = 0; j < nHeight; ++j)
            {
                int start = _bytesPerLine * j;
                for (int i = 0; i < nWidth * _bytesPerPixel; ++i)
                {
                    ret[desIndex++] = _pixelData[start + i];
                }
            }
            return ret;
        }

        private uint GetPixel(int x, int y)
        {
            int start = _bytesPerLine * y + _bytesPerPixel * x;
            return (((uint)_pixelData[start]) << 24) +
                (((uint)_pixelData[start + 1]) << 16) +
                (((uint)_pixelData[start + 2]) << 8) +
                (((uint)_pixelData[start + 3]));
        }
        private int DeleteLeftBorder()
        {
            uint color = GetPixel(0, 0);
            int r = 0;
            for (int i = 0; i < _width; ++i)
            {
                bool failed = false;
                for (int j = 0; j < _height; ++j)
                {
                    if (color != GetPixel(i, j))
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                {
                    break;
                }
                else
                {
                    r++;
                }
            }
            return r;
        }
        private int DeleteRightBorder()
        {
            uint color = GetPixel(_width - 1, _height - 1);
            int r = 0;
            for (int i = _width - 1; i >= 0; --i)
            {
                bool failed = false;
                for (int j = 0; j < _height; ++j)
                {
                    if (color != GetPixel(i, j))
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                {
                    break;
                }
                else
                {
                    r++;
                }
            }
            return r;
        }
        private int DeleteTopBorder()
        {
            uint color = GetPixel(0, 0);
            int r = 0;
            for (int j = 0; j < _height; ++j)
            {
                bool failed = false;
                for (int i = 0; i < _width; ++i)
                {
                    if (color != GetPixel(i, j))
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                {
                    break;
                }
                else
                {
                    r++;
                }
            }
            return r;
        }
        private int DeleteBottonBorder()
        {
            uint color = GetPixel(0, _height - 1);
            int r = 0;
            for (int j = _height - 1; j >= 0; --j)
            {
                bool failed = false;
                for (int i = 0; i < _width; ++i)
                {
                    if (color != GetPixel(i, j))
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                {
                    break;
                }
                else
                {
                    r++;
                }
            }
            return r;
        }
    }

}
