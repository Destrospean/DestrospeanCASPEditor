using System;
using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Gtk;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

public partial class MainWindow : Window
{
    public IPackage CurrentPackage;

    public readonly Dictionary<IResourceIndexEntry, CASPart> CASParts = new Dictionary<IResourceIndexEntry, CASPart>();

    public readonly Dictionary<IResourceIndexEntry, GeometryResource> GeometryResources = new Dictionary<IResourceIndexEntry, GeometryResource>();

    public readonly Dictionary<IResourceIndexEntry, GenericRCOLResource> VPXYResources = new Dictionary<IResourceIndexEntry, GenericRCOLResource>();

    public readonly ListStore ResourceListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(IResourceIndexEntry));

    public MainWindow() : base(WindowType.Toplevel)
    {
        Build();
        RescaleAndReposition();
        BuildResourceTable();
        GameFoldersDialog.LoadGameFolders();
        ResourcePropertyNotebook.RemovePage(0);
    }

    public void AddCASPartWidgets(CASPart casPart)
    {
        var flagNotebook = new Notebook
            {
                ShowTabs = false
            };
        var flagPageButtonHBox = new HBox(false, 0);
        var flagPageVBox = new VBox(false, 0);
        var flagTables = new Table[2];
        for (var i = 0; i < flagTables.Length; i++)
        {
            flagTables[i] = new Table(2, 3, true);
            flagNotebook.AppendPage(flagTables[i], new Label());
        }
        flagPageVBox.PackStart(flagPageButtonHBox, false, false, 0);
        flagPageVBox.PackStart(flagNotebook, true, true, 0);
        Button nextButton = new Button(), prevButton = new Button();
        flagNotebook.SwitchPage += (o, args) =>
            {
                nextButton.Sensitive = flagNotebook.CurrentPage < flagNotebook.NPages - 1;
                prevButton.Sensitive = flagNotebook.CurrentPage > 0;
            };
        nextButton.Add(new Arrow(ArrowType.Right, ShadowType.None)
            {
                Xalign = .5f
            });
        prevButton.Add(new Arrow(ArrowType.Left, ShadowType.None)
            {
                Xalign = .5f
            });
        nextButton.Clicked += (sender, e) => flagNotebook.NextPage();
        prevButton.Clicked += (sender, e) => flagNotebook.PrevPage();
        flagPageButtonHBox.PackStart(prevButton, false, true, 4);
        flagPageButtonHBox.PackStart(nextButton, false, true, 4);
        flagTables[0].Attach(WidgetUtils.GetFlagsInNewFrame("Clothing Category", casPart.CASPartResource, "ClothingCategory"), 0, 1, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetFlagsInNewFrame("Clothing Type", casPart.CASPartResource, "Clothing"), 1, 2, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetFlagsInNewFrame("Data Type", casPart.CASPartResource, "DataType"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetFlagsInNewFrame("Age", casPart.CASPartResource.AgeGender, "Age"), 0, 1, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetFlagsInNewFrame("Gender", casPart.CASPartResource.AgeGender, "Gender"), 1, 2, 0, 1);
        flagTables[1].Attach(WidgetUtils.GetFlagsInNewFrame("Species", casPart.CASPartResource.AgeGender, "Species"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetFlagsInNewFrame("Handedness", casPart.CASPartResource.AgeGender, "Handedness"), 1, 2, 1, 2);
        flagTables[0].ShowAll();
        flagTables[1].ShowAll();
        ResourcePropertyTable.Attach(flagPageVBox, 0, 1, 0, 1);
        ResourcePropertyTable.Attach(PresetNotebook.CreateInstance(casPart, Image), 1, 2, 0, 1);
        ResourcePropertyTable.ShowAll();
    }

    public void BuildResourceTable()
    {
        CellRendererText groupCell = new CellRendererText(), instanceCell = new CellRendererText(), tagCell = new CellRendererText(), typeCell = new CellRendererText();
        TreeViewColumn groupColumn = new TreeViewColumn
            {
                Title = "Group"
            }, instanceColumn = new TreeViewColumn
            {
                Title = "Instance"
            }, tagColumn = new TreeViewColumn
            {
                Title = "Tag"
            }, typeColumn = new TreeViewColumn
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
                            Image.Pixbuf = ImageUtils.PreloadedImages[resourceIndexEntry][0];
                            break;
                        case "CASP":
                            AddCASPartWidgets(CASParts[resourceIndexEntry]);
                            break;
                        case "GEOM":
                            WidgetUtils.AddPropertiesToNotebook(CurrentPackage, GeometryResources[resourceIndexEntry], ResourcePropertyNotebook, Image, this);
                            break;
                        case "TXTC":
                            break;
                        case "VPXY":
                            /*
                            var vpxy = (VPXY)VPXYResources[resourceIndexEntry].ChunkEntries[0].RCOLBlock;
                            foreach (var entry in vpxy.Entries)
                            {
                                var entry00 = entry as VPXY.Entry00;
                                if (entry00 != null)
                                {
                                    Console.WriteLine(entry00.EntryID);
                                    foreach (var tgiIndex in entry00.TGIIndexes)
                                    {
                                        Console.WriteLine(ResourceUtils.ReverseEvaluateResourceKey(entry00.ParentTGIBlocks[tgiIndex]));
                                    }
                                }
                                var entry01 = entry as VPXY.Entry01;
                                if (entry01 != null)
                                {
                                }
                            }
                            */
                            break;
                    }
                }
            };
    }

    public void ClearTemporaryData()
    {
        CASParts.Clear();
        GeometryResources.Clear();
        VPXYResources.Clear();
        ImageUtils.PreloadedGameImages.Clear();
        ImageUtils.PreloadedImages.Clear();
    }

    public void RefreshWidgets()
    {
        ClearTemporaryData();
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
        foreach (var resourceIndexEntry in resourceList)
        {
            var tag = ResourceUtils.GetResourceTypeTag(resourceIndexEntry);
            switch (tag)
            {
                case "_IMG":
                case "CASP":
                case "GEOM":
                case "TXTC":
                case "VPXY":
                    if (!resourceIndexEntry.IsDeleted)
                    {
                        ResourceListStore.AppendValues(tag, "0x" + resourceIndexEntry.ResourceType.ToString("X8"), "0x" + resourceIndexEntry.ResourceGroup.ToString("X8"), "0x" + resourceIndexEntry.Instance.ToString("X16"), resourceIndexEntry);
                    }
                    break;
            }
            switch (tag)
            {
                case "_IMG":
                    ImageUtils.PreloadImage(CurrentPackage, resourceIndexEntry, Image);
                    ImageUtils.PreloadedImages[resourceIndexEntry].Add(ImageUtils.PreloadedImages[resourceIndexEntry][0].ScaleSimple(WidgetUtils.SmallImageHeight, WidgetUtils.SmallImageHeight, Gdk.InterpType.Bilinear));
                    break;
                case "CASP":
                    CASParts.Add(resourceIndexEntry, new CASPart(CurrentPackage, resourceIndexEntry));
                    break;
                case "GEOM":
                    GeometryResources.Add(resourceIndexEntry, (GeometryResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry));
                    break;
                case "TXTC":
                    break;
                case "VPXY":
                    VPXYResources.Add(resourceIndexEntry, (GenericRCOLResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry));
                    break;
            }
        }
        foreach (var casPart in CASParts.Values)
        {
            AddCASPartWidgets(casPart);
        }
        foreach (var geometryResource in GeometryResources.Values)
        {
            WidgetUtils.AddPropertiesToNotebook(CurrentPackage, geometryResource, ResourcePropertyNotebook, Image, this);
        }
        foreach (var vpxyResource in VPXYResources.Values)
        {
        }
        ResourceTreeView.Selection.SelectPath(new TreePath("0"));
        ShowAll();
    }

    public void RescaleAndReposition()
    {
        var monitorGeometry = Screen.GetMonitorGeometry(Screen.GetMonitorAtWindow(GdkWindow));
        var scaleEnvironmentVariable = Environment.GetEnvironmentVariable("CASP_EDITOR_SCALE");
        WidgetUtils.Scale = string.IsNullOrEmpty(scaleEnvironmentVariable) ? Platform.IsUnix ? (float)monitorGeometry.Height / 1080 : 1 : float.Parse(scaleEnvironmentVariable);
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
        Move(((int)((float)monitorGeometry.Width / WidgetUtils.WineScaleDenominator) - WidthRequest) / 2, ((int)((float)monitorGeometry.Height / WidgetUtils.WineScaleDenominator) - HeightRequest) / 2);
        AllowShrink = Platform.IsRunningUnderWine;
    }

    protected void OnCloseActionActivated(object sender, EventArgs e)
    {
        s3pi.Package.Package.ClosePackage(0, CurrentPackage);
        CurrentPackage = null;
        ResourceUtils.MissingResourceKeys.Clear();
        RefreshWidgets();
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
        ResourceUtils.MissingResourceKeys.Add(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry));
        RefreshWidgets();
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
                ResourceUtils.ResolveResourceType(CurrentPackage, ResourceUtils.AddResource(CurrentPackage, fileChooserDialog.Filename));
                RefreshWidgets();
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
                IResourceIndexEntry resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4), tempResourceIndexEntry = ResourceUtils.AddResource(CurrentPackage, fileChooserDialog.Filename, resourceIndexEntry, false);
                ResourceUtils.ResolveResourceType(CurrentPackage, tempResourceIndexEntry);
                CurrentPackage.ReplaceResource(resourceIndexEntry, WrapperDealer.GetResource(0, CurrentPackage, tempResourceIndexEntry));
                CurrentPackage.DeleteResource(tempResourceIndexEntry);
                RefreshWidgets();
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooserDialog.Destroy();
    }

    [GLib.ConnectBeforeAttribute]
    protected void OnResourceTreeViewButtonPress(object o, ButtonPressEventArgs args)
    {
        TreeViewColumn column;
        TreeIter iter;
        TreePath path;
        int x, y;
        ResourceTreeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path, out column, out x, out y);
        ResourceListStore.GetIter(out iter, path);
        switch (args.Event.Button)
        {
            case 1:
                ResourceTreeView.Selection.SelectIter(iter);
                break;
            case 3:
                var resourceIndexEntry = (IResourceIndexEntry)ResourceListStore.GetValue(iter, 4);
                Console.WriteLine(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry));
                break;
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
    }

    protected void OnSaveAsActionActivated(object sender, EventArgs e)
    {
    }

    protected void OnSizeAllocated(object sender, SizeAllocatedArgs a)
    {
        if (Platform.IsRunningUnderWine && (a.Allocation.Height < DefaultHeight - 1 || a.Allocation.Width < DefaultWidth))
        {
            int x, y;
            GetPosition(out x, out y);
            ReshowWithInitialSize();
            Move(x, y);
            Resize(a.Allocation.Width < DefaultWidth ? DefaultWidth : a.Allocation.Width, a.Allocation.Height < DefaultHeight - 1 ? DefaultHeight : a.Allocation.Height);
        }
    }
}
