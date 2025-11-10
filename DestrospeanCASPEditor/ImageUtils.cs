using System.Collections.Generic;
using System.Drawing;
using Gdk;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ImageUtils
    {
        public static readonly Dictionary<string, List<Pixbuf>> PreloadedGameImagePixbufs = new Dictionary<string, List<Pixbuf>>();

        public static readonly Dictionary<string, Bitmap> PreloadedGameImages = new Dictionary<string, Bitmap>();

        public static readonly Dictionary<IResourceIndexEntry, List<Pixbuf>> PreloadedImagePixbufs = new Dictionary<IResourceIndexEntry, List<Pixbuf>>();

        public static readonly Dictionary<string, Bitmap> PreloadedImages = new Dictionary<string, Bitmap>();

        public static Pixbuf ConvertToPixbuf(Bitmap image)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return new Pixbuf(stream);
            }
        }

        public static Pixbuf CreateCheckerboard(int size, int checkSize, Gdk.Color primary, Gdk.Color secondary)
        {
            var checkerboard = new Pixbuf(Colorspace.Rgb, true, 8, size, size);
            checkerboard.Fill(((uint)primary.Red >> 8 << 24) + ((uint)primary.Green >> 8 << 16) + ((uint)primary.Blue >> 8 << 8) + byte.MaxValue);
            for (var y = 0; y < size; y += checkSize)
            {
                for (var x = y / checkSize % 2 == 0 ? checkSize : 0; x < size; x += 2 * checkSize)
                {
                    for (var i = 0; i < checkSize && y + i < size; i++)
                    {
                        for (var j = 0; j < checkSize && x + j < size; j++)
                        {
                            checkerboard.SetPixel(x + j, y + i, (byte)(secondary.Red >> 8), (byte)(secondary.Green >> 8), (byte)(secondary.Blue >> 8), byte.MaxValue);
                        }
                    }
                }
            }
            return checkerboard;
        }

        public static void PreloadGameImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            var key = ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry);
            PreloadedGameImages[key] = GDImageLibrary._DDS.LoadImage(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry).AsBytes);
            PreloadedGameImagePixbufs[key] = new List<Pixbuf>
                {
                    ConvertToPixbuf(PreloadedGameImages[key]).ScaleSimple(shortestDimension, shortestDimension, InterpType.Bilinear)
                };
        }

        public static void PreloadImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            var key = ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry);
            PreloadedImages[key] = GDImageLibrary._DDS.LoadImage(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry).AsBytes);
            PreloadedImagePixbufs[resourceIndexEntry] = new List<Pixbuf>
                {
                    ConvertToPixbuf(PreloadedImages[key]).ScaleSimple(shortestDimension, shortestDimension, InterpType.Bilinear)
                };
        }

        public static unsafe void SetPixel(this Pixbuf pixbuf, int x, int y, byte r, byte g, byte b, byte a)
        {
            if (pixbuf == null || !pixbuf.HasAlpha || pixbuf.Colorspace != Colorspace.Rgb || pixbuf.BitsPerSample != 8)
            {
                throw new System.ArgumentException("Pixbuf must be RGBA with 8 bits per sample.");
            }
            if (x < 0 || x >= pixbuf.Width || y < 0 || y >= pixbuf.Height)
            {
                throw new System.ArgumentOutOfRangeException("Coordinates are out of bounds.");
            }
            byte* pixels = (byte*)pixbuf.Pixels,
            pixelPtr = pixels + (y * pixbuf.Rowstride) + (x * pixbuf.NChannels);
            pixelPtr[0] = r;
            pixelPtr[1] = g;
            pixelPtr[2] = b;
            pixelPtr[3] = a;
        }
    }
}
