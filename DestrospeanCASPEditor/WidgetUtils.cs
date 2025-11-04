using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using System.Reflection;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class WidgetUtils
    {
        public static float Scale, WineScaleDenominator;

        public static int DefaultTableCellHeight
        {
            get
            {
                return (int)(24 * Scale);
            }
        }

        public static uint DefaultTableColumnSpacing
        {
            get
            {
                return (uint)(6 * Scale);
            }
        }

        public static int SmallImageHeight
        {
            get
            {
                return (int)(16 * Scale);
            }
        }

        public static void AddPropertiesToNotebook(IPackage package, GeometryResource geometryResource, Notebook notebook, Gtk.Image imageWidget, Gtk.Window window)
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
            AddPropertiesToTable(package, geometryResource, table, scrolledWindow, imageWidget, window);
            notebook.ShowAll();
        }

        public static void AddPropertiesToTable(IPackage package, GeometryResource geometryResource, Table table, ScrolledWindow scrolledWindow, Gtk.Image imageWidget, Gtk.Window window)
        {
            var geom = (GEOM)geometryResource.ChunkEntries[0].RCOLBlock;
            var shaders = new List<string>(Enum.GetNames(typeof(ShaderType)));
            shaders.RemoveAt(0);
            shaders.Sort();
            shaders.Insert(0, "None");
            var shaderComboBoxAlignment = new Alignment(0, .5f, 1, 0)
                {
                    HeightRequest = DefaultTableCellHeight
                };
            var shaderComboBox = new ComboBox(shaders.ToArray())
                {
                    Active = shaders.IndexOf(geom.Shader.ToString())
                };
            shaderComboBoxAlignment.Add(shaderComboBox);
            shaderComboBox.Changed += (sender, e) => geom.Shader = (ShaderType)Enum.Parse(typeof(ShaderType), shaderComboBox.ActiveText);
            table.Attach(new Label
                {
                    Text = "Shader",
                    Xalign = 0
                }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.Attach(shaderComboBoxAlignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
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
            var propertyButtonHBox = new HBox(false, 4);
            propertyButtonHBox.PackStart(new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                {
                    Xalign = 1
                }, true, true, 0);
            propertyButtonHBox.PackStart(new Label
                {
                    Text = "Add Property",
                    Xalign = 0
                }, true, true, 0);
            var addPropertyButton = new Button();
            addPropertyButton.Add(propertyButtonHBox);
            addPropertyButton.Clicked += (sender, e) =>
                {
                    var addGEOMPropertyDialog = new AddGEOMPropertyDialog(window);
                    if (addGEOMPropertyDialog.Run() == (int)ResponseType.Ok)
                    {
                        foreach (var child in table.Children)
                        {
                            table.Remove(child);
                        }
                        var element = (ShaderData)Activator.CreateInstance(addGEOMPropertyDialog.DataType, 0, new EventHandler((sender1, e1) => 
                            {
                            }));
                        element.Field = addGEOMPropertyDialog.Field;
                        geom.Mtnf.SData.Add(element);
                        AddPropertiesToTable(package, geometryResource, table, scrolledWindow, imageWidget, window);
                        table.ShowAll();
                        scrolledWindow.Vadjustment.Value = scrolledWindow.Vadjustment.Upper;
                    }
                    addGEOMPropertyDialog.Destroy();
                };
            table.Attach(addPropertyButton, 0, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.NRows++;
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
                        ImageUtils.PreloadedGameImages[currentValue].Add(ImageUtils.PreloadedGameImages[currentValue][0].ScaleSimple(WidgetUtils.SmallImageHeight, WidgetUtils.SmallImageHeight, InterpType.Bilinear));
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

        public static Frame GetEnumPropertyCheckButtonsInNewFrame(string label, object propertyHolder, params string[] propertyPathComponents)
        {
            var property = propertyHolder;
            var propertyInfo = property.GetType().GetProperty(propertyPathComponents[0]);
            for (var i = 1; i < propertyPathComponents.Length; i++)
            {
                property = propertyInfo.GetValue(property, null);
                propertyInfo = property.GetType().GetProperty(propertyPathComponents[i]);
            }
            var enumInstance = (Enum)propertyInfo.GetValue(property, null);
            bool disableToggled = false, isFlagType = enumInstance.GetType().IsDefined(typeof(FlagsAttribute), false);
            var frame = new Frame
                {
                    Label = label
                };
            RadioButton groupRadioButton = null;
            var scrolledWindow = new ScrolledWindow();
            var vBox = new VBox();
            frame.Add(scrolledWindow);
            scrolledWindow.AddWithViewport(vBox);
            foreach (var value in Enum.GetValues(enumInstance.GetType()))
            {
                CheckButton checkButton;
                if (isFlagType)
                {
                    checkButton = new CheckButton(value.ToString())
                        {
                            Active = enumInstance.HasFlag((Enum)value),
                            UseUnderline = false
                        };
                }
                else
                {
                    disableToggled = true;
                    checkButton = new RadioButton(value.ToString())
                        {
                            UseUnderline = false
                        };
                    if (groupRadioButton == null)
                    {
                        groupRadioButton = (RadioButton)checkButton;
                    }
                    else
                    {
                        ((RadioButton)checkButton).Group = groupRadioButton.Group;
                    }
                    checkButton.Active = enumInstance.ToString() == value.ToString();
                    disableToggled = false;
                }
                checkButton.Toggled += (sender, e) =>
                    {
                        if (disableToggled)
                        {
                            return;
                        }
                        switch (enumInstance.GetType().GetEnumUnderlyingType().Name)
                        {
                            case "Byte":
                                propertyInfo.SetValue(property, isFlagType ? (byte)((byte)(object)enumInstance ^ (byte)value) : value, null);
                                break;
                            case "UInt32":
                                propertyInfo.SetValue(property, isFlagType ? (uint)(object)enumInstance ^ (uint)value : value, null);
                                break;
                        }
                    };
                vBox.PackStart(checkButton, false, false, 0);
            }
            return frame;
        }

        public static void RescaleAndReposition(this Gtk.Window self, Gtk.Window parent)
        {
            self.SetSizeRequest(self.WidthRequest == -1 ? -1 : (int)(self.WidthRequest * WidgetUtils.Scale), self.HeightRequest == -1 ? -1 : (int)(self.HeightRequest * WidgetUtils.Scale));
            int parentHeight, parentWidth, parentX, parentY;
            parent.GetPosition(out parentX, out parentY);
            parent.GetSize(out parentWidth, out parentHeight);
            self.Move(parentX + parentWidth / 2 - self.WidthRequest / 2, parentY + parentHeight / 2 - self.HeightRequest / 2);
        }
    }
}
