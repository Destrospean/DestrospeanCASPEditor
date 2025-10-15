namespace Destrospean.DestrospeanCASPEditor
{
    public static class ImageUtils
    {
        public static Gdk.Pixbuf ConvertToPixbuf(s3pi.Interfaces.IResource imageResource)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                GDImageLibrary._DDS.LoadImage(imageResource.AsBytes).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return new Gdk.Pixbuf(stream);
            }
        }

        public static Gdk.Pixbuf Preload_IMG(Gtk.Image imageWidget, s3pi.Interfaces.IResource imageResource)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            return ConvertToPixbuf(imageResource).ScaleSimple(shortestDimension, shortestDimension, Gdk.InterpType.Bilinear);
        }
    }
}
