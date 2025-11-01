using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class WidgetUtils
    {
        public static float Scale, WineScale;

        public const int DefaultTableCellHeight = 48, DefaultTableColumnSpacing = 12;

        public static void AddPresetToNotebook(CASPart.Preset preset, Notebook notebook, Gtk.Image imageWidget, bool isSubNotebook = false)
        {
            var subNotebook = isSubNotebook ? notebook : new Notebook();
            if (!isSubNotebook)
            {
                notebook.AppendPage(subNotebook, new Label
                    {
                        Text = "Preset " + (notebook.NPages + 1).ToString()
                    });
            }
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
                        ColumnSpacing = DefaultTableColumnSpacing
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

        public static void AddPropertiesToNotebook(IPackage package, GeometryResource geometryResource, Notebook notebook, Gtk.Image imageWidget)
        {
            var scrolledWindow = new ScrolledWindow();
            var table = new Table(1, 2, false)
                {
                    ColumnSpacing = DefaultTableColumnSpacing
                };
            scrolledWindow.AddWithViewport(table);
            notebook.AppendPage(scrolledWindow, new Label
                {
                    Text = "Configuration"
                });
            AddPropertiesToTable(package, geometryResource, table, imageWidget);
            notebook.ShowAll();
        }

        public static void AddPropertiesToTable(CASPart.IComplate complate, Table table, Gtk.Image imageWidget)
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
                        HeightRequest = DefaultTableCellHeight
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
                                Color = new Color
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
                        var spinButton = new SpinButton(new Adjustment(float.Parse(value), 0, 1, .0001, 10, 0), 0, 4);
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
                        ComboBox comboBox;
                        var entries = BuildImageResourceComboBoxEntries(complate.CurrentPackage, value, out comboBox, imageWidget);
                        comboBox.Changed += (sender, e) => complate.SetValue(name, entries[comboBox.Active].Item2);
                        valueWidget = comboBox;
                        break;
                    case "vec2":
                        var hBox = new HBox();
                        var coordinates = new List<string>(value.Split(',')).ConvertAll(new Converter<string, float>(float.Parse));
                        var spinButtons = new List<SpinButton>
                            {
                                new SpinButton(new Adjustment(coordinates[0], 0, 1, .0001, 10, 0), 0, 4),
                                new SpinButton(new Adjustment(coordinates[1], 0, 1, .0001, 10, 0), 0, 4)
                            };
                        spinButtons.ForEach(x =>
                            {
                                x.ValueChanged += (sender, e) => complate.SetValue(name, spinButtons[0].Value.ToString("F4") + "," + spinButtons[1].Value.ToString("F4"));
                                hBox.PackStart(x, false, false, 0);
                            });
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
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
        }

        public static void AddPropertiesToTable(IPackage package, GeometryResource geometryResource, Table table, Gtk.Image imageWidget)
        {
            var geom = (GEOM)geometryResource.ChunkEntries[0].RCOLBlock;
            var shaders = new List<string>();
            foreach (var shader in Enum.GetValues(typeof(ShaderType)))
            {
                shaders.Add(shader.ToString());
            }
            shaders.RemoveAt(0);
            shaders.Sort();
            shaders.Insert(0, "None");
            var shaderComboBox = new ComboBox(shaders.ToArray())
                {
                    Active = shaders.IndexOf(geom.Shader.ToString()),
                    HeightRequest = DefaultTableCellHeight
                };
            shaderComboBox.Changed += (sender, e) => geom.Shader = (ShaderType)Enum.Parse(typeof(ShaderType), shaderComboBox.ActiveText);
            table.Attach(new Label
                {
                    Text = "Shader",
                    Xalign = 0
                }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.Attach(shaderComboBox, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.NRows++;
            foreach (var element in geom.Mtnf.SData)
            {
                Widget valueWidget = null;
                var alignment = new Alignment(0, .5f, 0, 0)
                    {
                        HeightRequest = DefaultTableCellHeight
                    };
                var elementFloat = element as ElementFloat;
                if (elementFloat != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementFloat.Data, 0, 1, .0001, 10, 0), 0, 4);
                    spinButton.ValueChanged += (sender, e) => elementFloat.Data = (float)spinButton.Value;
                    valueWidget = spinButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementFloat2 = element as ElementFloat2;
                if (elementFloat2 != null)
                {
                    var hBox = new HBox();
                    var spinButtons = new SpinButton[]
                        {
                            new SpinButton(new Adjustment(elementFloat2.Data0, 0, 1, .0001, 10, 0), 0, 4),
                            new SpinButton(new Adjustment(elementFloat2.Data1, 0, 1, .0001, 10, 0), 0, 4)
                        };
                    spinButtons[0].ValueChanged += (sender, e) => elementFloat2.Data0 = (float)spinButtons[0].Value;
                    spinButtons[1].ValueChanged += (sender, e) => elementFloat2.Data1 = (float)spinButtons[1].Value;
                    foreach (var spinButton in spinButtons)
                    {
                        hBox.PackStart(spinButton, false, false, 0);
                    }
                    valueWidget = hBox;
                    goto AttachLabelAndValueWidget;
                }
                var elementFloat3 = element as ElementFloat3;
                if (elementFloat3 != null)
                {
                    var colorButton = new ColorButton
                        {
                            Color = new Color
                                {
                                    Blue = (ushort)(elementFloat3.Data2 * ushort.MaxValue),
                                    Green = (ushort)(elementFloat3.Data1 * ushort.MaxValue),
                                    Red = (ushort)(elementFloat3.Data0 * ushort.MaxValue)
                                }
                        };
                    colorButton.ColorSet += (sender, e) =>
                        {
                            elementFloat3.Data0 = (float)colorButton.Color.Red / ushort.MaxValue;
                            elementFloat3.Data1 = (float)colorButton.Color.Green / ushort.MaxValue;
                            elementFloat3.Data2 = (float)colorButton.Color.Blue / ushort.MaxValue;
                        };
                    valueWidget = colorButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementFloat4 = element as ElementFloat4;
                if (elementFloat4 != null)
                {
                    var colorButton = new ColorButton
                        {
                            Alpha = (ushort)(elementFloat4.Data3 * ushort.MaxValue),
                            Color = new Color
                                {
                                    Blue = (ushort)(elementFloat4.Data2 * ushort.MaxValue),
                                    Green = (ushort)(elementFloat4.Data1 * ushort.MaxValue),
                                    Red = (ushort)(elementFloat4.Data0 * ushort.MaxValue)
                                },
                            UseAlpha = true
                        };
                    colorButton.ColorSet += (sender, e) =>
                        {
                            elementFloat4.Data0 = (float)colorButton.Color.Red / ushort.MaxValue;
                            elementFloat4.Data1 = (float)colorButton.Color.Green / ushort.MaxValue;
                            elementFloat4.Data2 = (float)colorButton.Color.Blue / ushort.MaxValue;
                            elementFloat4.Data3 = (float)colorButton.Alpha / ushort.MaxValue;
                        };
                    valueWidget = colorButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementInt = element as ElementInt;
                if (elementInt != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementInt.Data, int.MinValue, int.MaxValue, 1, 10, 0), 0, 0);
                    spinButton.ValueChanged += (sender, e) => elementInt.Data = spinButton.ValueAsInt;
                    valueWidget = spinButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementTextureRef = element as ElementTextureRef;
                if (elementTextureRef != null)
                {
                    alignment.Xscale = 1;
                    ComboBox comboBox;
                    var entries = BuildImageResourceComboBoxEntries(package, ResourceUtils.ReverseEvaluateResourceKey(element.ParentTGIBlocks[elementTextureRef.Index]), out comboBox, imageWidget);
                    comboBox.Changed += (sender, e) =>
                        {
                            var index = element.ParentTGIBlocks.FindIndex(x => ResourceUtils.ReverseEvaluateResourceKey(x) == entries[comboBox.Active].Item2);
                            if (index == -1)
                            {
                                element.ParentTGIBlocks.Add(new TGIBlock(0, null, ResourceUtils.EvaluateImageResourceKey(package, entries[comboBox.Active].Item2).Item2));
                                index = element.ParentTGIBlocks.Count - 1;
                            }
                            elementTextureRef.Index = index;
                        };
                    valueWidget = comboBox;
                }
                AttachLabelAndValueWidget:
                table.Attach(new Label
                    {
                        Text = element.Field.ToString(),
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                alignment.Add(valueWidget);
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
        }

        public static List<Tuple<Pixbuf, string>> BuildImageResourceComboBoxEntries(IPackage package, string currentValue, out ComboBox comboBox, Gtk.Image imageWidget)
        {
            var listStore = new ListStore(typeof(Pixbuf), typeof(string));
            var entries = package.FindAll(x => x.ResourceType == 0xB2D882).ConvertAll(new Converter<IResourceIndexEntry, Tuple<Pixbuf, string>>(x => new Tuple<Pixbuf, string>(ImageUtils.PreloadedImages[x][1], ResourceUtils.ReverseEvaluateResourceKey(x))));
            entries.ForEach(x => listStore.AppendValues(x.Item1, x.Item2));
            var missing = ResourceUtils.MissingResourceKeys.Contains(currentValue);
            if (!entries.Exists(x => x.Item2 == currentValue))
            {
                if (!ImageUtils.PreloadedGameImages.ContainsKey(currentValue) && !missing)
                {
                    try
                    {
                        var evaluated = ResourceUtils.EvaluateImageResourceKey(package, currentValue);
                        ImageUtils.PreloadGameImage(evaluated.Item1, evaluated.Item2, imageWidget);
                        ImageUtils.PreloadedGameImages[currentValue].Add(ImageUtils.PreloadedGameImages[currentValue][0].ScaleSimple(32, 32, InterpType.Bilinear));
                    }
                    catch
                    {
                        ResourceUtils.MissingResourceKeys.Add(currentValue);
                        missing = true;
                    }
                }
                entries.Add(new Tuple<Pixbuf, string>(missing ? null : ImageUtils.PreloadedGameImages[currentValue][1], currentValue));
                listStore.AppendValues(entries[entries.Count - 1].Item1, entries[entries.Count - 1].Item2);
            }
            comboBox = new ComboBox
                {
                    Active = entries.FindIndex(x => x.Item2 == currentValue),
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
            return entries;
        }

        public static Frame GetFlagsInNewFrame(string label, CASPart casPart, Type enumType, Enum enumInstance, params string[] propertyPathParts)
        {
            var frame = new Frame
                {
                    Label = label
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
                        var propertyInfo = property.GetType().GetProperty(propertyPathParts[0]);
                        if (propertyPathParts.Length > 1)
                        {
                            for (var i = 1; i < propertyPathParts.Length; i++)
                            {
                                property = propertyInfo.GetValue(property);
                                propertyInfo = property.GetType().GetProperty(propertyPathParts[i]);
                            }
                        }
                        try
                        {
                            var value = (byte)propertyInfo.GetValue(property);
                            propertyInfo.SetValue(property, (byte)(value ^ (byte)flag));
                            goto FinalSteps;
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
                        FinalSteps:
                        //casPart.CurrentPackage.ReplaceResource(casPart.ResourceIndexEntry, casPart.CASPartResource);
                        return;
                    };
                vBox.PackStart(checkButton, false, false, 0);
            }
            return frame;
        }
    }
}

