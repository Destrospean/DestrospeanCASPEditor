using System.Collections.Generic;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ImageUtils
    {
        public static readonly Dictionary<s3pi.Interfaces.IResourceIndexEntry, List<Gdk.Pixbuf>> PreloadedImages = new Dictionary<s3pi.Interfaces.IResourceIndexEntry, List<Gdk.Pixbuf>>();

        public static Gdk.Pixbuf ConvertToPixbuf(s3pi.Interfaces.IResource imageResource)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                GDImageLibrary._DDS.LoadImage(imageResource.AsBytes).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return new Gdk.Pixbuf(stream);
            }
        }

        public static void PreloadImage(Gtk.Image imageWidget, s3pi.Interfaces.IPackage package, s3pi.Interfaces.IResourceIndexEntry resourceIndexEntry)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            ImageUtils.PreloadedImages.Add(resourceIndexEntry, new List<Gdk.Pixbuf>()
                {
                    ConvertToPixbuf(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry)).ScaleSimple(shortestDimension, shortestDimension, Gdk.InterpType.Bilinear)
                });
        }
    }
}
