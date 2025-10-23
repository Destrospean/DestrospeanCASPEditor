using System;
using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor;
using Gtk;
using s3pi.Interfaces;

public partial class MainWindow : Window
{
    public Dictionary<IResourceIndexEntry, CASPart> CASParts = new Dictionary<IResourceIndexEntry, CASPart>();

    public IPackage CurrentPackage;

    public Dictionary<IResourceIndexEntry, meshExpImp.ModelBlocks.GeometryResource> GeometryResources = new Dictionary<IResourceIndexEntry, meshExpImp.ModelBlocks.GeometryResource>();

    public ListStore ResourceListStore = new ListStore(typeof(string), typeof(string), typeof(IResourceIndexEntry));

    public MainWindow() : base(WindowType.Toplevel)
    {
        Build();
        Rescale();
        BuildResourceTable();
        LoadGameFolders();
        PresetNotebook.RemovePage(0);
    }

    public void AddCASPartWidgets(CASPart casPart)
    {
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.ClothingCategoryFlags), casPart.CASPartResource.ClothingCategory, "Clothing Category"), 0, 1, 0, 2);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.ClothingType), casPart.CASPartResource.Clothing, "Clothing"), 1, 2, 0, 2);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.DataTypeFlags), casPart.CASPartResource.DataType, "Data Type"), 2, 3, 0, 2);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.AgeFlags), casPart.CASPartResource.AgeGender.Age, "Age Gender", "Age"), 3, 4, 0, 2);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.GenderFlags), casPart.CASPartResource.AgeGender.Gender, "Age Gender", "Gender"), 4, 5, 0, 1);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.SpeciesType), casPart.CASPartResource.AgeGender.Species, "Age Gender", "Species"), 5, 6, 0, 2);
        CASPartFlagTable.Attach(ComponentUtils.GetFlagsInNewFrame(casPart, typeof(CASPartResource.HandednessFlags), casPart.CASPartResource.AgeGender.Handedness, "Age Gender", "Handedness"), 4, 5, 1, 2);
        CASPartFlagTable.ShowAll();
        casPart.Presets.ForEach(x => ComponentUtils.AddPresetToNotebook(x, PresetNotebook, Image));
    }

    public void BuildResourceTable()
    {
        CellRendererText instanceCell = new CellRendererText(), tagCell = new CellRendererText();
        TreeViewColumn instanceColumn = new TreeViewColumn
            {
                Title = "Instance"
            }, tagColumn = new TreeViewColumn
            {
                Title = "Type"
            };
        tagColumn.PackStart(tagCell, true);
        tagColumn.AddAttribute(tagCell, "text", 0);
        instanceColumn.PackStart(instanceCell, true);
        instanceColumn.AddAttribute(instanceCell, "text", 1);
        ResourceTreeView.AppendColumn(tagColumn);
        ResourceTreeView.AppendColumn(instanceColumn);
        ResourceTreeView.Model = ResourceListStore;
        ResourceTreeView.Selection.Changed += (sender, e) => 
            {
                Image.Clear();
                foreach (var child in CASPartFlagTable.Children)
                {
                    CASPartFlagTable.Remove(child);
                    child.Dispose();
                }
                while (PresetNotebook.NPages > 0)
                {
                    PresetNotebook.RemovePage(0);
                }
                TreeIter iter;
                TreeModel model;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter))
                {
                    var resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 2);
                    switch ((string)model.GetValue(iter, 0))
                    {
                        case "_IMG":
                            Image.Pixbuf = ImageUtils.PreloadedImages[resourceIndexEntry][0];
                            break;
                        case "CASP":
                            AddCASPartWidgets(CASParts[resourceIndexEntry]);
                            break;
                        case "GEOM":
                            ComponentUtils.AddPropertiesToNotebook(CurrentPackage, GeometryResources[resourceIndexEntry], PresetNotebook, Image);
                            break;
                        case "TXTC":
                            break;
                        case "VPXY":
                            break;
                    }
                }
            };
    }

    public void ClearTemporaryData()
    {
        CASParts.Clear();
        GeometryResources.Clear();
        ImageUtils.PreloadedGameImages.Clear();
        ImageUtils.PreloadedImages.Clear();
    }

    public void LoadGameFolders()
    {
        if (System.IO.File.Exists(GameFoldersDialog.ConfigurationPath))
        {
            var installDirectories = "";
            using (var stream = System.IO.File.OpenText(GameFoldersDialog.ConfigurationPath))
            {
                foreach (var installDirectoryKvp in Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(stream.ReadToEnd()))
                {
                    installDirectories += ";" + installDirectoryKvp.Key + "=" + installDirectoryKvp.Value;
                }
            }
            s3pi.Filetable.GameFolders.InstallDirs = installDirectories;
        }
    }

    public void RefreshWidgets()
    {
        ClearTemporaryData();
        Image.Clear();
        ResourceListStore.Clear();
        foreach (var child in CASPartFlagTable.Children)
        {
            CASPartFlagTable.Remove(child);
            child.Dispose();
        }
        while (PresetNotebook.NPages > 0)
        {
            PresetNotebook.RemovePage(0);
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
                        ResourceListStore.AppendValues(tag, "0x" + resourceIndexEntry.Instance.ToString("X"), resourceIndexEntry);
                    }
                    break;
            }
            switch (tag)
            {
                case "_IMG":
                    ImageUtils.PreloadImage(CurrentPackage, resourceIndexEntry, Image);
                    ImageUtils.PreloadedImages[resourceIndexEntry].Add(ImageUtils.PreloadedImages[resourceIndexEntry][0].ScaleSimple(32, 32, Gdk.InterpType.Bilinear));
                    break;
                case "CASP":
                    CASParts.Add(resourceIndexEntry, new CASPart(CurrentPackage, resourceIndexEntry));
                    break;
                case "GEOM":
                    GeometryResources.Add(resourceIndexEntry, (meshExpImp.ModelBlocks.GeometryResource)s3pi.WrapperDealer.WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry));
                    break;
            }
        }
        foreach (var casPart in CASParts.Values)
        {
            AddCASPartWidgets(casPart);
        }
        foreach (var geometryResource in GeometryResources.Values)
        {
            ComponentUtils.AddPropertiesToNotebook(CurrentPackage, geometryResource, PresetNotebook, Image);
        }
        ResourceTreeView.Selection.SelectPath(new TreePath("0"));
        ShowAll();
    }

    public void Rescale()
    {
        var monitorGeometry = Screen.GetMonitorGeometry(Screen.GetMonitorAtWindow(GdkWindow));
        var scaleEnvironmentVariable = Environment.GetEnvironmentVariable("CASP_EDITOR_SCALE");
        ComponentUtils.Scale = string.IsNullOrEmpty(scaleEnvironmentVariable) ? Platform.OS.HasFlag(Platform.OSFlags.Unix) ? (float)monitorGeometry.Height / 1080 : 1 : float.Parse(scaleEnvironmentVariable);
        ComponentUtils.WineScale = Platform.IsRunningUnderWine ? (float)Screen.Resolution / 96 : 1;
        SetDefaultSize((int)(DefaultWidth * ComponentUtils.Scale), (int)(DefaultHeight * ComponentUtils.Scale));
        foreach (var widget in new Widget[]
            {
                CASPartFlagTable,
                Image,
                MainTable,
                PresetNotebook,
                this
            })
        {
            widget.SetSizeRequest(widget.WidthRequest == -1 ? -1 : (int)(widget.WidthRequest * ComponentUtils.Scale), widget.HeightRequest == -1 ? -1 : (int)(widget.HeightRequest * ComponentUtils.Scale));
        }
        Resize(DefaultWidth, DefaultHeight);
        if (Platform.OS.HasFlag(Platform.OSFlags.Unix) || Platform.IsRunningUnderWine)
        {
            Move(((int)((float)monitorGeometry.Width / ComponentUtils.WineScale) - WidthRequest) / 2, ((int)((float)monitorGeometry.Height / ComponentUtils.WineScale) - HeightRequest) / 2);
        }
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
        if (ResourceTreeView.Selection.GetSelected(out model, out iter))
        {
            var resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 2);
            CurrentPackage.DeleteResource(resourceIndexEntry);
            ResourceUtils.MissingResourceKeys.Add(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry));
            RefreshWidgets();
        }
    }

    protected void OnGameFoldersActionActivated(object sender, EventArgs e)
    {
        new GameFoldersDialog(this).ShowAll();
    }

    protected void OnImportResourceActionActivated(object sender, EventArgs e)
    {
        FileChooserDialog fileChooser = new FileChooserDialog("Import Resource", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fileChooser.Run() == (int)ResponseType.Accept)
        {
            try
            {
                ResourceUtils.ResolveResourceType(CurrentPackage, ResourceUtils.AddResource(CurrentPackage, fileChooser.Filename));
                RefreshWidgets();
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooser.Destroy();
    }

    protected void OnNewActionActivated(object sender, EventArgs e)
    {
    }

    protected void OnOpenActionActivated(object sender, EventArgs e)
    {
        FileChooserDialog fileChooser = new FileChooserDialog("Open Package", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fileChooser.Run() == (int)ResponseType.Accept)
        {
            try
            {
                var package = s3pi.Package.Package.OpenPackage(0, fileChooser.Filename, true);
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
        fileChooser.Destroy();
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        Application.Quit();
    }

    protected void OnReplaceResourceActionActivated(object sender, EventArgs e)
    {
        FileChooserDialog fileChooser = new FileChooserDialog("Replace Resource", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fileChooser.Run() == (int)ResponseType.Accept)
        {
            try
            {
                TreeIter iter;
                TreeModel model;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter))
                {
                    IResourceIndexEntry addedResourceIndexEntry = ResourceUtils.AddResource(CurrentPackage, fileChooser.Filename), resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 2);
                    ResourceUtils.ResolveResourceType(CurrentPackage, addedResourceIndexEntry);
                    CurrentPackage.ReplaceResource(resourceIndexEntry, s3pi.WrapperDealer.WrapperDealer.GetResource(0, CurrentPackage, addedResourceIndexEntry));
                    CurrentPackage.DeleteResource(addedResourceIndexEntry);
                    RefreshWidgets();
                }
            }
            catch (System.IO.InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooser.Destroy();
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
