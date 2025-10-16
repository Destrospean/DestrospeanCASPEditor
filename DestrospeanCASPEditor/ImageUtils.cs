using System.Collections.Generic;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ImageUtils
    {
        static Dictionary<IResourceIndexEntry, IPackage> mGameImageResources;

        public static Dictionary<IResourceIndexEntry, IPackage> GameImageResources
        {
            get
            {
                if (mGameImageResources == null)
                {
                    mGameImageResources = new Dictionary<IResourceIndexEntry, IPackage>();
                    foreach (var game in s3pi.Filetable.GameFolders.Games)
                    {
                        var enumerator = game.DDSImages.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var package = s3pi.Package.Package.OpenPackage(0, enumerator.Current.Path);
                            foreach (var resourceIndexEntry in package.FindAll(x => x.ResourceType == 0xB2D882))
                            {
                                mGameImageResources.Add(resourceIndexEntry, package);
                            }
                        }
                    }
                }
                return mGameImageResources;
            }
        }

        public static readonly Dictionary<string, List<Gdk.Pixbuf>> PreloadedGameImages = new Dictionary<string, List<Gdk.Pixbuf>>();

        public static readonly Dictionary<IResourceIndexEntry, List<Gdk.Pixbuf>> PreloadedImages = new Dictionary<IResourceIndexEntry, List<Gdk.Pixbuf>>();

        public static Gdk.Pixbuf ConvertToPixbuf(IResource imageResource)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                GDImageLibrary._DDS.LoadImage(imageResource.AsBytes).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return new Gdk.Pixbuf(stream);
            }
        }

        public static void PreloadGameImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            ImageUtils.PreloadedGameImages.Add(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry), new List<Gdk.Pixbuf>()
                {
                    ConvertToPixbuf(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry)).ScaleSimple(shortestDimension, shortestDimension, Gdk.InterpType.Bilinear)
                });
        }

        public static void PreloadImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            ImageUtils.PreloadedImages.Add(resourceIndexEntry, new List<Gdk.Pixbuf>()
                {
                    ConvertToPixbuf(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry)).ScaleSimple(shortestDimension, shortestDimension, Gdk.InterpType.Bilinear)
                });
        }
    }
}
