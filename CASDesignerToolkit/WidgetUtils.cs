using System;
using System.Collections.Generic;
using Destrospean.CmarNYCBorrowed;
using Destrospean.Common;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Destrospean.S3PIExtensions;
using Gdk;
using Gtk;
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

        public static void AddProperties(this Notebook notebook, IPackage package, GEOM geometryResource, Gtk.Image imageWidget, int pageIndexOffset = 0)
        {
            try
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
            catch (Exception ex)
            {
                Program.WriteError(ex);
                throw;
            }
        }

        public static void AddProperties(this Table table, IPackage package, GEOM geometryResource, ScrolledWindow scrolledWindow, Gtk.Image imageWidget)
        {
            try
            {
                var mainWindow = MainWindow.Singleton;
                var geometryResourceKey = "";
                foreach (var geometryResourceKvp in PreloadedData.GEOMs)
                {
                    if (geometryResourceKvp.Value == geometryResource)
                    {
                        geometryResourceKey = geometryResourceKvp.Key;
                        break;
                    }
                }
                var shaders = new List<string>();
                foreach (var shader in Enum.GetValues(typeof(Shader)))
                {
                    shaders.Add(string.Format("{0} ({1})", shader, (uint)shader));
                }
                shaders.Sort();
                var shaderComboBoxAlignment = new Alignment(0, .5f, 1, 0);
                var shaderComboBox = new ComboBox(shaders.ToArray())
                    {
                        Active = shaders.IndexOf(string.Format("{0} ({1})", (Shader)geometryResource.ShaderHash, (uint)geometryResource.ShaderHash))
                    };
                shaderComboBoxAlignment.Add(shaderComboBox);
                shaderComboBox.Changed += (sender, e) =>
                    {
                        geometryResource.SetShader((uint)Enum.Parse(typeof(Shader), shaderComboBox.ActiveText.Split(' ')[0]));
                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                    };
                table.Attach(new Label("Shader")
                    {
                        Xalign = 0
                    }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                table.Attach(shaderComboBoxAlignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
                var fieldIndex = -1;
                foreach (var field in geometryResource.Shader.GetFields())
                {
                    Widget valueWidget = null;
                    var alignment = new Alignment(0, .5f, 0, 0);
                    int valueType;
                    geometryResource.Shader.GetFieldValue(field, out valueType);
                    var element = geometryResource.Shader.Data[++fieldIndex];
                    var elementIndex = fieldIndex;
                    if (valueType == 1)
                    {
                        switch (element.Length)
                        {
                            case 1:
                                var spinButton = new SpinButton(new Adjustment((float)element[0], -1, 1, .0001, 10, 0), 0, 4);
                                spinButton.ValueChanged += (sender, e) =>
                                    {
                                        element[0] = (float)spinButton.Value;
                                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                                    };
                                valueWidget = spinButton;
                                break;
                            case 2:
                                var hBox = new HBox();
                                var spinButtons = new List<SpinButton>
                                    {
                                        new SpinButton(new Adjustment((float)element[0], -1, 1, .0001, 10, 0), 0, 4),
                                        new SpinButton(new Adjustment((float)element[1], -1, 1, .0001, 10, 0), 0, 4)
                                    };
                                spinButtons[0].ValueChanged += (sender, e) =>
                                    {
                                        element[0] = (float)spinButtons[0].Value;
                                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                                    };
                                spinButtons[1].ValueChanged += (sender, e) =>
                                    {
                                        element[1] = (float)spinButtons[1].Value;
                                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                                    };
                                spinButtons.ForEach(x => hBox.PackStart(x, false, false, 0));
                                valueWidget = hBox;
                                break;
                            case 3:
                                var colorButton = new ColorButton
                                    {
                                        Color = new Color
                                            {
                                                Blue = (ushort)((float)element[2] * ushort.MaxValue),
                                                Green = (ushort)((float)element[1] * ushort.MaxValue),
                                                Red = (ushort)((float)element[0] * ushort.MaxValue)
                                            }
                                    };
                                colorButton.ColorSet += (sender, e) =>
                                    {
                                        element[0] = (float)colorButton.Color.Red / ushort.MaxValue;
                                        element[1] = (float)colorButton.Color.Green / ushort.MaxValue;
                                        element[2] = (float)colorButton.Color.Blue / ushort.MaxValue;
                                        var color = new OpenTK.Vector3((float)element[0], (float)element[1], (float)element[2]);
                                        var material = PreloadedData.Materials[geometryResourceKey];
                                        switch ((FieldType)field)
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
                                break;
                            case 4:
                                var colorButtonWithAlpha = new ColorButton
                                    {
                                        Alpha = (ushort)((float)element[3] * ushort.MaxValue),
                                        Color = new Color
                                            {
                                                Blue = (ushort)((float)element[2] * ushort.MaxValue),
                                                Green = (ushort)((float)element[1] * ushort.MaxValue),
                                                Red = (ushort)((float)element[0] * ushort.MaxValue)
                                            },
                                        UseAlpha = true
                                    };
                                colorButtonWithAlpha.ColorSet += (sender, e) =>
                                    {
                                        element[0] = (float)colorButtonWithAlpha.Color.Red / ushort.MaxValue;
                                        element[1] = (float)colorButtonWithAlpha.Color.Green / ushort.MaxValue;
                                        element[2] = (float)colorButtonWithAlpha.Color.Blue / ushort.MaxValue;
                                        element[3] = (float)colorButtonWithAlpha.Alpha / ushort.MaxValue;
                                        var color = new OpenTK.Vector3((float)element[0], (float)element[1], (float)element[2]);
                                        var material = PreloadedData.Materials[geometryResourceKey];
                                        switch ((FieldType)field)
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
                                        }
                                        ;
                                        mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                                    };
                                valueWidget = colorButtonWithAlpha;
                                break;
                        }
                    }
                    else if (valueType == 2)
                    {
                        var spinButton = new SpinButton(new Adjustment((int)element[0], int.MinValue, int.MaxValue, 1, 10, 0), 0, 0);
                        spinButton.ValueChanged += (sender, e) =>
                            {
                                element[0] = spinButton.ValueAsInt;
                                mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                            };
                        valueWidget = spinButton;
                    }
                    else if (valueType == 4)
                    {
                        alignment.Xscale = 1;
                        var comboBox = ImageResourceComboBox.CreateInstance(package, new ResourceKey(geometryResource.TGIList[(uint)element[0]].Type, geometryResource.TGIList[(uint)element[0]].Group, geometryResource.TGIList[(uint)element[0]].Instance).ReverseEvaluateResourceKey(), imageWidget);
                        var comboBoxLastActive = comboBox.Active;
                        comboBox.Changed += (sender, e) =>
                            {
                                if (comboBox.Active == comboBox.EntryCount - 1 || comboBox.Active == comboBoxLastActive)
                                {
                                    return;
                                }
                                comboBoxLastActive = comboBox.Active;
                                var key = comboBox[comboBox.Active].Label;
                                var index = Array.FindIndex(geometryResource.TGIList, x => new ResourceKey(x.Type, x.Group, x.Instance).ReverseEvaluateResourceKey() == key);
                                if (index == -1)
                                {
                                    var temp = new List<TGI>(geometryResource.TGIList);
                                    var resourceIndexEntry = package.EvaluateImageResourceKey(key).ResourceIndexEntry;
                                    temp.Add(new TGI(resourceIndexEntry.ResourceType, resourceIndexEntry.ResourceGroup, resourceIndexEntry.Instance));
                                    geometryResource.TGIList = temp.ToArray();
                                    index = geometryResource.TGIList.Length - 1;
                                }
                                element[0] = (uint)index;
                                var material = PreloadedData.Materials[geometryResourceKey];
                                switch ((FieldType)field)
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
                    var deleteButton = new Button(new Gtk.Image(Stock.Delete, IconSize.Menu))
                        {
                            Relief = ReliefStyle.None,
                        };
                    deleteButton.Clicked += (sender, e) =>
                        {
                            var elementList = new List<object[]>(geometryResource.Shader.Data);
                            var fieldList = new List<uint[]>(geometryResource.Shader.Fields);
                            elementList.RemoveAt(elementIndex);
                            fieldList.RemoveAt(elementIndex);
                            geometryResource.Shader.Data = elementList.ToArray();
                            geometryResource.Shader.Fields = fieldList.ToArray();
                            geometryResource.Shader.FieldCount--;
                            foreach (var child in table.Children)
                            {
                                table.Remove(child);
                            }
                            table.AddProperties(package, geometryResource, scrolledWindow, imageWidget);
                            table.ShowAll();
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        };
                    var labelHBox = new HBox(false, 6);
                    labelHBox.PackStart(new Label(((FieldType)field).ToString())
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
                        var addMaterialPropertyDialog = new AddMaterialPropertyDialog(mainWindow);
                        if (addMaterialPropertyDialog.Run() == (int)ResponseType.Ok)
                        {
                            foreach (var child in table.Children)
                            {
                                table.Remove(child);
                            }
                            var elementList = new List<object[]>(geometryResource.Shader.Data);
                            var fieldList = new List<uint[]>(geometryResource.Shader.Fields);
                            elementList.Add(new object[addMaterialPropertyDialog.ValueCount]);
                            for (var i = 0; i < addMaterialPropertyDialog.ValueCount; i++)
                            {
                                switch (addMaterialPropertyDialog.DataType)
                                {
                                    case 1:
                                        elementList[elementList.Count - 1][i] = 0f;
                                        break;
                                    case 2:
                                        elementList[elementList.Count - 1][i] = 0;
                                        break;
                                    default:
                                        elementList[elementList.Count - 1][i] = 0u;
                                        break;
                                }
                            }
                            fieldList.Add(new uint[]
                                {
                                    addMaterialPropertyDialog.Field,
                                    addMaterialPropertyDialog.DataType,
                                    addMaterialPropertyDialog.ValueCount
                                });
                            geometryResource.Shader.Data = elementList.ToArray();
                            geometryResource.Shader.Fields = fieldList.ToArray();
                            geometryResource.Shader.FieldCount++;
                            table.AddProperties(package, geometryResource, scrolledWindow, imageWidget);
                            table.ShowAll();
                            scrolledWindow.Vadjustment.Value = scrolledWindow.Vadjustment.Upper;
                            mainWindow.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                        }
                        addMaterialPropertyDialog.Destroy();
                    };
                table.Attach(addPropertyButton, 0, 2, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
            catch (Exception ex)
            {
                Program.WriteError(ex);
                throw;
            }
        }

        public static Frame GetEnumPropertyCheckButtonsInNewFrame(string label, CmarNYCBorrowed.Action additionalToggleAction, object propertyHolder, params string[] propertyPathComponents)
        {
            try
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
            catch (Exception ex)
            {
                Program.WriteError(ex);
                throw;
            }
        }

        public static Frame GetEnumPropertyCheckButtonsInNewFrame(string label, object propertyHolder, params string[] propertyPathComponents)
        {
            return GetEnumPropertyCheckButtonsInNewFrame(label, () =>
                {
                }, propertyHolder, propertyPathComponents);
        }

        public static void RescaleAndReposition(this Gtk.Window self, Gtk.Window parent)
        {
            try
            {
                self.SetSizeRequest(self.WidthRequest == -1 ? -1 : (int)(self.WidthRequest * Scale), self.HeightRequest == -1 ? -1 : (int)(self.HeightRequest * Scale));
                int parentHeight, parentWidth, parentX, parentY;
                parent.GetPosition(out parentX, out parentY);
                parent.GetSize(out parentWidth, out parentHeight);
                self.Move(parentX + (parentWidth >> 1) - (self.WidthRequest >> 1), parentY + (parentHeight >> 1) - (self.HeightRequest >> 1));
            }
            catch (Exception ex)
            {
                Program.WriteError(ex);
                throw;
            }
        }
    }
}
