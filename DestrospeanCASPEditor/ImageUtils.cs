using System.Collections.Generic;
using Gdk;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ImageUtils
    {
        static Dictionary<IResourceIndexEntry, IPackage> mGameImageResourcePackages;

        public static Dictionary<IResourceIndexEntry, IPackage> GameImageResourcePackages
        {
            get
            {
                if (mGameImageResourcePackages == null)
                {
                    mGameImageResourcePackages = new Dictionary<IResourceIndexEntry, IPackage>();
                    foreach (var game in s3pi.Filetable.GameFolders.Games)
                    {
                        var enumerator = game.DDSImages.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var package = s3pi.Package.Package.OpenPackage(0, enumerator.Current.Path);
                            foreach (var resourceIndexEntry in package.FindAll(x => x.ResourceType == 0xB2D882))
                            {
                                mGameImageResourcePackages.Add(resourceIndexEntry, package);
                            }
                        }
                    }
                }
                return mGameImageResourcePackages;
            }
        }

        public static readonly Dictionary<string, List<Pixbuf>> PreloadedGameImages = new Dictionary<string, List<Pixbuf>>();

        public static readonly Dictionary<IResourceIndexEntry, List<Pixbuf>> PreloadedImages = new Dictionary<IResourceIndexEntry, List<Pixbuf>>();

        public static Pixbuf ConvertToPixbuf(IResource imageResource)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                GDImageLibrary._DDS.LoadImage(imageResource.AsBytes).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return new Pixbuf(stream);
            }
        }

        public static void PreloadGameImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            PreloadedGameImages.Add(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry), new List<Pixbuf>
                {
                    ConvertToPixbuf(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry)).ScaleSimple(shortestDimension, shortestDimension, InterpType.Bilinear)
                });
        }

        public static void PreloadImage(IPackage package, IResourceIndexEntry resourceIndexEntry, Gtk.Image imageWidget)
        {
            var shortestDimension = System.Math.Min(imageWidget.HeightRequest, imageWidget.WidthRequest);
            PreloadedImages.Add(resourceIndexEntry, new List<Pixbuf>
                {
                    ConvertToPixbuf(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry)).ScaleSimple(shortestDimension, shortestDimension, InterpType.Bilinear)
                });
        }
    }
}
