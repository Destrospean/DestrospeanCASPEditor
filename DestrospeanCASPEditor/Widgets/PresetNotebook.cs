using System.Collections.Generic;
using Gdk;
using Gtk;

namespace Destrospean.DestrospeanCASPEditor.Widgets
{
    public class PresetNotebook : Notebook
    {
        bool mDisableSwitchPage = false;

        readonly bool mIsSubNotebook;

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

        PresetNotebook(CASPart casPart, Gtk.Image imageWidget, bool isSubNotebook = false) : base()
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
                        CASPart.Presets.Add(new CASPart.Preset(CASPart, CASPart.Presets[LastSelectedPage].XmlFile));
                        ((PresetNotebook)CurrentPageWidget).AddPreset(CASPart.Presets[CASPart.Presets.Count - 1]);
                        SetTabLabel(GetNthPage(0), GetPageLabelHBox(-CASPart.Presets.Count));
                        SetTabLabel(CurrentPageWidget, GetPageLabelHBox(-1));
                        AppendPage(new PresetNotebook(CASPart, Image, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                            {
                                Xalign = Platform.IsWindows ? 1 : .5f
                            });
                        ShowAll();
                    }
                    LastSelectedPage = CurrentPage;
                };
        }

        void AddPropertiesToTable(CASPart.IComplate complate, Table table)
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
                        HeightRequest = WidgetUtils.DefaultTableCellHeight
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
                        var rgba = new List<string>(value.Split(',')).ConvertAll(new System.Converter<string, ushort>(x => (ushort)(float.Parse(x) * ushort.MaxValue)));
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
                        var entries = WidgetUtils.BuildImageResourceComboBoxEntries(complate.ParentPackage, value, out comboBox, Image);
                        comboBox.Changed += (sender, e) => complate.SetValue(name, entries[comboBox.Active].Item2);
                        valueWidget = comboBox;
                        break;
                    case "vec2":
                        var hBox = new HBox();
                        var coordinates = new List<string>(value.Split(',')).ConvertAll(new System.Converter<string, float>(float.Parse));
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

        HBox GetPageLabelHBox(int pageIndexOffset = 0)
        {
            var pageIndex = NPages + pageIndexOffset;
            var deleteButton = new Button
                {
                    Relief = ReliefStyle.None,
                };
            var hBox = new HBox(false, 0);
            deleteButton.Add(new Gtk.Image(Stock.Delete, IconSize.Menu));
            deleteButton.Clicked += (sender, e) =>
                {
                    mDisableSwitchPage = true;
                    CASPart.Presets.RemoveAt(pageIndex);
                    while (NPages > 0)
                    {
                        RemovePage(0);
                    }
                    CASPart.Presets.ForEach(AddPreset);
                    CurrentPage = LastSelectedPage < NPages ? LastSelectedPage : NPages - 1;
                    AppendPage(new PresetNotebook(CASPart, Image, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                        {
                            Xalign = Platform.IsWindows ? 1 : .5f
                        });
                    ShowAll();
                    LastSelectedPage = CurrentPage;
                    mDisableSwitchPage = false;
                };
            hBox.PackStart(new Label
                {
                    Text = "Preset " + (pageIndex + 1).ToString()
                }, true, true, 0);
            if (CASPart.Presets.Count > 1)
            {
                hBox.PackEnd(deleteButton, false, true, 0);
            }
            hBox.ShowAll();
            return hBox;
        }

        public void AddPreset(CASPart.Preset preset)
        {
            var subNotebook = mIsSubNotebook ? this : new PresetNotebook(CASPart, Image, true);
            if (!mIsSubNotebook)
            {
                AppendPage(subNotebook, GetPageLabelHBox());
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
                        ColumnSpacing = WidgetUtils.DefaultTableColumnSpacing
                    };
                scrolledWindow.AddWithViewport(table);
                subNotebook.AppendPage(scrolledWindow, new Label
                    {
                        Text = pattern == null ? "Configuration" : pattern.Name
                    });
                AddPropertiesToTable(complate, table);
            }
            ShowAll();
        }

        public static PresetNotebook CreateInstance(CASPart casPart, Gtk.Image imageWidget)
        {
            var notebook = new PresetNotebook(casPart, imageWidget);
            casPart.Presets.ForEach(notebook.AddPreset);
            notebook.AppendPage(new PresetNotebook(casPart, imageWidget, true), new Gtk.Image(Stock.Add, IconSize.SmallToolbar)
                {
                    Xalign = Platform.IsWindows ? 1 : .5f
                });
            notebook.ShowAll();
            return notebook;
        }
    }
}
