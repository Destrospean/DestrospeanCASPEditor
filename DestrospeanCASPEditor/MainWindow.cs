using System;
using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Gtk;
using meshExpImp.ModelBlocks;
using OpenTK.Graphics.OpenGL;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

[Flags]
public enum NextStateOptions : byte
{
    NoUnsavedChanges,
    UnsavedChanges,
    UpdateModels,
    UnsavedChangesAndUpdateModels
}

public partial class MainWindow : Window
{
    PresetNotebook mPresetNotebook;

    public IPackage CurrentPackage;

    public readonly Dictionary<IResourceIndexEntry, CASPart> CASParts = new Dictionary<IResourceIndexEntry, CASPart>();

    public readonly Dictionary<IResourceIndexEntry, GeometryResource> GeometryResources = new Dictionary<IResourceIndexEntry, GeometryResource>();

    public readonly Dictionary<IResourceIndexEntry, GenericRCOLResource> VPXYResources = new Dictionary<IResourceIndexEntry, GenericRCOLResource>();

    public readonly ListStore ResourceListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(IResourceIndexEntry));

    public readonly List<SwitchPageHandler> ResourcePropertyNotebookSwitchPageHandlers = new List<SwitchPageHandler>();

    public bool HasUnsavedChanges
    {
        get;
        private set;
    }

    public NextStateOptions NextState
    {
        set
        {
            if (value.HasFlag(NextStateOptions.UpdateModels))
            {
                TreeIter iter;
                TreeModel model;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter) && (string)model.GetValue(iter, 0) == "CASP")
                {
                    LoadGEOMs(CASParts[(IResourceIndexEntry)model.GetValue(iter, 4)]);
                }
            }
            if (value.HasFlag(NextStateOptions.UnsavedChanges))
            {
                Title += HasUnsavedChanges ? "" : " *";
                HasUnsavedChanges = true;
            }
            else if (value == NextStateOptions.NoUnsavedChanges)
            {
                Title = Title.Substring(0, HasUnsavedChanges && Title.EndsWith(" *") ? Title.Length - 2 : Title.Length);
                HasUnsavedChanges = false;
            }
        }
    }

    public static MainWindow Singleton
    {
        get;
        private set;
    }

    public MainWindow() : base(WindowType.Toplevel)
    {
        HasUnsavedChanges = false;
        Singleton = this;
        Build();
        RescaleAndReposition();
        BuildResourceTable();
        ApplicationSpecificSettings.LoadSettings();
        ResourcePropertyNotebook.RemovePage(0);
        MainTable.Attach(new Gtk.Image
            {
                HeightRequest = Image.HeightRequest,
                Pixbuf = ImageUtils.CreateCheckerboard(Image.HeightRequest, (int)(8 * WidgetUtils.Scale), new Gdk.Color(191, 191, 191), new Gdk.Color(127, 127, 127)),
                WidthRequest = Image.WidthRequest,
                Xalign = 0,
                Yalign = 0
            }, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        PrepareGLWidget();
        MainTable.Attach(mGLWidget, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        MainTable.ShowAll();
        mGLWidget.Hide();
    }

    void AddCASPartWidgets(CASPart casPart)
    {
        var flagNotebook = new Notebook
            {
                ShowTabs = false
            };
        HBox buttonHBox = new HBox(false, 0), 
        flagPageButtonHBox = new HBox(false, 0)
            {
                WidthRequest = Image.WidthRequest
            };
        var flagPageVBox = new VBox(false, 0);
        var flagTables = new Table[2];
        for (var i = 0; i < flagTables.Length; i++)
        {
            flagTables[i] = new Table(2, 3, true);
            flagNotebook.AppendPage(flagTables[i], new Label());
        }
        flagPageVBox.PackStart(buttonHBox, false, false, 0);
        flagPageVBox.PackStart(flagNotebook, true, true, 0);
        Button nextButton = new Button(new Arrow(ArrowType.Right, ShadowType.None)
            {
                Xalign = .5f
            }),
        prevButton = new Button(new Arrow(ArrowType.Left, ShadowType.None)
            {
                Xalign = .5f,
            }),
        resetViewButton = new Button("Reset View");
        nextButton.Clicked += (sender, e) => flagNotebook.NextPage();
        prevButton.Clicked += (sender, e) => flagNotebook.PrevPage();
        resetViewButton.Clicked += (sender, e) =>
            {
                mCamera.Orientation = new OpenTK.Vector3((float)Math.PI, 0, 0);
                mCamera.Position = new OpenTK.Vector3(0, 1, 4);
                mCurrentRotation = OpenTK.Vector3.Zero;
                mFOV = OpenTK.MathHelper.DegreesToRadians(30);
            };
        flagNotebook.SwitchPage += (o, args) =>
            {
                nextButton.Sensitive = flagNotebook.CurrentPage < flagNotebook.NPages - 1;
                prevButton.Sensitive = flagNotebook.CurrentPage > 0;
            };
        Alignment nextButtonAlignment = new Alignment(.5f, .5f, 0, 0),
        prevButtonAlignment = new Alignment(.5f, .5f, 0, 0);
        nextButtonAlignment.Add(nextButton);
        prevButtonAlignment.Add(prevButton);
        flagPageButtonHBox.PackStart(prevButtonAlignment, false, true, 4);
        flagPageButtonHBox.PackStart(nextButtonAlignment, false, true, 4);
        flagPageButtonHBox.PackEnd(resetViewButton, false, true, 0);
        buttonHBox.PackStart(flagPageButtonHBox, false, true, 0);
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Clothing Category", casPart.CASPartResource, "ClothingCategory"), 0, 1, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Clothing Type", casPart.CASPartResource, "Clothing"), 1, 2, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Data Type", casPart.CASPartResource, "DataType"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Age", casPart.CASPartResource.AgeGender, "Age"), 0, 1, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Gender", casPart.CASPartResource.AgeGender, "Gender"), 1, 2, 0, 1);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Species", casPart.CASPartResource.AgeGender, "Species"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Handedness", casPart.CASPartResource.AgeGender, "Handedness"), 1, 2, 1, 2);
        flagTables[0].ShowAll();
        flagTables[1].ShowAll();
        ResourcePropertyTable.Attach(flagPageVBox, 0, 1, 0, 1);
        mPresetNotebook = PresetNotebook.CreateInstance(casPart, Image);
        mPresetNotebook.SwitchPage += (o, args) => LoadGEOMs(casPart);
        ResourcePropertyTable.Attach(mPresetNotebook, 1, 2, 0, 1);
        ResourcePropertyTable.ShowAll();
        BuildLODNotebook(casPart);
    }

    void BuildLODNotebook(CASPart casPart, int startLODPageIndex = 0, int startGEOMPageIndex = 0)
    {
        foreach (var switchPageHandler in ResourcePropertyNotebookSwitchPageHandlers)
        {
            ResourcePropertyNotebook.SwitchPage -= switchPageHandler;
        }
        ResourcePropertyNotebookSwitchPageHandlers.Clear();
        ResourcePropertyNotebookSwitchPageHandlers.Insert(0, (o, args) => LoadGEOMs(casPart));
        ResourcePropertyNotebook.SwitchPage += ResourcePropertyNotebookSwitchPageHandlers[0];
        foreach (var lodKvp in casPart.LODs)
        {
            var geomNotebook = new Notebook
                {
                    ShowTabs = false
                };
            var actionGroup = new ActionGroup("Default");
            Gtk.Action exportGEOMAction = new Gtk.Action("ExportGEOMAction", "Export GEOM", null, Stock.Directory),
            exportOBJAction = new Gtk.Action("ExportOBJAction", "Export OBJ", null, Stock.Directory),
            exportWSOAction = new Gtk.Action("ExportWSOAction", "Export WSO", null, Stock.Directory),
            importGEOMAction = new Gtk.Action("ImportGEOMAction", "Import GEOM", null, Stock.Directory),
            importOBJAction = new Gtk.Action("ImportOBJAction", "Import OBJ", null, Stock.Directory),
            importWSOAction = new Gtk.Action("ImportWSOAction", "Import WSO", null, Stock.Directory);
            actionGroup.Add(new Gtk.Action("ExportAction", "Export"));
            actionGroup.Add(new Gtk.Action("ImportAction", "Import"));
            actionGroup.Add(exportGEOMAction);
            actionGroup.Add(exportOBJAction);
            actionGroup.Add(exportWSOAction);
            actionGroup.Add(importGEOMAction);
            actionGroup.Add(importOBJAction);
            actionGroup.Add(importWSOAction);
            var uiManager = new UIManager();
            uiManager.InsertActionGroup(actionGroup, 0);
            uiManager.AddUiFromString(@"
                <ui>
                    <menubar name='GEOMPropertiesMenuBar'>
                        <menu name='ExportAction' action='ExportAction'>
                            <menuitem name='ExportGEOMAction' action='ExportGEOMAction'/>
                            <menuitem name='ExportOBJAction' action='ExportOBJAction'/>
                            <menuitem name='ExportWSOAction' action='ExportWSOAction'/>
                        </menu>
                        <menu name='ImportAction' action='ImportAction'>
                            <menuitem name='ImportGEOMAction' action='ImportGEOMAction'/>
                            <menuitem name='ImportOBJAction' action='ImportOBJAction'/>
                            <menuitem name='ImportWSOAction' action='ImportWSOAction'/>
                        </menu>
                    </menubar>
                </ui>");
            var menuBar = (MenuBar)uiManager.GetWidget("/GEOMPropertiesMenuBar");
            menuBar.PackDirection = PackDirection.Rtl;
            Button nextButton = new Button(new Arrow(ArrowType.Right, ShadowType.None)
                {
                    Xalign = .5f
                }),
            prevButton = new Button(new Arrow(ArrowType.Left, ShadowType.None)
                {
                    Xalign = .5f
                });
            nextButton.Clicked += (sender, e) => geomNotebook.NextPage();
            prevButton.Clicked += (sender, e) => geomNotebook.PrevPage();
            Alignment nextButtonAlignment = new Alignment(.5f, .5f, 0, 0),
            prevButtonAlignment = new Alignment(.5f, .5f, 0, 0);
            nextButtonAlignment.Add(nextButton);
            prevButtonAlignment.Add(prevButton);
            geomNotebook.SwitchPage += (o, args) =>
                {
                    nextButton.Sensitive = geomNotebook.CurrentPage < geomNotebook.NPages - 1;
                    prevButton.Sensitive = geomNotebook.CurrentPage > 0;
                };
            importGEOMAction.Activated += (sender, e) =>
                {
                    var fileChooserDialog = new FileChooserDialog("Import GEOM", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
                    var fileFilter = new FileFilter
                        {
                            Name = "The Sims 3 GEOM Resources"
                        };
                    fileFilter.AddPattern("*.simgeom");
                    fileChooserDialog.AddFilter(fileFilter);
                    if (fileChooserDialog.Run() == (int)ResponseType.Accept)
                    {
                        try
                        {
                            foreach (var geometryResourceKvp in GeometryResources)
                            {
                                int selectedGEOMIndex = geomNotebook.CurrentPage,
                                selectedLODIndex = ResourcePropertyNotebook.CurrentPage;
                                if (geometryResourceKvp.Value == lodKvp.Value[selectedGEOMIndex])
                                {
                                    IResourceIndexEntry resourceIndexEntry = geometryResourceKvp.Key,
                                    tempResourceIndexEntry = CurrentPackage.AddResource(fileChooserDialog.Filename, resourceIndexEntry, false);
                                    CurrentPackage.ResolveResourceType(tempResourceIndexEntry);
                                    var resource = WrapperDealer.GetResource(0, CurrentPackage, tempResourceIndexEntry);
                                    CurrentPackage.ReplaceResource(resourceIndexEntry, resource);
                                    CurrentPackage.DeleteResource(tempResourceIndexEntry);
                                    GeometryResources[resourceIndexEntry] = (GeometryResource)resource;
                                    casPart.LoadLODs(GeometryResources, VPXYResources);
                                    foreach (var child in ResourcePropertyNotebook.Children)
                                    {
                                        ResourcePropertyNotebook.Remove(child);
                                    }
                                    BuildLODNotebook(casPart, selectedLODIndex, selectedGEOMIndex);
                                    NextState = NextStateOptions.UnsavedChanges;
                                    break;
                                }
                            }
                        }
                        catch (System.IO.InvalidDataException ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    fileChooserDialog.Destroy();
                };
            var geomPageButtonHBox = new HBox(false, 0);
            geomPageButtonHBox.PackEnd(menuBar, true, true, 4);
            geomPageButtonHBox.PackStart(prevButtonAlignment, false, true, 4);
            geomPageButtonHBox.PackStart(nextButtonAlignment, false, true, 4);
            geomPageButtonHBox.ShowAll();
            var lodPageVBox = new VBox(false, 0);
            lodPageVBox.PackStart(geomPageButtonHBox, false, true, 0);
            lodPageVBox.PackStart(geomNotebook, true, true, 0);
            lodPageVBox.ShowAll();
            ResourcePropertyNotebook.AppendPage(lodPageVBox, new Label("LOD " + lodKvp.Key.ToString()));
            lodKvp.Value.ForEach(x => geomNotebook.AddProperties(CurrentPackage, x, Image));
            if (lodKvp.Value == new List<List<GeometryResource>>(casPart.LODs.Values)[startLODPageIndex])
            {
                ResourcePropertyNotebook.CurrentPage = startLODPageIndex;
                geomNotebook.CurrentPage = startGEOMPageIndex;
            }
        }
    }

    void BuildResourceTable()
    {
        CellRendererText groupCell = new CellRendererText(),
        instanceCell = new CellRendererText(),
        tagCell = new CellRendererText(),
        typeCell = new CellRendererText();
        TreeViewColumn groupColumn = new TreeViewColumn
            {
                Title = "Group"
            },
        instanceColumn = new TreeViewColumn
            {
                Title = "Instance"
            },
        tagColumn = new TreeViewColumn
            {
                Title = "Tag"
            },
        typeColumn = new TreeViewColumn
            {
                Title = "Type"
            };
        tagColumn.PackStart(tagCell, true);
        tagColumn.AddAttribute(tagCell, "text", 0);
        typeColumn.PackStart(typeCell, true);
        typeColumn.AddAttribute(typeCell, "text", 1);
        groupColumn.PackStart(groupCell, true);
        groupColumn.AddAttribute(groupCell, "text", 2);
        instanceColumn.PackStart(instanceCell, true);
        instanceColumn.AddAttribute(instanceCell, "text", 3);
        ResourceTreeView.AppendColumn(tagColumn);
        ResourceTreeView.AppendColumn(typeColumn);
        ResourceTreeView.AppendColumn(groupColumn);
        ResourceTreeView.AppendColumn(instanceColumn);
        ResourceTreeView.Model = ResourceListStore;
        ResourceTreeView.ButtonPressEvent += OnResourceTreeViewButtonPress;
        ResourceTreeView.Selection.Changed += (sender, e) => 
            {
                mObjects.Clear();
                mGLWidget.Hide();
                Image.Clear();
                foreach (var child in ResourcePropertyTable.Children)
                {
                    ResourcePropertyTable.Remove(child);
                }
                while (ResourcePropertyNotebook.NPages > 0)
                {
                    ResourcePropertyNotebook.RemovePage(0);
                }
                TreeIter iter;
                TreeModel model;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter))
                {
                    var resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4);
                    switch ((string)model.GetValue(iter, 0))
                    {
                        case "_IMG":
                            Image.Pixbuf = ImageUtils.PreloadedImagePixbufs[resourceIndexEntry][0];
                            break;
                        case "CASP":
                            mGLWidget.Show();
                            AddCASPartWidgets(CASParts[resourceIndexEntry]);
                            break;
                    }
                }
            };
    }

    void RescaleAndReposition()
    {
        var monitorGeometry = Screen.GetMonitorGeometry(Screen.GetMonitorAtWindow(GdkWindow));
        var scaleEnvironmentVariable = Environment.GetEnvironmentVariable("CASP_EDITOR_SCALE");
        WidgetUtils.Scale = string.IsNullOrEmpty(scaleEnvironmentVariable) ? Platform.IsUnix ? monitorGeometry.Height / 1080f : 1 : float.Parse(scaleEnvironmentVariable);
        WidgetUtils.WineScaleDenominator = Platform.IsRunningUnderWine ? (float)Screen.Resolution / 96 : 1;
        SetDefaultSize((int)(DefaultWidth * WidgetUtils.Scale), (int)(DefaultHeight * WidgetUtils.Scale));
        foreach (var widget in new Widget[]
            {
                Image,
                MainTable,
                ResourcePropertyNotebook,
                ResourcePropertyTable,
                this
            })
        {
            widget.SetSizeRequest(widget.WidthRequest == -1 ? -1 : (int)(widget.WidthRequest * WidgetUtils.Scale), widget.HeightRequest == -1 ? -1 : (int)(widget.HeightRequest * WidgetUtils.Scale));
        }
        Resize(DefaultWidth, DefaultHeight);
        Move(((int)(monitorGeometry.Width / WidgetUtils.WineScaleDenominator) - WidthRequest) >> 1, ((int)(monitorGeometry.Height / WidgetUtils.WineScaleDenominator) - HeightRequest) >> 1);
        Resizable = !Platform.IsRunningUnderWine;
    }

    public void ClearTemporaryData()
    {
        mObjects.Clear();
        CASParts.Clear();
        GeometryResources.Clear();
        Materials.Clear();
        DeleteTextures();
        VPXYResources.Clear();
        ImageUtils.PreloadedGameImagePixbufs.Clear();
        ImageUtils.PreloadedGameImages.Clear();
        ImageUtils.PreloadedImagePixbufs.Clear();
        ImageUtils.PreloadedImages.Clear();
    }

    public void RefreshWidgets(bool clearTemporaryData = true)
    {
        if (clearTemporaryData)
        {
            ClearTemporaryData();
        }
        Image.Clear();
        ResourceListStore.Clear();
        foreach (var child in ResourcePropertyTable.Children)
        {
            ResourcePropertyTable.Remove(child);
        }
        while (ResourcePropertyNotebook.NPages > 0)
        {
            ResourcePropertyNotebook.RemovePage(0);
        }
        foreach (var action in new Gtk.Action[]
            {
                CloseAction,
                ResourceAction,
                SaveAction,
                SaveAsAction
            })
        {
            action.Sensitive = CurrentPackage != null;
        }
        if (!CloseAction.Sensitive)
        {
            return;
        }
        var resourceList = CurrentPackage.GetResourceList;
        resourceList.Sort((a, b) => ResourceUtils.GetResourceTypeTag(a).CompareTo(ResourceUtils.GetResourceTypeTag(b)));
        foreach (var resourceIndexEntry in resourceList.FindAll(x => !x.IsDeleted))
        {
            var tag = ResourceUtils.GetResourceTypeTag(resourceIndexEntry);
            switch (tag)
            {
                case "_IMG":
                case "CASP":
                    ResourceListStore.AppendValues(tag, "0x" + resourceIndexEntry.ResourceType.ToString("X8"), "0x" + resourceIndexEntry.ResourceGroup.ToString("X8"), "0x" + resourceIndexEntry.Instance.ToString("X16"), resourceIndexEntry);
                    break;
            }
            var missingResourceKeyIndex = ResourceUtils.MissingResourceKeys.IndexOf(resourceIndexEntry.ReverseEvaluateResourceKey());
            switch (tag)
            {
                case "_IMG":
                    if (!ImageUtils.PreloadedImagePixbufs.ContainsKey(resourceIndexEntry) || missingResourceKeyIndex > -1)
                    {
                        CurrentPackage.PreloadImage(resourceIndexEntry, Image);
                        ImageUtils.PreloadedImagePixbufs[resourceIndexEntry].Add(ImageUtils.PreloadedImagePixbufs[resourceIndexEntry][0].ScaleSimple(WidgetUtils.SmallImageSize, WidgetUtils.SmallImageSize, Gdk.InterpType.Bilinear));
                    }
                    break;
                case "CASP":
                    if (!CASParts.ContainsKey(resourceIndexEntry) || missingResourceKeyIndex > -1)
                    {
                        CASParts[resourceIndexEntry] = new CASPart(CurrentPackage, resourceIndexEntry, GeometryResources, VPXYResources);
                    }
                    break;
                case "GEOM":
                    if (!GeometryResources.ContainsKey(resourceIndexEntry) || missingResourceKeyIndex > -1)
                    {
                        GeometryResources[resourceIndexEntry] = (GeometryResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry);
                    }
                    break;
                case "VPXY":
                    if (!VPXYResources.ContainsKey(resourceIndexEntry) || missingResourceKeyIndex > -1)
                    {
                        VPXYResources[resourceIndexEntry] = (GenericRCOLResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry);
                    }
                    break;
            }
            if (missingResourceKeyIndex > -1)
            {
                ResourceUtils.MissingResourceKeys.RemoveAt(missingResourceKeyIndex);
            }
        }
        foreach (var casPart in CASParts.Values)
        {
            AddCASPartWidgets(casPart);
        }
        ResourceTreeView.Selection.SelectPath(new TreePath("0"));
    }

    protected void OnCloseActionActivated(object sender, EventArgs e)
    {
        s3pi.Package.Package.ClosePackage(0, CurrentPackage);
        CurrentPackage = null;
        ResourceUtils.MissingResourceKeys.Clear();
        RefreshWidgets();
        NextState = NextStateOptions.NoUnsavedChanges;
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnDeleteResourceActionActivated(object sender, EventArgs e)
    {
        TreeIter iter;
        TreeModel model;
        ResourceTreeView.Selection.GetSelected(out model, out iter);
        var resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4);
        CurrentPackage.DeleteResource(resourceIndexEntry);
        ResourceUtils.MissingResourceKeys.Add(resourceIndexEntry.ReverseEvaluateResourceKey());
        RefreshWidgets(false);
        NextState = NextStateOptions.NoUnsavedChanges;
    }

    protected void OnGameFoldersActionActivated(object sender, EventArgs e)
    {
        new GameFoldersDialog(this).ShowAll();
    }

    protected void OnImportResourceActionActivated(object sender, EventArgs e)
    {
        var fileChooserDialog = new FileChooserDialog("Import Resource", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fileChooserDialog.Run() == (int)ResponseType.Accept)
        {
            try
            {
                CurrentPackage.ResolveResourceType(CurrentPackage.AddResource(fileChooserDialog.Filename));
                RefreshWidgets(false);
                NextState = NextStateOptions.UnsavedChanges;
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooserDialog.Destroy();
    }

    protected void OnNewActionActivated(object sender, EventArgs e)
    {
    }

    protected void OnOpenActionActivated(object sender, EventArgs e)
    {
        var fileChooserDialog = new FileChooserDialog("Open Package", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        var fileFilter = new FileFilter
            {
                Name = "The Sims 3 DBPF Packages"
            };
        fileFilter.AddPattern("*.package");
        fileChooserDialog.AddFilter(fileFilter);
        if (fileChooserDialog.Run() == (int)ResponseType.Accept)
        {
            try
            {
                var package = s3pi.Package.Package.OpenPackage(0, fileChooserDialog.Filename, true);
                s3pi.Package.Package.ClosePackage(0, CurrentPackage);
                CurrentPackage = package;
                ResourceUtils.MissingResourceKeys.Clear();
                RefreshWidgets();
                NextState = NextStateOptions.NoUnsavedChanges;
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooserDialog.Destroy();
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        Application.Quit();
    }

    protected void OnReplaceResourceActionActivated(object sender, EventArgs e)
    {
        var fileChooserDialog = new FileChooserDialog("Replace Resource", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fileChooserDialog.Run() == (int)ResponseType.Accept)
        {
            try
            {
                TreeIter iter;
                TreeModel model;
                ResourceTreeView.Selection.GetSelected(out model, out iter);
                IResourceIndexEntry resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4),
                tempResourceIndexEntry = CurrentPackage.AddResource(fileChooserDialog.Filename, resourceIndexEntry, false);
                CurrentPackage.ResolveResourceType(tempResourceIndexEntry);
                CurrentPackage.ReplaceResource(resourceIndexEntry, WrapperDealer.GetResource(0, CurrentPackage, tempResourceIndexEntry));
                CurrentPackage.DeleteResource(tempResourceIndexEntry);
                ResourceUtils.MissingResourceKeys.Add(resourceIndexEntry.ReverseEvaluateResourceKey());
                RefreshWidgets(false);
                NextState = NextStateOptions.UnsavedChanges;
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooserDialog.Destroy();
    }

    [GLib.ConnectBefore]
    protected void OnResourceTreeViewButtonPress(object o, ButtonPressEventArgs args)
    {
        TreeViewColumn column;
        TreeIter iter;
        TreePath path;
        int x, y;
        if (ResourceTreeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path, out column, out x, out y))
        {
            ResourceListStore.GetIter(out iter, path);
            switch (args.Event.Button)
            {
                case 1:
                    ResourceTreeView.Selection.SelectIter(iter);
                    break;
                case 3:
                    //var resourceIndexEntry = (IResourceIndexEntry)ResourceListStore.GetValue(iter, 4);
                    //Console.WriteLine(resourceIndexEntry.ReverseEvaluateResourceKey());
                    break;
            }
        }
        args.RetVal = true;
    }

    protected void OnSaveActionActivated(object sender, EventArgs e)
    {
        foreach (var casPartKvp in CASParts)
        {
            casPartKvp.Value.SavePresets();
            CurrentPackage.ReplaceResource(casPartKvp.Key, casPartKvp.Value.CASPartResource);
        }
        foreach (var geometryResourceKvp in GeometryResources)
        {
            CurrentPackage.ReplaceResource(geometryResourceKvp.Key, geometryResourceKvp.Value);
        }
        foreach (var vpxyResourceKvp in VPXYResources)
        {
            CurrentPackage.ReplaceResource(vpxyResourceKvp.Key, vpxyResourceKvp.Value);
        }
        CurrentPackage.SavePackage();
        RefreshWidgets(false);
        NextState = NextStateOptions.NoUnsavedChanges;
    }

    protected void OnSaveAsActionActivated(object sender, EventArgs e)
    {
    }
}
