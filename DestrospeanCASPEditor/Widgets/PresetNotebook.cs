using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Globalization;

namespace Destrospean.DestrospeanCASPEditor.Widgets
{
    public class PresetNotebook : Notebook
    {
        protected bool mDisableSwitchPage = false;

        protected readonly bool mIsSubNotebook;

        public CASPart CASPart
        {
            get;
            private set;
        }

        public Gtk.Image Image
        {
            get;
            private set;
        }

        public int LastSelectedPage
        {
            get;
            private set;
        }

        protected PresetNotebook(CASPart casPart, Gtk.Image imageWidget, bool isSubNotebook = false) : base()
        {
            CASPart = casPart;
            Image = imageWidget;
            mIsSubNotebook = isSubNotebook;
            LastSelectedPage = 0;
            SwitchPage += (o, args) =>
                {
                    if (mDisableSwitchPage || mIsSubNotebook)
                    {
                        return;
                    }
                    if (NPages > 1 && CurrentPage == NPages - 1)
                    {
                        CASPart.Presets.Add(new CASPart.Preset(CASPart, CASPart.AllPresets[LastSelectedPage].XmlFile));
                        ((PresetNotebook)CurrentPageWidget).AddPreset(CASPart.AllPresets[CASPart.AllPresets.Count - 1]);
                        SetTabLabel(GetNthPage(0), GetPageLabelHBox(-CASPart.AllPresets.Count, CASPart.DefaultPreset != null));
                        SetTabLabel(CurrentPageWidget, GetPageLabelHBox(CASPart.Presets.Count - CASPart.AllPresets.Count - 1));
                        AppendPage(new PresetNotebook(CASPart, Image, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                            {
                                Xalign = Platform.IsWindows ? 1 : .5f
                            });
                        ShowAll();
                        MainWindow.Singleton.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                    }
                    LastSelectedPage = CurrentPage;
                };
        }

        protected void AddPropertiesToTable(Table table, CASPart.Complate complate)
        {
            foreach (var propertyName in complate.PropertyNames)
            {
                string type;
                if (!complate.PropertiesTyped.TryGetValue(propertyName, out type))
                {
                    continue;
                }
                Widget valueWidget = null;
                var alignment = new Alignment(0, .5f, 1, 0);
                var value = complate[propertyName];
                switch (type)
                {
                    case "bool":
                        var checkButton = new CheckButton
                            {
                                Active = bool.Parse(value),
                                UseUnderline = false
                            };
                        checkButton.Toggled += (sender, e) => complate[propertyName] = checkButton.Active.ToString();
                        valueWidget = checkButton;
                        break;
                    case "color":
                        alignment.Xscale = 0;
                        var rgba = System.Array.ConvertAll(value.Split(','), x => (ushort)(float.Parse(x, CultureInfo.InvariantCulture) * ushort.MaxValue));
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
                        colorButton.ColorSet += (sender, e) => complate[propertyName] = string.Join(",", System.Array.ConvertAll(new ushort[]
                            {
                                colorButton.Color.Red,
                                colorButton.Color.Green,
                                colorButton.Color.Blue,
                                colorButton.Alpha
                            }, x => (float)x / ushort.MaxValue));
                        valueWidget = colorButton;
                        break;
                    case "float":
                        alignment.Xscale = 0;
                        var spinButton = new SpinButton(new Adjustment(float.Parse(value, CultureInfo.InvariantCulture), -1, 1, .0001, 10, 0), 0, 4);
                        spinButton.ValueChanged += (sender, e) => complate[propertyName] = spinButton.Value.ToString("F4");
                        valueWidget = spinButton;
                        break;
                    case "pattern":
                        var button = new Button(new Label(value)
                            {
                                UseUnderline = false,
                                Xalign = 0
                            });
                        button.Clicked += (sender, e) =>
                            {
                                var choosePatternDialog = new ChoosePatternDialog(MainWindow.Singleton, complate.CASPart.ParentPackage);
                                if (choosePatternDialog.Run() == (int)ResponseType.Ok)
                                {
                                    ((CASPart.Preset)complate).ReplacePattern(propertyName, choosePatternDialog.ResourceKey);
                                    complate[propertyName] = choosePatternDialog.PatternPath;
                                    for (var i = 0; i < (mIsSubNotebook ? this : (PresetNotebook)CurrentPageWidget).NPages; i++)
                                    {
                                        var patternTable = (Table)((Viewport)((ScrolledWindow)(mIsSubNotebook ? this : (PresetNotebook)CurrentPageWidget).GetNthPage(i)).Child).Child;
                                        foreach (var child in patternTable.Children)
                                        {
                                            patternTable.Remove(child);
                                        }
                                        patternTable.NRows = 1;
                                        AddPropertiesToTable(patternTable, i == 0 ? complate : ((CASPart.Preset)complate).Patterns[i - 1]);
                                    }
                                }
                                choosePatternDialog.Destroy();
                            };
                        valueWidget = button;
                        break;
                    case "string":
                        var entry = new Entry
                            {
                                Sensitive = false,
                                Text = value
                            };
                        entry.Changed += (sender, e) => complate[propertyName] = entry.Text;
                        valueWidget = entry;
                        break;
                    case "texture":
                        var comboBox = ImageResourceComboBox.CreateInstance(complate.ParentPackage, value, Image);
                        var comboBoxLastActive = comboBox.Active;
                        comboBox.Changed += (sender, e) =>
                            {
                                if (comboBox.Active < comboBox.EntryCount - 1 && comboBoxLastActive != comboBox.Active)
                                {
                                    comboBoxLastActive = comboBox.Active;
                                    complate[propertyName] = comboBox[comboBox.Active].Label;
                                }
                            };
                        valueWidget = comboBox;
                        break;
                    case "vec2":
                        var hBox = new HBox();
                        var coordinates = System.Array.ConvertAll(value.Split(','), x => float.Parse(x, CultureInfo.InvariantCulture));
                        var spinButtons = new List<SpinButton>
                            {
                                new SpinButton(new Adjustment(coordinates[0], -1, 1, .0001, 10, 0), 0, 4),
                                new SpinButton(new Adjustment(coordinates[1], -1, 1, .0001, 10, 0), 0, 4)
                            };
                        spinButtons.ForEach(x =>
                            {
                                x.ValueChanged += (sender, e) => complate[propertyName] = spinButtons[0].Value.ToString("F4") + "," + spinButtons[1].Value.ToString("F4");
                                hBox.PackStart(x, false, false, 0);
                            });
                        valueWidget = hBox;
                        break;
                }
                table.Attach(new Label(propertyName)
                    {
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, table.NRows - 1, table.NRows, AttachOptions.Fill, 0, 0, 0);
                alignment.Add(valueWidget);
                table.Attach(alignment, 1, 2, table.NRows - 1, table.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                table.NRows++;
            }
            table.SizeAllocated += (o, args) =>
                {
                    var maxHeight = 0;
                    foreach (var child in table.Children)
                    {
                        maxHeight = System.Math.Max(child.Allocation.Height, maxHeight);
                    }
                    foreach (var child in table.Children)
                    {
                        child.HeightRequest = maxHeight;
                    }
                };
            table.ShowAll();
        }

        protected HBox GetPageLabelHBox(int pageIndexOffset = 0, bool isDefault = false)
        {
            var pageIndex = NPages + pageIndexOffset;
            var deleteButton = new Button
                {
                    Relief = ReliefStyle.None,
                };
            deleteButton.Add(new Gtk.Image(Stock.Delete, IconSize.Menu));
            deleteButton.Clicked += (sender, e) =>
                {
                    mDisableSwitchPage = true;
                    CASPart.Presets.RemoveAt(pageIndex);
                    while (NPages > 0)
                    {
                        RemovePage(0);
                    }
                    if (CASPart.DefaultPreset != null)
                    {
                        AddPreset(CASPart.DefaultPreset, true);
                    }
                    CASPart.Presets.ForEach(x => AddPreset(x));
                    CurrentPage = LastSelectedPage < NPages ? LastSelectedPage : NPages - 1;
                    AppendPage(new PresetNotebook(CASPart, Image, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                        {
                            Xalign = Platform.IsWindows ? 1 : .5f
                        });
                    ShowAll();
                    LastSelectedPage = CurrentPage;
                    mDisableSwitchPage = false;
                    MainWindow.Singleton.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
                };
            var hBox = new HBox(false, 0);
            hBox.PackStart(new Label(isDefault ? "Default" : "Preset " + pageIndex.ToString()), true, true, 0);
            if (CASPart.DefaultPreset == null ? CASPart.Presets.Count > 1 : !isDefault)
            {
                hBox.PackEnd(deleteButton, false, true, 0);
            }
            hBox.ShowAll();
            return hBox;
        }

        public void AddPreset(CASPart.Preset preset, bool isDefault = false)
        {
            var subNotebook = mIsSubNotebook ? this : new PresetNotebook(CASPart, Image, true);
            if (!mIsSubNotebook)
            {
                AppendPage(subNotebook, GetPageLabelHBox(CASPart.Presets.Count - CASPart.AllPresets.Count, isDefault));
            }
            var complates = new List<CASPart.Complate>
                {
                    preset
                };
            complates.AddRange(preset.Patterns);
            foreach (var complate in complates)
            {
                var scrolledWindow = new ScrolledWindow();
                var table = new Table(1, 2, false)
                    {
                        ColumnSpacing = WidgetUtils.DefaultTableColumnSpacing
                    };
                scrolledWindow.AddWithViewport(table);
                var pattern = complate as CASPart.Pattern;
                subNotebook.AppendPage(scrolledWindow, new Label(pattern == null ? "Configuration" : pattern.SlotName));
                AddPropertiesToTable(table, complate);
            }
            ShowAll();
        }

        public static PresetNotebook CreateInstance(CASPart casPart, Gtk.Image imageWidget)
        {
            var notebook = new PresetNotebook(casPart, imageWidget);
            if (casPart.DefaultPreset != null)
            {
                notebook.AddPreset(casPart.DefaultPreset, true);
            }
            casPart.Presets.ForEach(x => notebook.AddPreset(x));
            notebook.AppendPage(new PresetNotebook(casPart, imageWidget, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                {
                    Xalign = Platform.IsWindows ? 1 : .5f
                });
            notebook.ShowAll();
            return notebook;
        }
    }
}
