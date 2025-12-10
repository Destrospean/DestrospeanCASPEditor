using Destrospean.S3PIAbstractions;
using Gdk;
using Gtk;

namespace Destrospean.DestrospeanCASPEditor.Widgets
{
    public class ImageResourceComboBox : ComboBox
    {
        protected readonly System.Collections.Generic.List<ImageResourceComboBoxEntry> mEntries;

        public ImageResourceComboBoxEntry this[int index]
        {
            get
            {
                return mEntries[index];
            }
            set
            {
                mEntries[index] = value;
            }
        }

        public struct ImageResourceComboBoxEntry
        {
            public readonly Pixbuf Image;

            public readonly string Label;

            public ImageResourceComboBoxEntry(Pixbuf image, string label)
            {
                Image = image;
                Label = label;
            }
        }

        protected ImageResourceComboBox(System.Collections.Generic.List<ImageResourceComboBoxEntry> entries) : base()
        {
            mEntries = entries;
        }

        public static ImageResourceComboBox CreateInstance(s3pi.Interfaces.IPackage package, string currentValue, Gtk.Image imageWidget)
        {
            var entries = package.FindAll(x => x.ResourceType == ResourceUtils.GetResourceType("_IMG")).ConvertAll(new System.Converter<s3pi.Interfaces.IResourceIndexEntry, ImageResourceComboBoxEntry>(x =>
                {
                    var key = x.ReverseEvaluateResourceKey();
                    return new ImageResourceComboBoxEntry(ImageUtils.PreloadedImagePixbufs[key][1], key);
                }));
            var listStore = new ListStore(typeof(Pixbuf), typeof(string));
            entries.ForEach(x => listStore.AppendValues(x.Image, x.Label));
            var missing = ResourceUtils.MissingResourceKeys.Contains(currentValue);
            if (!entries.Exists(x => x.Label == currentValue))
            {
                if (!ImageUtils.PreloadedGameImagePixbufs.ContainsKey(currentValue) && !missing)
                {
                    try
                    {
                        var evaluated = package.EvaluateImageResourceKey(currentValue);
                        evaluated.Package.PreloadGameImage(evaluated.ResourceIndexEntry, imageWidget);
                        ImageUtils.PreloadedGameImagePixbufs[currentValue].Add(ImageUtils.PreloadedGameImagePixbufs[currentValue][0].ScaleSimple(WidgetUtils.SmallImageSize, WidgetUtils.SmallImageSize, InterpType.Bilinear));
                    }
                    catch
                    {
                        ResourceUtils.MissingResourceKeys.Add(currentValue);
                        missing = true;
                    }
                }
                entries.Add(new ImageResourceComboBoxEntry(missing ? null : ImageUtils.PreloadedGameImagePixbufs[currentValue][1], currentValue));
                listStore.AppendValues(entries[entries.Count - 1].Image, entries[entries.Count - 1].Label);
            }
            var comboBox = new ImageResourceComboBox(entries)
                {
                    Active = entries.FindIndex(x => x.Label == currentValue),
                    Model = listStore
                };
            var pixbufRenderer = new CellRendererPixbuf
                {
                    Xpad = 4
                };
            var textRenderer = new CellRendererText
                {
                    Xpad = 4
                };
            comboBox.PackStart(pixbufRenderer, false);
            comboBox.AddAttribute(pixbufRenderer, "pixbuf", 0);
            comboBox.PackStart(textRenderer, false);
            comboBox.AddAttribute(textRenderer, "text", 1);
            return comboBox;
        }
    }
}
