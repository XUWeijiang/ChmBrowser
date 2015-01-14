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

        public static async Task<BitmapImage> LoadSnapshot(string key)
        {
            try
            {
                var uri = new System.Uri(string.Format("ms-appdata:///local/{0}", key + ChmSnapshotEntension));
                var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                Windows.UI.Xaml.Media.Imaging.BitmapImage image = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                using (var fileStream = await file.OpenReadAsync())
                {
                    var tmp = new BitmapImage();
                    tmp.SetSource(fileStream);
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

                // Create a small thumbnail.
                int longlength = 96, width = 0, height = 0;
                double srcwidth = decoder.PixelWidth, srcheight = decoder.PixelHeight;
                double factor = srcwidth / srcheight;
                if (factor < 1)
                {
                    height = longlength;
                    width = (int)(longlength * factor);
                }
                else
                {
                    width = longlength;
                    height = (int)(longlength / factor);
                }
                BitmapTransform transform = new BitmapTransform();
                transform.ScaledHeight = (uint)height;
                transform.ScaledWidth = (uint)width;
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                PixelDataProvider pixelData2 = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(key + ChmSnapshotEntension, CreationCollisionOption.ReplaceExisting);
                //using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8,
                //        BitmapAlphaMode.Straight,
                //        (uint)width,
                //        (uint)height,
                //        decoder.DpiX,
                //        decoder.DpiY,
                //        pixelData.DetachPixelData());
                //    await encoder.FlushAsync();
                //}
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixelData2.DetachPixelData());
                    await encoder.FlushAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //private static uint GetPixel(byte[] pixelData, uint width, uint x, uint y)
        //{

        //}

        //private static byte[] TrimImage(byte[] pixelData, ref uint width, ref uint height)
        //{
        //    uint bytesPerPixel = sizeof(byte) * 4;
        //    uint bytestPerLine = width * bytesPerPixel;

        //    uint rightTopColor = pixelData[bytesPerPixel * (width - 1)]
        //}
    }


    //private class ImageCropper
    //{
    //    public ImageCropper(byte[] pixelData, uint width, uint height)
    //    {

    //    }
    //}

}
