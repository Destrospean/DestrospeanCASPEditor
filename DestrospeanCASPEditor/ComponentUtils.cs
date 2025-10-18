using System;
using System.Collections.Generic;
using Gtk;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ComponentUtils
    {
        public static float Scale, WineScale;

        public static void AddPresetToNotebook(CASPart.Preset preset, Notebook notebook, Image imageWidget)
        {
            var subNotebook = new Notebook();
            notebook.AppendPage(subNotebook, new Label
                {
                    Text = "Preset " + notebook.NPages.ToString()
                });
            var complates = new List<CASPart.IComplate>
                {
                    preset
                };
            complates.AddRange(preset.Patterns);
            foreach (var complate in complates)
            {
                var pattern = complate as CASPart.Pattern;
                var scrolledWindow = new ScrolledWindow();
                var table = new Table(1, 2, false)
                    {
                        ColumnSpacing = 12
                    };
                scrolledWindow.AddWithViewport(table);
                subNotebook.AppendPage(scrolledWindow, new Label
                    {
                        Text = pattern == null ? "Configuration" : pattern.Name
                    });
                AddPropertiesToTable(complate, table, imageWidget);
            }
            notebook.ShowAll();
        }

        public static void AddPropertiesToNotebook(s3pi.Interfaces.IPackage package, GeometryResource geometryResource, Notebook notebook, Image imageWidget)
        {
            var scrolledWindow = new ScrolledWindow();
            var table = new Table(1, 2, false)
                {
                    ColumnSpacing = 12
                };
            scrolledWindow.AddWithViewport(table);
            notebook.AppendPage(scrolledWindow, new Label
                {
                    Text = "Configuration"
                });
            AddPropertiesToTable(package, geometryResource, table, imageWidget);
            notebook.ShowAll();
        }

        public static void AddPropertiesToTable(CASPart.IComplate complate, Table table, Image imageWidget)
        {
            var propertyNames = complate.PropertyNames;
            propertyNames.Sort();
            foreach (var name in propertyNames)
            {
                string type;
                if (!complate.PropertiesTyped.TryGetValue(name, out type))
                {
                    continue;
                }
                Widget valueWidget = null;
                var alignment = new Alignment(0, .5f, 1, 0)
                    {
                        HeightRequest = 48
                    };
                var value = complate.GetValue(name);
                switch (type)
                {
                    case "bool":
                        var checkButton = new CheckButton
                            {
                                Active = bool.Parse(value),
                                UseUnderline = false
                            };
                        checkButton.Toggled += (sender, e) => complate.SetValue(name, checkButton.Active.ToString());
                        valueWidget = checkButton;
                        break;
                    case "color":
                        alignment.Xscale = 0;
                        var rgba = new List<string>(value.Split(',')).ConvertAll(new Converter<string, ushort>(x => (ushort)(float.Parse(x) * ushort.MaxValue)));
                        var colorButton = new ColorButton
                            {
                                Alpha = rgba[3],
                                Color = new Gdk.Color
                                    {
                                        Blue = rgba[2],
                                        Green = rgba[1],
                                        Red = rgba[0]
                                    },
                                UseAlpha = true
                            };
                        colorButton.ColorSet += (sender, e) =>
                            {
                                rgba = new List<ushort>
                                    {
                                        colorButton.Color.Red,
                                        colorButton.Color.Green,
                                        colorButton.Color.Blue,
                                        colorButton.Alpha
                                    };
                                var output = "";
                                rgba.ForEach(x => output += "," + ((float)x / ushort.MaxValue).ToString("F4"));
                                complate.SetValue(name, output.Substring(1));
                            };
                        valueWidget = colorButton;
                        break;
                    case "float":
                        alignment.Xscale = 0;
                        var spinButton = new SpinButton(new Adjustment(float.Parse(value), 0, 1, .0001, 10, 0), 10, 4);
                        spinButton.ValueChanged += (sender, e) => complate.SetValue(name, spinButton.Value.ToString("F4"));
                        valueWidget = spinButton;
                        break;
                    case "pattern":
                        var entry = new Entry
                            {
                                Text = value
                            };
                        entry.Changed += (sender, e) => complate.SetValue(name, entry.Text);
                        valueWidget = entry;
                        break;
                    case "texture":
                        var listStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string));
                        var entries = complate.CurrentPackage.FindAll(x => x.ResourceType == 0xB2D882).ConvertAll(new Converter<s3pi.Interfaces.IResourceIndexEntry, Tuple<Gdk.Pixbuf, string>>(x => new Tuple<Gdk.Pixbuf, string>(ImageUtils.PreloadedImages[x][1], ResourceUtils.ReverseEvaluateResourceKey(x))));
                        entries.ForEach(x => listStore.AppendValues(x.Item1, x.Item2));
                        var missing = ResourceUtils.MissingResourceKeys.Contains(value);
                        if (!entries.Exists(x => x.Item2 == value))
                        {
                            if (!ImageUtils.PreloadedGameImages.ContainsKey(value) && !missing)
                            {
                                try
                                {
                                    var evaluated = ResourceUtils.EvaluateImageResourceKey(complate.CurrentPackage, value);
                                    ImageUtils.PreloadGameImage(evaluated.Item1, evaluated.Item2, imageWidget);
                                    ImageUtils.PreloadedGameImages[value].Add(ImageUtils.PreloadedGameImages[value][0].ScaleSimple(32, 32, Gdk.InterpType.Bilinear));
                                }
                                catch
                                {
                                    ResourceUtils.MissingResourceKeys.Add(value);
                                    missing = true;
                                }
                            }
                            entries.Add(new Tuple<Gdk.Pixbuf, string>(missing ? null : ImageUtils.PreloadedGameImages[value][1], value));
                            listStore.AppendValues(entries[entries.Count - 1].Item1, entries[entries.Count - 1].Item2);
                        }
                        var comboBox = new ComboBox
                            {
                                Active = entries.FindIndex(x => x.Item2 == value),
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
                        comboBox.Changed += (sender, e) => complate.SetValue(name, entries[comboBox.Active].Item2);
                        valueWidget = comboBox;
                        break;
                    case "vec2":
                        var hBox = new HBox();
                        var coordinates = new List<string>(value.Split(',')).ConvertAll(new Converter<string, float>(float.Parse));
                        SpinButton spinButtonX = new SpinButton(new Adjustment(coordinates[0], 0, 1, .0001, 10, 0), 10, 4), spinButtonY = new SpinButton(new Adjustment(coordinates[1], 0, 1, .0001, 10, 0), 10, 4);
                        EventHandler valueChanged = (sender, e) => complate.SetValue(name, spinButtonX.Value.ToString("F4") + "," + spinButtonY.Value.ToString("F4"));
                        spinButtonX.ValueChanged += valueChanged;
                        spinButtonY.ValueChanged += valueChanged;
                        hBox.PackStart(spinButtonX, false, false, 0);
                        hBox.PackStart(spinButtonY, false, false, 0);
                        valueWidget = hBox;
                        break;
                }
                table.Attach(new Label
                    {
                        Text = name,
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                alignment.Add(valueWidget);
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
        }

        public static void AddPropertiesToTable(s3pi.Interfaces.IPackage package, GeometryResource geometryResource, Table table, Image imageWidget)
        {
            var geom = (GEOM)geometryResource.ChunkEntries[0].RCOLBlock;
            Console.WriteLine(geom.Shader.ToString());
            foreach (var element in geom.Mtnf.SData)
            {
                Widget valueWidget = null;
                var alignment = new Alignment(0, .5f, 0, 0)
                    {
                        HeightRequest = 48
                    };
                var elementFloat = element as ElementFloat;
                if (elementFloat != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementFloat.Data, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4);
                    spinButton.ValueChanged += (sender, e) => elementFloat.Data = (float)spinButton.Value;
                    valueWidget = spinButton;
                }
                var elementFloat2 = element as ElementFloat2;
                if (elementFloat2 != null)
                {
                    var hBox = new HBox();
                    var spinButtons = new SpinButton[]
                        {
                            new SpinButton(new Adjustment(elementFloat2.Data0, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat2.Data1, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4)
                        };
                    spinButtons[0].ValueChanged += (sender, e) => elementFloat2.Data0 = (float)spinButtons[0].Value;
                    spinButtons[1].ValueChanged += (sender, e) => elementFloat2.Data1 = (float)spinButtons[1].Value;
                    foreach (var spinButton in spinButtons)
                    {
                        hBox.PackStart(spinButton, false, false, 0);
                    }
                    valueWidget = hBox;
                }
                var elementFloat3 = element as ElementFloat3;
                if (elementFloat3 != null)
                {
                    var hBox = new HBox();
                    var spinButtons = new SpinButton[]
                        {
                            new SpinButton(new Adjustment(elementFloat3.Data0, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat3.Data1, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat3.Data2, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4)
                        };
                    spinButtons[0].ValueChanged += (sender, e) => elementFloat3.Data0 = (float)spinButtons[0].Value;
                    spinButtons[1].ValueChanged += (sender, e) => elementFloat3.Data1 = (float)spinButtons[1].Value;
                    spinButtons[2].ValueChanged += (sender, e) => elementFloat3.Data2 = (float)spinButtons[2].Value;
                    foreach (var spinButton in spinButtons)
                    {
                        hBox.PackStart(spinButton, false, false, 0);
                    }
                    valueWidget = hBox;
                }
                var elementFloat4 = element as ElementFloat4;
                if (elementFloat4 != null)
                {
                    var hBox = new HBox();
                    var spinButtons = new SpinButton[]
                        {
                            new SpinButton(new Adjustment(elementFloat4.Data0, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat4.Data1, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat4.Data2, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4),
                            new SpinButton(new Adjustment(elementFloat4.Data3, float.MinValue, float.MaxValue, .0001, 10, 0), 10, 4)
                        };
                    spinButtons[0].ValueChanged += (sender, e) => elementFloat4.Data0 = (float)spinButtons[0].Value;
                    spinButtons[1].ValueChanged += (sender, e) => elementFloat4.Data1 = (float)spinButtons[1].Value;
                    spinButtons[2].ValueChanged += (sender, e) => elementFloat4.Data2 = (float)spinButtons[2].Value;
                    spinButtons[3].ValueChanged += (sender, e) => elementFloat4.Data3 = (float)spinButtons[3].Value;
                    foreach (var spinButton in spinButtons)
                    {
                        hBox.PackStart(spinButton, false, false, 0);
                    }
                    valueWidget = hBox;
                }
                var elementInt = element as ElementInt;
                if (elementInt != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementInt.Data, int.MinValue, int.MaxValue, 1, 10, 0), 0, 0);
                    spinButton.ValueChanged += (sender, e) => elementInt.Data = spinButton.ValueAsInt;
                    valueWidget = spinButton;
                }
                var elementTextureRef = element as ElementTextureRef;
                if (elementTextureRef != null)
                {
                    var value = ResourceUtils.ReverseEvaluateResourceKey(element.ParentTGIBlocks[elementTextureRef.Index]);
                    var listStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string));
                    var entries = package.FindAll(x => x.ResourceType == 0xB2D882).ConvertAll(new Converter<s3pi.Interfaces.IResourceIndexEntry, Tuple<Gdk.Pixbuf, string>>(x => new Tuple<Gdk.Pixbuf, string>(ImageUtils.PreloadedImages[x][1], ResourceUtils.ReverseEvaluateResourceKey(x))));
                    entries.ForEach(x => listStore.AppendValues(x.Item1, x.Item2));
                    var missing = ResourceUtils.MissingResourceKeys.Contains(value);
                    if (!entries.Exists(x => x.Item2 == value))
                    {
                        if (!ImageUtils.PreloadedGameImages.ContainsKey(value) && !missing)
                        {
                            try
                            {
                                var evaluated = ResourceUtils.EvaluateImageResourceKey(package, value);
                                ImageUtils.PreloadGameImage(evaluated.Item1, evaluated.Item2, imageWidget);
                                ImageUtils.PreloadedGameImages[value].Add(ImageUtils.PreloadedGameImages[value][0].ScaleSimple(32, 32, Gdk.InterpType.Bilinear));
                            }
                            catch
                            {
                                ResourceUtils.MissingResourceKeys.Add(value);
                                missing = true;
                            }
                        }
                        entries.Add(new Tuple<Gdk.Pixbuf, string>(missing ? null : ImageUtils.PreloadedGameImages[value][1], value));
                        listStore.AppendValues(entries[entries.Count - 1].Item1, entries[entries.Count - 1].Item2);
                    }
                    var comboBox = new ComboBox
                        {
                            Active = entries.FindIndex(x => x.Item2 == value),
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
                    comboBox.Changed += (sender, e) =>
                        {
                            var index = element.ParentTGIBlocks.FindIndex(x => ResourceUtils.ReverseEvaluateResourceKey(x) == entries[comboBox.Active].Item2);
                            if (index == -1)
                            {
                                element.ParentTGIBlocks.Add(new s3pi.Interfaces.TGIBlock(0, null, ResourceUtils.EvaluateImageResourceKey(package, entries[comboBox.Active].Item2).Item2));
                                index = element.ParentTGIBlocks.Count - 1;
                            }
                            elementTextureRef.Index = index;
                        };
                    valueWidget = comboBox;
                }
                table.Attach(new Label
                    {
                        Text = element.Field.ToString(),
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                alignment.Add(valueWidget);
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
        }

        public static Frame GetFlagsInNewFrame(CASPart casPart, Type enumType, Enum enumInstance, params string[] propertyPathParts)
        {
            var frame = new Frame
                {
                    Label = propertyPathParts[propertyPathParts.Length - 1],
                };
            var scrolledWindow = new ScrolledWindow();
            var vBox = new VBox();
            frame.Add(scrolledWindow);
            scrolledWindow.AddWithViewport(vBox);
            foreach (var flag in Enum.GetValues(enumType))
            {
                var checkButton = new CheckButton(flag.ToString())
                    {
                        Active = enumInstance.HasFlag((Enum)flag),
                        UseUnderline = false
                    };
                checkButton.Toggled += (sender, e) =>
                    {
                        object property = casPart.CASPartResource;
                        var propertyInfo = property.GetType().GetProperty(propertyPathParts[0].Replace(" ", ""));
                        if (propertyPathParts.Length > 1)
                        {
                            for (var i = 1; i < propertyPathParts.Length; i++)
                            {
                                property = propertyInfo.GetValue(property);
                                propertyInfo = property.GetType().GetProperty(propertyPathParts[i].Replace(" ", ""));
                            }
                        }
                        try
                        {
                            var value = (byte)propertyInfo.GetValue(property);
                            propertyInfo.SetValue(property, (byte)(value ^ (byte)flag));
                        }
                        catch (InvalidCastException)
                        {
                        }
                        try
                        {
                            var value = (uint)propertyInfo.GetValue(property);
                            propertyInfo.SetValue(property, value ^ (uint)flag);
                        }
                        catch (InvalidCastException)
                        {
                        }
                        casPart.CurrentPackage.ReplaceResource(casPart.ResourceIndexEntry, casPart.CASPartResource);
                    };
                vBox.PackStart(checkButton, false, false, 0);
            }
            return frame;
        }
    }
}

