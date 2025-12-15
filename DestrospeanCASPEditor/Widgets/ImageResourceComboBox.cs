using Destrospean.S3PIAbstractions;
using Gdk;
using Gtk;

namespace Destrospean.DestrospeanCASPEditor.Widgets
{
    public class ImageResourceComboBox : ComboBox
    {
        protected readonly System.Collections.Generic.List<ImageResourceComboBoxEntry> mEntries;

        public int EntryCount
        {
            get
            {
                return mEntries.Count;
            }
        }

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
            var entries = package.GetResourceList.ConvertAll(ResourceUtils.ReverseEvaluateResourceKey).FindAll(ImageUtils.PreloadedImagePixbufs.ContainsKey).ConvertAll(x => new ImageResourceComboBoxEntry(ImageUtils.PreloadedImagePixbufs[x][1], x));
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
            entries.Add(new ImageResourceComboBoxEntry(null, "<Specify key>"));
            listStore.AppendValues(entries[entries.Count - 1].Image, entries[entries.Count - 1].Label);
            var comboBox = new ImageResourceComboBox(entries)
                {
                    Active = entries.FindIndex(x => x.Label == currentValue),
                    Model = listStore
                };
            var comboBoxLastActive = comboBox.Active;
            comboBox.Changed += (sender, e) =>
                {
                    if (comboBox.Active == entries.Count - 1)
                    {
                        var textEntryDialog = new TextEntryDialog("Specify Key", "Specify the image resource's key (in the format of \"key:########:########:################\"):", MainWindow.Singleton);
                        if (textEntryDialog.Run() == (int)ResponseType.Ok)
                        {
                            var existingEntryIndex = entries.FindIndex(x => x.Label == textEntryDialog.TextEntryValue);
                            if (existingEntryIndex == -1)
                            {
                                var exists = true;
                                if (!ImageUtils.PreloadedGameImagePixbufs.ContainsKey(textEntryDialog.TextEntryValue))
                                {
                                    try
                                    {
                                        var evaluated = package.EvaluateImageResourceKey(textEntryDialog.TextEntryValue);
                                        evaluated.Package.PreloadGameImage(evaluated.ResourceIndexEntry, imageWidget);
                                        ImageUtils.PreloadedGameImagePixbufs[textEntryDialog.TextEntryValue].Add(ImageUtils.PreloadedGameImagePixbufs[textEntryDialog.TextEntryValue][0].ScaleSimple(WidgetUtils.SmallImageSize, WidgetUtils.SmallImageSize, InterpType.Bilinear));
                                    }
                                    catch
                                    {
                                        comboBox.Active = comboBoxLastActive;
                                        exists = false;
                                    }
                                }
                                if (exists)
                                {
                                    entries.Insert(entries.Count - 1, new ImageResourceComboBoxEntry(ImageUtils.PreloadedGameImagePixbufs[textEntryDialog.TextEntryValue][1], textEntryDialog.TextEntryValue));
                                    listStore.InsertWithValues(entries.Count - 2, entries[entries.Count - 2].Image, entries[entries.Count - 2].Label);
                                    comboBox.Active = entries.Count - 2;
                                }
                            }
                            else
                            {
                                comboBox.Active = existingEntryIndex;
                            }
                        }
                        else
                        {
                            comboBox.Active = comboBoxLastActive;
                        }
                        textEntryDialog.Destroy();
                    }
                    else
                    {
                        comboBoxLastActive = comboBox.Active;
                    }
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
