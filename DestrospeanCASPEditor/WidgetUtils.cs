using System;
using System.Collections.Generic;
using Destrospean.CmarNYCBorrowed;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Destrospean.S3PIAbstractions;
using Gdk;
using Gtk;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class WidgetUtils
    {
        public static uint DefaultTableColumnSpacing
        {
            get
            {
                return (uint)(6 * Scale);
            }
        }

        public static float Scale, WineScaleDenominator;

        public static int SmallImageSize
        {
            get
            {
                return (int)(16 * Scale);
            }
        }

        public static void AddProperties(this Notebook notebook, IPackage package, GeometryResource geometryResource, Gtk.Image imageWidget, int pageIndexOffset = 0)
        {
            var scrolledWindow = new ScrolledWindow();
            var table = new Table(1, 2, false)
                {
                    ColumnSpacing = DefaultTableColumnSpacing
                };
            scrolledWindow.AddWithViewport(table);
            notebook.AppendPage(scrolledWindow, new Label("GEOM " + (notebook.NPages + pageIndexOffset).ToString()));
            table.AddProperties(package, geometryResource, scrolledWindow, imageWidget);
            table.SizeAllocated += (o, args) =>
                {
                    var maxHeight = 0;
                    foreach (var child in table.Children)
                    {
                        maxHeight = Math.Max(child.Allocation.Height, maxHeight);
                    }
                    foreach (var child in table.Children)
                    {
                        child.HeightRequest = maxHeight;
                    }
                };
            notebook.ShowAll();
        }

        public static void AddProperties(this Table table, IPackage package, GeometryResource geometryResource, ScrolledWindow scrolledWindow, Gtk.Image imageWidget)
        {
            var mainWindow = MainWindow.Singleton;
            var geometryResourceKey = "";
            foreach (var geometryResourceKvp in mainWindow.PreloadedGeometryResources)
            {
                if (geometryResourceKvp.Value == geometryResource)
                {
                    geometryResourceKey = geometryResourceKvp.Key;
                    break;
                }
            }
            var geom = (meshExpImp.ModelBlocks.GEOM)geometryResource.ChunkEntries[0].RCOLBlock;
            var shaders = new List<string>();
            foreach (var shader in Enum.GetValues(typeof(Shader)))
            {
                shaders.Add(string.Format("{0} ({1})", shader, (uint)shader));
            }
            shaders.Sort();
            var shaderComboBoxAlignment = new Alignment(0, .5f, 1, 0);
            var shaderComboBox = new ComboBox(shaders.ToArray())
                {
                    Active = shaders.IndexOf(string.Format("{0} ({1})", (Shader)geom.Shader, (uint)geom.Shader))
                };
            shaderComboBoxAlignment.Add(shaderComboBox);
            shaderComboBox.Changed += (sender, e) =>
                {
                    geom.Shader = (ShaderType)Enum.Parse(typeof(Shader), shaderComboBox.ActiveText.Split(' ')[0]);
                    mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                };
            table.Attach(new Label("Shader")
                {
                    Xalign = 0
                }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.Attach(shaderComboBoxAlignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
            table.NRows++;
            foreach (var element in new List<ShaderData>(geom.Mtnf.SData))
            {
                Widget valueWidget = null;
                var alignment = new Alignment(0, .5f, 0, 0);
                var elementFloat = element as ElementFloat;
                if (elementFloat != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementFloat.Data, -1, 1, .0001, 10, 0), 0, 4);
                    spinButton.ValueChanged += (sender, e) =>
                        {
                            elementFloat.Data = (float)spinButton.Value;
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    valueWidget = spinButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementFloat2 = element as ElementFloat2;
                if (elementFloat2 != null)
                {
                    var hBox = new HBox();
                    var spinButtons = new SpinButton[]
                        {
                            new SpinButton(new Adjustment(elementFloat2.Data0, -1, 1, .0001, 10, 0), 0, 4),
                            new SpinButton(new Adjustment(elementFloat2.Data1, -1, 1, .0001, 10, 0), 0, 4)
                        };
                    spinButtons[0].ValueChanged += (sender, e) =>
                        {
                            elementFloat2.Data0 = (float)spinButtons[0].Value;
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    spinButtons[1].ValueChanged += (sender, e) =>
                        {
                            elementFloat2.Data1 = (float)spinButtons[1].Value;
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
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
                            var color = new OpenTK.Vector3(elementFloat3.Data0, elementFloat3.Data1, elementFloat3.Data2);
                            var material = mainWindow.Materials[geometryResourceKey];
                            switch (element.Field)
                            {
#pragma warning disable 0618
                                case FieldType.Ambient:
#pragma warning restore 0618
                                    material.AmbientColor = color;
                                    break;
                                case FieldType.Diffuse:
                                    material.DiffuseColor = color;
                                    break;
                                case FieldType.Specular:
                                    material.SpecularColor = color;
                                    break;
                            };
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
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
                            var color = new OpenTK.Vector3(elementFloat4.Data0, elementFloat4.Data1, elementFloat4.Data2);
                            var material = mainWindow.Materials[geometryResourceKey];
                            switch (element.Field)
                            {
#pragma warning disable 0618
                                case FieldType.Ambient:
#pragma warning restore 0618
                                    material.AmbientColor = color;
                                    break;
                                case FieldType.Diffuse:
                                    material.DiffuseColor = color;
                                    break;
                                case FieldType.Specular:
                                    material.SpecularColor = color;
                                    break;
                            };
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    valueWidget = colorButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementInt = element as ElementInt;
                if (elementInt != null)
                {
                    var spinButton = new SpinButton(new Adjustment(elementInt.Data, int.MinValue, int.MaxValue, 1, 10, 0), 0, 0);
                    spinButton.ValueChanged += (sender, e) =>
                        {
                            elementInt.Data = spinButton.ValueAsInt;
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    valueWidget = spinButton;
                    goto AttachLabelAndValueWidget;
                }
                var elementTextureRef = element as ElementTextureRef;
                if (elementTextureRef != null)
                {
                    alignment.Xscale = 1;
                    var comboBox = ImageResourceComboBox.CreateInstance(package, element.ParentTGIBlocks[elementTextureRef.Index].ReverseEvaluateResourceKey(), imageWidget);
                    var comboBoxLastActive = comboBox.Active;
                    comboBox.Changed += (sender, e) =>
                        {
                            if (comboBox.Active == comboBox.EntryCount - 1 || comboBox.Active == comboBoxLastActive)
                            {
                                return;
                            }
                            comboBoxLastActive = comboBox.Active;
                            var key = comboBox[comboBox.Active].Label;
                            var index = element.ParentTGIBlocks.FindIndex(x => x.ReverseEvaluateResourceKey() == key);
                            if (index == -1)
                            {
                                element.ParentTGIBlocks.Add(new TGIBlock(0, null, package.EvaluateImageResourceKey(key).ResourceIndexEntry));
                                index = element.ParentTGIBlocks.Count - 1;
                            }
                            elementTextureRef.Index = index;
                            var material = mainWindow.Materials[geometryResourceKey];
                            switch (element.Field)
                            {
                                case FieldType.AmbientOcclusionMap:
                                    material.AmbientMap = key;
                                    break;
                                case FieldType.DiffuseMap:
                                    material.DiffuseMap = key;
                                    break;
                                case FieldType.NormalMap:
                                    material.NormalMap = key;
                                    break;
                                case FieldType.SpecularMap:
                                    material.SpecularMap = key;
                                    break;
                            };
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    valueWidget = comboBox;
                }
                AttachLabelAndValueWidget:
                var deleteButton = new Button(new Gtk.Image(Stock.Delete, IconSize.Menu))
                    {
                        Relief = ReliefStyle.None,
                    };
                deleteButton.Clicked += (sender, e) =>
                    {
                        geom.Mtnf.SData.Remove(element);
                        foreach (var child in table.Children)
                        {
                            table.Remove(child);
                        }
                        table.AddProperties(package, geometryResource, scrolledWindow, imageWidget);
                        table.ShowAll();
                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                    };
                var labelHBox = new HBox(false, 6);
                labelHBox.PackStart(new Label(element.Field.ToString())
                    {
                        UseUnderline = false,
                        Xalign = 0
                    }, true, true, 0);
                labelHBox.PackEnd(deleteButton, false, true, 0);
                table.Attach(labelHBox, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                alignment.Add(valueWidget);
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
            var addPropertyButtonHBox = new HBox(false, 4);
            addPropertyButtonHBox.PackStart(new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                {
                    Xalign = 1
                }, true, true, 0);
            addPropertyButtonHBox.PackStart(new Label("Add Property")
                {
                    Xalign = 0
                }, true, true, 0);
            var addPropertyButton = new Button(addPropertyButtonHBox);
            addPropertyButton.Clicked += (sender, e) =>
                {
                    var addGEOMPropertyDialog = new AddGEOMPropertyDialog(mainWindow);
                    if (addGEOMPropertyDialog.Run() == (int)ResponseType.Ok)
                    {
                        foreach (var child in table.Children)
                        {
                            table.Remove(child);
                        }
                        var element = addGEOMPropertyDialog.DataType == typeof(ElementTextureRef) ? new ElementTextureRef(0, null, null, "GEOM") : (ShaderData)Activator.CreateInstance(addGEOMPropertyDialog.DataType, 0, null);
                        element.Field = addGEOMPropertyDialog.Field;
                        geom.Mtnf.SData.Add(element);
                        table.AddProperties(package, geometryResource, scrolledWindow, imageWidget);
                        table.ShowAll();
                        scrolledWindow.Vadjustment.Value = scrolledWindow.Vadjustment.Upper;
                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                    }
                    addGEOMPropertyDialog.Destroy();
                };
            table.Attach(addPropertyButton, 0, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
            table.NRows++;
        }

        public static Frame GetEnumPropertyCheckButtonsInNewFrame(string label, System.Action additionalToggleAction, object propertyHolder, params string[] propertyPathComponents)
        {
            var property = propertyHolder;
            var propertyInfo = property.GetType().GetProperty(propertyPathComponents[0]);
            for (var i = 1; i < propertyPathComponents.Length; i++)
            {
                property = propertyInfo.GetValue(property, null);
                propertyInfo = property.GetType().GetProperty(propertyPathComponents[i]);
            }
            var enumInstance = propertyInfo.GetValue(property, null);
            bool disableToggled = false,
            isFlagType = enumInstance.GetType().IsDefined(typeof(FlagsAttribute), false);
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
                            Active = ((Enum)enumInstance).HasFlag((Enum)value),
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
                        if (isFlagType)
                        {
                            switch (enumInstance.GetType().GetEnumUnderlyingType().Name)
                            {
                                case "Byte":
                                    propertyInfo.SetValue(property, (byte)((byte)enumInstance ^ (byte)value), null);
                                    break;
                                case "Char":
                                    propertyInfo.SetValue(property, (char)((char)enumInstance ^ (char)value), null);
                                    break;
                                case "Int16":
                                    propertyInfo.SetValue(property, (short)((short)enumInstance ^ (short)value), null);
                                    break;
                                case "Int32":
                                    propertyInfo.SetValue(property, (int)enumInstance ^ (int)value, null);
                                    break;
                                case "Int64":
                                    propertyInfo.SetValue(property, (long)enumInstance ^ (long)value, null);
                                    break;
                                case "SByte":
                                    propertyInfo.SetValue(property, (sbyte)((sbyte)enumInstance ^ (sbyte)value), null);
                                    break;
                                case "UInt16":
                                    propertyInfo.SetValue(property, (ushort)((ushort)enumInstance ^ (ushort)value), null);
                                    break;
                                case "UInt32":
                                    propertyInfo.SetValue(property, (uint)enumInstance ^ (uint)value, null);
                                    break;
                                case "UInt64":
                                    propertyInfo.SetValue(property, (ulong)enumInstance ^ (ulong)value, null);
                                    break;
                            }
                        }
                        else
                        {
                            propertyInfo.SetValue(property, value, null);
                        }
                        enumInstance = propertyInfo.GetValue(property, null);
                        additionalToggleAction();
                    };
                vBox.PackStart(checkButton, false, false, 0);
            }
            return frame;
        }

        public static Frame GetEnumPropertyCheckButtonsInNewFrame(string label, object propertyHolder, params string[] propertyPathComponents)
        {
            return GetEnumPropertyCheckButtonsInNewFrame(label, () =>
                {
                }, propertyHolder, propertyPathComponents);
        }

        public static void RescaleAndReposition(this Gtk.Window self, Gtk.Window parent)
        {
            self.SetSizeRequest(self.WidthRequest == -1 ? -1 : (int)(self.WidthRequest * Scale), self.HeightRequest == -1 ? -1 : (int)(self.HeightRequest * Scale));
            int parentHeight, parentWidth, parentX, parentY;
            parent.GetPosition(out parentX, out parentY);
            parent.GetSize(out parentWidth, out parentHeight);
            self.Move(parentX + (parentWidth >> 1) - (self.WidthRequest >> 1), parentY + (parentHeight >> 1) - (self.HeightRequest >> 1));
        }
    }
}
