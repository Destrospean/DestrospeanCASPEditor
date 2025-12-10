using System;
using System.Collections.Generic;
using System.IO;
using Destrospean.CmarNYCBorrowed;
using Destrospean.DestrospeanCASPEditor;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Gtk;
using meshExpImp.ModelBlocks;
using OpenTK.Graphics.OpenGL;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;
using GEOM = Destrospean.CmarNYCBorrowed.GEOM;
using VPXY = Destrospean.CmarNYCBorrowed.VPXY;

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

    string mSaveAsPath;

    public IPackage CurrentPackage;

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
                    LoadGEOMs(PreloadedCASParts[((IResourceIndexEntry)model.GetValue(iter, 4)).ReverseEvaluateResourceKey()]);
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

    public readonly Dictionary<string, CASPart> PreloadedCASParts = new Dictionary<string, CASPart>();

    public readonly Dictionary<string, GeometryResource> PreloadedGeometryResources = new Dictionary<string, GeometryResource>();

    public readonly Dictionary<string, GenericRCOLResource> PreloadedVPXYResources = new Dictionary<string, GenericRCOLResource>();

    public readonly ListStore ResourceListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(IResourceIndexEntry));

    public readonly List<SwitchPageHandler> ResourcePropertyNotebookSwitchPageHandlers = new List<SwitchPageHandler>();

    public static MainWindow Singleton
    {
        get;
        private set;
    }

    enum MeshFileType
    {
        GEOM,
        OBJ,
        WSO
    }

    public MainWindow() : base(WindowType.Toplevel)
    {
        HasUnsavedChanges = false;
        Singleton = this;
        Build();
        RescaleAndReposition();
        BuildResourceTable();
        ApplicationSpecificSettings.LoadSettings();
        UseAdvancedShadersAction.Active = ApplicationSpecificSettings.UseAdvancedOpenGLShaders;
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
                Xalign = .5f
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
        System.Action additionalToggleAction = delegate
            {
                casPart.ClearCurrentRig();
                MainWindow.Singleton.NextState = NextStateOptions.UnsavedChangesAndUpdateModels;
            };
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Clothing Category", additionalToggleAction, casPart.CASPartResource, "ClothingCategory"), 0, 1, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Clothing Type", additionalToggleAction, casPart.CASPartResource, "Clothing"), 1, 2, 0, 2);
        flagTables[0].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Data Type", additionalToggleAction, casPart.CASPartResource, "DataType"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Age", additionalToggleAction, casPart.CASPartResource.AgeGender, "Age"), 0, 1, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Gender", additionalToggleAction, casPart.CASPartResource.AgeGender, "Gender"), 1, 2, 0, 1);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Species", additionalToggleAction, casPart.CASPartResource.AgeGender, "Species"), 2, 3, 0, 2);
        flagTables[1].Attach(WidgetUtils.GetEnumPropertyCheckButtonsInNewFrame("Handedness", additionalToggleAction, casPart.CASPartResource.AgeGender, "Handedness"), 1, 2, 1, 2);
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
            Gtk.Action exportGEOMAction = new Gtk.Action("ExportGEOMAction", "Export GEOM", null, Stock.SaveAs),
            exportOBJAction = new Gtk.Action("ExportOBJAction", "Export OBJ", null, Stock.SaveAs),
            exportWSOAction = new Gtk.Action("ExportWSOAction", "Export WSO", null, Stock.SaveAs),
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
            System.Action<MeshFileType> exportMesh = (MeshFileType meshFileType) =>
                {
                    switch (meshFileType)
                    {
                        case MeshFileType.GEOM:
                        case MeshFileType.OBJ:
                        case MeshFileType.WSO:
                            break;
                        default:
                            return;
                    }
                    var fileChooserDialog = new FileChooserDialog("Export " + meshFileType.ToString(), this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
                    var fileFilter = new FileFilter
                        {
                            Name = meshFileType == MeshFileType.GEOM ? "The Sims 3 GEOM Resource" : meshFileType == MeshFileType.OBJ ? "Wavefront OBJ" : meshFileType == MeshFileType.WSO ? "The Sims Resource Workshop Object" : null
                        };
                    fileFilter.AddPattern(meshFileType == MeshFileType.GEOM ? "*.simgeom" : meshFileType == MeshFileType.OBJ ? "*.obj" : meshFileType == MeshFileType.WSO ? "*.wso" : null);
                    fileChooserDialog.AddFilter(fileFilter);
                    var geom = lodKvp.Value[geomNotebook.CurrentPage].ToGEOM();
                    if (fileChooserDialog.Run() == (int)ResponseType.Accept)
                    {
                        byte[] bblnIndices =
                            {
                                casPart.CASPartResource.BlendInfoFatIndex,
                                casPart.CASPartResource.BlendInfoFitIndex,
                                casPart.CASPartResource.BlendInfoThinIndex,
                                casPart.CASPartResource.BlendInfoSpecialIndex
                            };
                        var morphs = new GEOM[bblnIndices.Length];
                        for (var i = 0; i < bblnIndices.Length; i++)
                        {
                            BBLN bbln;
                            ResourceUtils.EvaluatedResourceKey evaluated;
                            try
                            {
                                evaluated = casPart.ParentPackage.EvaluateResourceKey(casPart.CASPartResource.TGIBlocks[bblnIndices[i]].ReverseEvaluateResourceKey());
                                bbln = new BBLN(new BinaryReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream));
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                                morphs[i] = null;
                                continue;
                            }
                            BGEO bgeo = null;
                            try
                            {
                                evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.BGEOTGI).ReverseEvaluateResourceKey());
                                bgeo = ((CASPartResource.BlendGeometryResource)WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry)).ToBGEO();
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                            }
                            foreach (var entry in bbln.Entries)
                            {
                                foreach (var geomMorph in entry.GEOMMorphs)
                                {
                                    if (bgeo != null)
                                    {
                                        morphs[i] = new GEOM(geom, bgeo, bgeo.GetSection1EntryIndex(casPart.AdjustedSpecies, (AgeGender)(uint)casPart.CASPartResource.AgeGender.Age, (AgeGender)((uint)casPart.CASPartResource.AgeGender.Gender << 12)), lodKvp.Key);
                                    }
                                    else if (bbln.TGIList != null && bbln.TGIList.Length > geomMorph.TGIIndex && geom.HasVertexIDs)
                                    {
                                        try
                                        {
                                            evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.TGIList[geomMorph.TGIIndex]).ReverseEvaluateResourceKey());
                                            var vpxy = new VPXY(new BinaryReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream));
                                            foreach (var link in vpxy.MeshLinks(lodKvp.Key))
                                            {
                                                try
                                                {
                                                    evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(link).ReverseEvaluateResourceKey());
                                                    morphs[i] = ((GeometryResource)WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry)).ToGEOM();
                                                }
                                                catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                                                {
                                                    morphs[i] = null;
                                                }
                                            }
                                        }
                                        catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                                        {
                                            morphs[i] = null;
                                        }
                                    }
                                }
                            }
                        }
                        switch (meshFileType)
                        {
                            case MeshFileType.GEOM:
                                var filename = fileChooserDialog.Filename;
                                if (filename.ToLower().EndsWith(".simgeom"))
                                {
                                    filename.Remove(filename.LastIndexOf('.'));
                                }
                                using (var fileStream = File.Create(filename + ".simgeom"))
                                {
                                    geom.Write(new BinaryWriter(fileStream));
                                }
                                for (var i = 0; i < morphs.Length; i++)
                                {
                                    if (morphs[i] != null)
                                    {
                                        using (var fileStream = File.Create(filename + "_" + "fat fit thin special".Split(' ')[i] + ".simgeom"))
                                        {
                                            morphs[i].Write(new BinaryWriter(fileStream));
                                        }
                                    }
                                }
                                break;
                            case MeshFileType.OBJ:
                                using (var fileStream = File.Create(fileChooserDialog.Filename + (fileChooserDialog.Filename.ToLower().EndsWith(".obj") ? "" : ".obj")))
                                {
                                    new OBJ(geom, morphs).Write(new StreamWriter(fileStream));
                                }
                                break;
                            case MeshFileType.WSO:
                                using (var fileStream = File.Create(fileChooserDialog.Filename + (fileChooserDialog.Filename.ToLower().EndsWith(".wso") ? "" : ".wso")))
                                {
                                    new WSO(geom, morphs).Write(new BinaryWriter(fileStream));
                                }
                                break;
                        }
                    }
                    fileChooserDialog.Destroy();
                },
            importMesh = (MeshFileType meshFileType) =>
                {
                    switch (meshFileType)
                    {
                        case MeshFileType.OBJ:
                        case MeshFileType.WSO:
                            break;
                        default:
                            return;
                    }
                    var fileChooserDialog = new FileChooserDialog("Import " + meshFileType.ToString(), this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
                    var fileFilter = new FileFilter
                        {
                            Name = meshFileType == MeshFileType.OBJ ? "Wavefront OBJ" : meshFileType == MeshFileType.WSO ? "The Sims Resource Workshop Object" : null
                        };
                    fileFilter.AddPattern(meshFileType == MeshFileType.OBJ ? "*.obj" : meshFileType == MeshFileType.WSO ? "*.wso" : null);
                    fileChooserDialog.AddFilter(fileFilter);
                    var geom = lodKvp.Value[geomNotebook.CurrentPage].ToGEOM();
                    if (fileChooserDialog.Run() == (int)ResponseType.Accept)
                    {
                        byte[] bblnIndices =
                            {
                                casPart.CASPartResource.BlendInfoFatIndex,
                                casPart.CASPartResource.BlendInfoFitIndex,
                                casPart.CASPartResource.BlendInfoThinIndex,
                                casPart.CASPartResource.BlendInfoSpecialIndex
                            };
                        var bblnResourceIndexEntries = new IResourceIndexEntry[bblnIndices.Length];
                        var morphsEvaluated = new ResourceUtils.EvaluatedResourceKey?[bblnIndices.Length];
                        for (var i = 0; i < bblnIndices.Length; i++)
                        {
                            BBLN bbln;
                            ResourceUtils.EvaluatedResourceKey evaluated;
                            try
                            {
                                evaluated = casPart.ParentPackage.EvaluateResourceKey(casPart.CASPartResource.TGIBlocks[bblnIndices[i]].ReverseEvaluateResourceKey());
                                bbln = new BBLN(new BinaryReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream));
                                bblnResourceIndexEntries[i] = evaluated.ResourceIndexEntry;
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                                morphsEvaluated[i] = null;
                                continue;
                            }
                            try
                            {
                                morphsEvaluated[i] = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.BGEOTGI).ReverseEvaluateResourceKey());
                                continue;
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                            }
                            foreach (var entry in bbln.Entries)
                            {
                                foreach (var geomMorph in entry.GEOMMorphs)
                                {
                                    if (bbln.TGIList != null && bbln.TGIList.Length > geomMorph.TGIIndex && geom.HasVertexIDs)
                                    {
                                        try
                                        {
                                            morphsEvaluated[i] = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.TGIList[geomMorph.TGIIndex]).ReverseEvaluateResourceKey());
                                        }
                                        catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                                        {
                                            morphsEvaluated[i] = null;
                                        }
                                    }
                                }
                            }
                        }
                        using (var fileStream = File.OpenRead(fileChooserDialog.Filename))
                        {
                            var newGEOMPlusMorphs = GEOM.GEOMsFromOBJ(meshFileType == MeshFileType.OBJ ? new OBJ(new StreamReader(fileStream)) : meshFileType == MeshFileType.WSO ? new OBJ(new WSO(new BinaryReader(fileStream))) : null, geom, new TGI(), false, false);
                            for (var i = newGEOMPlusMorphs.Length - 1; i > -1 ; i--)
                            {
                                var stream = new MemoryStream();
                                newGEOMPlusMorphs[i].Write(new BinaryWriter(stream));
                                int selectedGEOMIndex = geomNotebook.CurrentPage,
                                selectedLODIndex = ResourcePropertyNotebook.CurrentPage;
                                if (i == 0)
                                {
                                    foreach (var geometryResourceKvp in PreloadedGeometryResources)
                                    {
                                        if (geometryResourceKvp.Value == lodKvp.Value[selectedGEOMIndex])
                                        {
                                            var evaluated = casPart.ParentPackage.EvaluateResourceKey(geometryResourceKvp.Key);
                                            var tempResourceIndexEntry = casPart.ParentPackage.AddResource(evaluated.ResourceIndexEntry, stream, false);
                                            casPart.ParentPackage.ResolveResourceType(tempResourceIndexEntry);
                                            var resource = WrapperDealer.GetResource(0, casPart.ParentPackage, tempResourceIndexEntry);
                                            evaluated.Package.ReplaceResource(evaluated.ResourceIndexEntry, resource);
                                            casPart.ParentPackage.DeleteResource(tempResourceIndexEntry);
                                            PreloadedGeometryResources[geometryResourceKvp.Key] = (GeometryResource)resource;
                                            casPart.LoadLODs(PreloadedGeometryResources, PreloadedVPXYResources);
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
                                else if (morphsEvaluated[i - 1].HasValue)
                                {
                                    var lodMorphMeshes = new GEOM[4][];
                                    var morphEvaluated = morphsEvaluated[i - 1].Value;
                                    var morphName = "_fat _fit _thin _special".Split(' ')[i - 1];
                                    if (morphEvaluated.ResourceIndexEntry.GetResourceTypeTag() == "BGEO")
                                    {
                                        var bgeo = new BGEO(new BinaryReader(WrapperDealer.GetResource(0, morphEvaluated.Package, morphEvaluated.ResourceIndexEntry).Stream));
                                        for (var j = 0; j < lodMorphMeshes.Length; j++)
                                        {
                                            lodMorphMeshes[j] = new GEOM[]
                                                {
                                                    j == lodKvp.Key ? newGEOMPlusMorphs[i] : new GEOM(newGEOMPlusMorphs[0], bgeo, 0, j)
                                                };
                                        }
                                    }
                                    else
                                    {
                                        var vpxy = new VPXY(new BinaryReader(WrapperDealer.GetResource(0, morphEvaluated.Package, morphEvaluated.ResourceIndexEntry).Stream));
                                        for (var j = 0; j < lodMorphMeshes.Length; j++)
                                        {
                                            lodMorphMeshes[j] = j == lodKvp.Key ? new GEOM[]
                                                {
                                                    newGEOMPlusMorphs[i]
                                                } : Array.ConvertAll(vpxy.MeshLinks(j), x => PreloadedGeometryResources[new ResourceUtils.ResourceKey(x).ReverseEvaluateResourceKey()].ToGEOM());
                                        }
                                        for (var j = 0; j < lodMorphMeshes.Length; j++)
                                        {
                                            var meshLinks = vpxy.MeshLinks(j);
                                            for (var k = 0; k < meshLinks.Length; k++)
                                            {
                                                var key = new ResourceUtils.ResourceKey(meshLinks[k]).ReverseEvaluateResourceKey();
                                                var evaluated = casPart.ParentPackage.EvaluateResourceKey(key);
                                                evaluated.Package.DeleteResource(evaluated.ResourceIndexEntry);
                                                PreloadedGeometryResources.Remove(key);
                                            }
                                        }
                                    }
                                    var geomTGIs = new TGI[lodMorphMeshes.Length][];
                                    var group = 0u;
                                    foreach (var j in casPart.CASPartResource.Diffuse1Indexes)
                                    {
                                        group = casPart.CASPartResource.TGIBlocks[j].ResourceGroup;
                                    }
                                    foreach (var j in casPart.CASPartResource.Specular1Indexes)
                                    {
                                        group = casPart.CASPartResource.TGIBlocks[j].ResourceGroup;
                                    }
                                    for (var j = 0; j < lodMorphMeshes.Length; j++)
                                    {
                                        geomTGIs[j] = new TGI[lodMorphMeshes[j].Length];
                                        for (var k = 0; k < geomTGIs[j].Length; k++)
                                        {
                                            var temp = "_lod" + j.ToString();
                                            if (k > 0)
                                            {
                                                temp += "-" + (k + 1).ToString();
                                            }
                                            temp += morphName;
                                            geomTGIs[j][k] = new TGI(ResourceUtils.GetResourceType("GEOM"), group, System.Security.Cryptography.FNV64.GetHash(casPart.CASPartResource.Unknown1 + morphName + Environment.UserName + Environment.TickCount.ToString() + temp));
                                            var geomResourceKey = new TGIBlock(0, null, geomTGIs[j][k].Type, geomTGIs[j][k].Group, geomTGIs[j][k].Instance);
                                            var geomStream = new MemoryStream();
                                            lodMorphMeshes[j][k].Write(new BinaryWriter(geomStream));
                                            var geomResourceIndexEntry = casPart.ParentPackage.AddResource(geomResourceKey, geomStream, true);
                                            PreloadedGeometryResources[geomResourceIndexEntry.ReverseEvaluateResourceKey()] = (GeometryResource)WrapperDealer.GetResource(0, casPart.ParentPackage, geomResourceIndexEntry);
                                        }
                                    }
                                    var vpxyTGI = new TGI(ResourceUtils.GetResourceType("VPXY"), 1, bblnResourceIndexEntries[i - 1].Instance);
                                    var newVPXY = new VPXY(vpxyTGI, geomTGIs);
                                    var newBBLN = new BBLN(7, casPart.CASPartResource.Unknown1 + morphName, vpxyTGI);
                                    var vpxyResourceKey = new TGIBlock(0, null, bblnResourceIndexEntries[i - 1].ResourceType, bblnResourceIndexEntries[i - 1].ResourceGroup, bblnResourceIndexEntries[i - 1].Instance);
                                    var vpxyStream = new MemoryStream();
                                    newBBLN.Write(new BinaryWriter(vpxyStream));
                                    casPart.ParentPackage.DeleteResource(morphEvaluated.ResourceIndexEntry);
                                    casPart.ParentPackage.DeleteResource(bblnResourceIndexEntries[i - 1]);
                                    casPart.ParentPackage.AddResource(vpxyResourceKey, vpxyStream, true);
                                    vpxyResourceKey = new TGIBlock(0, null, vpxyTGI.Type, vpxyTGI.Group, vpxyTGI.Instance);
                                    vpxyStream = new MemoryStream();
                                    newVPXY.Write(new BinaryWriter(vpxyStream));
                                    var vpxyResourceIndexEntry = casPart.ParentPackage.AddResource(vpxyResourceKey, vpxyStream, true);
                                    PreloadedVPXYResources[vpxyResourceIndexEntry.ReverseEvaluateResourceKey()] = (GenericRCOLResource)WrapperDealer.GetResource(0, casPart.ParentPackage, vpxyResourceIndexEntry);
                                    casPart.CASPartResource.TGIBlocks[bblnIndices[i - 1]].ResourceGroup = bblnResourceIndexEntries[i - 1].ResourceGroup;
                                    casPart.CASPartResource.TGIBlocks[bblnIndices[i - 1]].Instance = bblnResourceIndexEntries[i - 1].Instance;
                                }
                            }
                        }
                    }
                    fileChooserDialog.Destroy();
                };
            exportGEOMAction.Activated += (sender, e) => exportMesh(MeshFileType.GEOM);
            exportOBJAction.Activated += (sender, e) => exportMesh(MeshFileType.OBJ);
            exportWSOAction.Activated += (sender, e) => exportMesh(MeshFileType.WSO);
            importGEOMAction.Activated += (sender, e) =>
                {
                    var fileChooserDialog = new FileChooserDialog("Import GEOM", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
                    var fileFilter = new FileFilter
                        {
                            Name = "The Sims 3 GEOM Resource"
                        };
                    fileFilter.AddPattern("*.simgeom");
                    fileChooserDialog.AddFilter(fileFilter);
                    if (fileChooserDialog.Run() == (int)ResponseType.Accept)
                    {
                        try
                        {
                            int selectedGEOMIndex = geomNotebook.CurrentPage,
                            selectedLODIndex = ResourcePropertyNotebook.CurrentPage;
                            foreach (var geometryResourceKvp in PreloadedGeometryResources)
                            {
                                if (geometryResourceKvp.Value == lodKvp.Value[selectedGEOMIndex])
                                {
                                    var evaluated = casPart.ParentPackage.EvaluateResourceKey(geometryResourceKvp.Key);
                                    var tempResourceIndexEntry = casPart.ParentPackage.AddResource(fileChooserDialog.Filename, evaluated.ResourceIndexEntry, false);
                                    casPart.ParentPackage.ResolveResourceType(tempResourceIndexEntry);
                                    var resource = WrapperDealer.GetResource(0, casPart.ParentPackage, tempResourceIndexEntry);
                                    evaluated.Package.ReplaceResource(evaluated.ResourceIndexEntry, resource);
                                    casPart.ParentPackage.DeleteResource(tempResourceIndexEntry);
                                    PreloadedGeometryResources[geometryResourceKvp.Key] = (GeometryResource)resource;
                                    casPart.LoadLODs(PreloadedGeometryResources, PreloadedVPXYResources);
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
                        catch (InvalidDataException ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    fileChooserDialog.Destroy();
                };
            importOBJAction.Activated += (sender, e) => importMesh(MeshFileType.OBJ);
            importWSOAction.Activated += (sender, e) => importMesh(MeshFileType.WSO);
            HScale fatnessHScale = new HScale(-1, 1, .01)
                {
                    Value = mFat - mThin
                },
            fitnessHScale = new HScale(0, 1, .01)
                {
                    Value = mFit
                },
            specialHScale = new HScale(0, 1, .01)
                {
                    Value = mSpecial
                };
            System.Action changeOtherSlidersAndUpdateModels = delegate
                {
                    for (var i = 0; i < casPart.LODs.Count; i++)
                    {
                        if (new List<int>(casPart.LODs.Keys)[i] == lodKvp.Key)
                        {
                            continue;
                        }
                        foreach (var child in ((VBox)ResourcePropertyNotebook.GetNthPage(i)).Children)
                        {
                            var hBox = child as HBox;
                            if (hBox != null)
                            {
                                var hScaleIndex = 0;
                                foreach (var hBoxChild in hBox.Children)
                                {
                                    var hScale = hBoxChild as HScale;
                                    if (hScale != null)
                                    {
                                        hScale.Value = hScaleIndex == 0 ? fatnessHScale.Value : hScaleIndex == 1 ? fitnessHScale.Value : specialHScale.Value;
                                        hScaleIndex++;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    ModelsNeedUpdated = true;
                };
            fatnessHScale.ValueChanged += (sender, e) =>
                {
                    mFat = fatnessHScale.Value > 0 ? (float)fatnessHScale.Value : 0;
                    mThin = fatnessHScale.Value < 0 ? (float)-fatnessHScale.Value : 0;
                    changeOtherSlidersAndUpdateModels();
                };
            fitnessHScale.ValueChanged += (sender, e) =>
                {
                    mFit = (float)fitnessHScale.Value;
                    changeOtherSlidersAndUpdateModels();
                };
            specialHScale.ValueChanged += (sender, e) =>
                {
                    mSpecial = (float)specialHScale.Value;
                    changeOtherSlidersAndUpdateModels();
                };
            var geomPageButtonHBox = new HBox(false, 0);
            geomPageButtonHBox.PackEnd(menuBar, true, true, 4);
            geomPageButtonHBox.PackStart(prevButtonAlignment, false, true, 4);
            geomPageButtonHBox.PackStart(nextButtonAlignment, false, true, 4);
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            geomPageButtonHBox.PackStart(new Image(assembly, "Destrospean.DestrospeanCASPEditor.Icons.Fatness.png")
                {
                    HeightRequest = WidgetUtils.SmallImageSize,
                    WidthRequest = WidgetUtils.SmallImageSize
                }, false, true, 4);
            geomPageButtonHBox.PackStart(fatnessHScale, true, true, 4);
            geomPageButtonHBox.PackStart(new Image(assembly, "Destrospean.DestrospeanCASPEditor.Icons.Fitness.png")
                {
                    HeightRequest = WidgetUtils.SmallImageSize,
                    WidthRequest = WidgetUtils.SmallImageSize
                }, false, true, 4);
            geomPageButtonHBox.PackStart(fitnessHScale, true, true, 4);
            geomPageButtonHBox.PackStart(new Image(assembly, "Destrospean.DestrospeanCASPEditor.Icons.BabyBump.png")
                {
                    HeightRequest = WidgetUtils.SmallImageSize,
                    WidthRequest = WidgetUtils.SmallImageSize
                }, false, true, 4);
            geomPageButtonHBox.PackStart(specialHScale, true, true, 4);
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
                mMeshes.Clear();
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
                    var key = ((IResourceIndexEntry)model.GetValue(iter, 4)).ReverseEvaluateResourceKey();
                    switch ((string)model.GetValue(iter, 0))
                    {
                        case "_IMG":
                            Image.Pixbuf = ImageUtils.PreloadedImagePixbufs[key][0];
                            break;
                        case "CASP":
                            mGLWidget.Show();
                            AddCASPartWidgets(PreloadedCASParts[key]);
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
        mSaveAsPath = null;
        mMeshes.Clear();
        PreloadedCASParts.Clear();
        PreloadedGeometryResources.Clear();
        Materials.Clear();
        DeleteTextures();
        PreloadedVPXYResources.Clear();
        ImageUtils.PreloadedGameImagePixbufs.Clear();
        ImageUtils.PreloadedGameImages.Clear();
        ImageUtils.PreloadedImagePixbufs.Clear();
        ImageUtils.PreloadedImages.Clear();
    }

    public ResponseType GetUnsavedChangesDialogResponseType()
    {
        var unsavedChangesDialog = new UnsavedChangesDialog(this);
        var responseType = (ResponseType)unsavedChangesDialog.Run();
        unsavedChangesDialog.Destroy();
        return responseType;
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
            var key = resourceIndexEntry.ReverseEvaluateResourceKey();
            var missingResourceKeyIndex = ResourceUtils.MissingResourceKeys.IndexOf(key);
            switch (tag)
            {
                case "_IMG":
                    if (!ImageUtils.PreloadedImagePixbufs.ContainsKey(key) || missingResourceKeyIndex > -1)
                    {
                        CurrentPackage.PreloadImage(resourceIndexEntry, Image);
                        ImageUtils.PreloadedImagePixbufs[key].Add(ImageUtils.PreloadedImagePixbufs[key][0].ScaleSimple(WidgetUtils.SmallImageSize, WidgetUtils.SmallImageSize, Gdk.InterpType.Bilinear));
                    }
                    break;
                case "CASP":
                    if (!PreloadedCASParts.ContainsKey(key) || missingResourceKeyIndex > -1)
                    {
                        PreloadedCASParts[key] = new CASPart(CurrentPackage, resourceIndexEntry, PreloadedGeometryResources, PreloadedVPXYResources);
                    }
                    break;
                case "GEOM":
                    if (!PreloadedGeometryResources.ContainsKey(key) || missingResourceKeyIndex > -1)
                    {
                        PreloadedGeometryResources[key] = (GeometryResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry);
                    }
                    break;
                case "VPXY":
                    if (!PreloadedVPXYResources.ContainsKey(key) || missingResourceKeyIndex > -1)
                    {
                        PreloadedVPXYResources[key] = (GenericRCOLResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry);
                    }
                    break;
            }
            if (missingResourceKeyIndex > -1)
            {
                ResourceUtils.MissingResourceKeys.RemoveAt(missingResourceKeyIndex);
            }
        }
        foreach (var casPart in PreloadedCASParts.Values)
        {
            AddCASPartWidgets(casPart);
        }
        ResourceTreeView.Selection.SelectPath(new TreePath("0"));
    }

    public void SavePackage(string path = null)
    {
        if (string.IsNullOrEmpty(mSaveAsPath))
        {
            mSaveAsPath = path;
        }
        foreach (var casPartKvp in PreloadedCASParts)
        {
            if (ResourceUtils.MissingResourceKeys.Contains(casPartKvp.Key))
            {
                continue;
            }
            casPartKvp.Value.SavePresets();
            CurrentPackage.ReplaceResource(CurrentPackage.EvaluateResourceKey(casPartKvp.Key).ResourceIndexEntry, casPartKvp.Value.CASPartResource);
        }
        foreach (var geometryResourceKvp in PreloadedGeometryResources)
        {
            CurrentPackage.ReplaceResource(CurrentPackage.EvaluateResourceKey(geometryResourceKvp.Key).ResourceIndexEntry, geometryResourceKvp.Value);
        }
        foreach (var vpxyResourceKvp in PreloadedVPXYResources)
        {
            CurrentPackage.ReplaceResource(CurrentPackage.EvaluateResourceKey(vpxyResourceKvp.Key).ResourceIndexEntry, vpxyResourceKvp.Value);
        }
        CurrentPackage.FindAll(x => !x.IsDeleted && x.Compressed == 0).ForEach(x => x.Compressed = 0xFFFF);
        if (string.IsNullOrEmpty(mSaveAsPath))
        {
            CurrentPackage.SavePackage();
        }
        else
        {
            CurrentPackage.SaveAs(mSaveAsPath);
        }
        NextState = NextStateOptions.NoUnsavedChanges;
    }

    protected void OnCloseActionActivated(object sender, EventArgs e)
    {
        if (HasUnsavedChanges)
        {
            switch (GetUnsavedChangesDialogResponseType())
            {
                case ResponseType.Cancel:
                    return;
                case ResponseType.Yes:
                    SavePackage();
                    break;
            }
        }
        s3pi.Package.Package.ClosePackage(0, CurrentPackage);
        CurrentPackage = null;
        ResourceUtils.MissingResourceKeys.Clear();
        RefreshWidgets();
        NextState = NextStateOptions.NoUnsavedChanges;
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if (HasUnsavedChanges)
        {
            switch (GetUnsavedChangesDialogResponseType())
            {
                case ResponseType.Cancel:
                    a.RetVal = true;
                    return;
                case ResponseType.Yes:
                    SavePackage();
                    break;
            }
        }
        Application.Quit();
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
            catch (InvalidDataException ex)
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
        if (HasUnsavedChanges)
        {
            switch (GetUnsavedChangesDialogResponseType())
            {
                case ResponseType.Cancel:
                    return;
                case ResponseType.Yes:
                    SavePackage();
                    break;
            }
        }
        var fileChooserDialog = new FileChooserDialog("Open Package", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        var fileFilter = new FileFilter
            {
                Name = "The Sims 3 DBPF Package"
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
            catch (InvalidDataException ex)
            {
                Console.WriteLine(ex);
            }
        }
        fileChooserDialog.Destroy();
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        if (HasUnsavedChanges)
        {
            switch (GetUnsavedChangesDialogResponseType())
            {
                case ResponseType.Cancel:
                    return;
                case ResponseType.Yes:
                    SavePackage();
                    break;
            }
        }
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
                IResourceIndexEntry resourceIndexEntry = CurrentPackage.GetResourceIndexEntry((IResourceIndexEntry)model.GetValue(iter, 4)),
                tempResourceIndexEntry = CurrentPackage.AddResource(fileChooserDialog.Filename, resourceIndexEntry, false);
                CurrentPackage.ResolveResourceType(tempResourceIndexEntry);
                CurrentPackage.ReplaceResource(resourceIndexEntry, WrapperDealer.GetResource(0, CurrentPackage, tempResourceIndexEntry));
                CurrentPackage.DeleteResource(tempResourceIndexEntry);
                ResourceUtils.MissingResourceKeys.Add(resourceIndexEntry.ReverseEvaluateResourceKey());
                RefreshWidgets(false);
                NextState = NextStateOptions.UnsavedChanges;
            }
            catch (InvalidDataException ex)
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
        SavePackage();
    }

    protected void OnSaveAsActionActivated(object sender, EventArgs e)
    {
        var fileChooserDialog = new FileChooserDialog("Save Package As", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        var fileFilter = new FileFilter
            {
                Name = "The Sims 3 DBPF Package"
            };
        fileFilter.AddPattern("*.package");
        fileChooserDialog.AddFilter(fileFilter);
        if (fileChooserDialog.Run() == (int)ResponseType.Accept)
        {
            SavePackage(fileChooserDialog.Filename + (fileChooserDialog.Filename.ToLower().EndsWith(".package") ? "" : ".package"));
        }
        fileChooserDialog.Destroy();
    }

    protected void OnUseAdvancedShadersActionToggled(object sender, EventArgs e)
    {
        ApplicationSpecificSettings.UseAdvancedOpenGLShaders = UseAdvancedShadersAction.Active;
        mActiveShader = UseAdvancedShadersAction.Active ? "lit_advanced" : "textured";
    }
}
