using System;
using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor;
using Destrospean.DestrospeanCASPEditor.Widgets;
using Gtk;
using meshExpImp.ModelBlocks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

public partial class MainWindow : Window
{
    OpenTK.Vector3[] mColorData, mVertexData;

    int mFSID, mProgramID, mUniformModelview, mVBOColor, mVBOModelview, mVBOPosition, mVColor, mVPosition, mVSID;

    Matrix4[] mModelviewData;

    //List<Volume> mObjects = new List<Volume>();

    public IPackage CurrentPackage;

    public readonly Dictionary<IResourceIndexEntry, CASPart> CASParts = new Dictionary<IResourceIndexEntry, CASPart>();

    public readonly Dictionary<IResourceIndexEntry, GeometryResource> GeometryResources = new Dictionary<IResourceIndexEntry, GeometryResource>();

    public readonly Dictionary<IResourceIndexEntry, GenericRCOLResource> VPXYResources = new Dictionary<IResourceIndexEntry, GenericRCOLResource>();

    public bool GLInit;

    public GLWidget GLWidget;

    public readonly ListStore ResourceListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(IResourceIndexEntry));

    public MainWindow() : base(WindowType.Toplevel)
    {
        Build();
        RescaleAndReposition();
        BuildResourceTable();
        ApplicationSpecificSettings.LoadSettings();
        ResourcePropertyNotebook.RemovePage(0);
        GLWidget = new GLWidget
            {
                HeightRequest = Image.HeightRequest,
                WidthRequest = Image.WidthRequest
            };
        GLWidget.Initialized += (object sender, EventArgs e) => 
            {
                InitProgram();
                mVertexData = new OpenTK.Vector3[]
                    {
                        new OpenTK.Vector3(-.8f, -.8f, 0),
                        new OpenTK.Vector3(.8f, -.8f, 0),
                        new OpenTK.Vector3(0, .8f, 0)
                    };
                mColorData = new OpenTK.Vector3[]
                    {
                        new OpenTK.Vector3(1, 0, 0),
                        new OpenTK.Vector3(0, 0, 1),
                        new OpenTK.Vector3(0, 1, 0)
                    };
                mModelviewData = new Matrix4[]
                    {
                        Matrix4.Identity
                    };
                GLInit = true;
                GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
            };
        MainTable.Attach(new Image
            {
                HeightRequest = Image.HeightRequest,
                WidthRequest = Image.WidthRequest,
                Pixbuf = ImageUtils.CreateCheckerboard(Image.HeightRequest, (int)(8 * WidgetUtils.Scale), new Gdk.Color(191, 191, 191), new Gdk.Color(127, 127, 127))
            }, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        MainTable.Attach(GLWidget, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        MainTable.ShowAll();
        GLWidget.Hide();
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
        Button nextButton = new Button(),
        prevButton = new Button();
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
        flagNotebook.SwitchPage += (o, args) =>
            {
                nextButton.Sensitive = flagNotebook.CurrentPage < flagNotebook.NPages - 1;
                prevButton.Sensitive = flagNotebook.CurrentPage > 0;
            };
        flagPageButtonHBox.PackStart(prevButton, false, true, 4);
        flagPageButtonHBox.PackStart(nextButton, false, true, 4);
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
        ResourcePropertyTable.Attach(PresetNotebook.CreateInstance(casPart, Image), 1, 2, 0, 1);
        ResourcePropertyTable.ShowAll();
        BuildLODNotebook(casPart);
    }

    public void BuildLODNotebook(CASPart casPart)
    {
        foreach (var lod in casPart.LODs)
        {
            var notebook = new Notebook
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
            Button nextButton = new Button(),
            prevButton = new Button();
            nextButton.Add(new Arrow(ArrowType.Right, ShadowType.None)
                {
                    Xalign = .5f
                });
            prevButton.Add(new Arrow(ArrowType.Left, ShadowType.None)
                {
                    Xalign = .5f
                });
            nextButton.Clicked += (sender, e) => notebook.NextPage();
            prevButton.Clicked += (sender, e) => notebook.PrevPage();
            Alignment nextButtonAlignment = new Alignment(.5f, .5f, 0, 0),
            prevButtonAlignment = new Alignment(.5f, .5f, 0, 0);
            nextButtonAlignment.Add(nextButton);
            prevButtonAlignment.Add(prevButton);
            notebook.SwitchPage += (o, args) =>
                {
                    nextButton.Sensitive = notebook.CurrentPage < notebook.NPages - 1;
                    prevButton.Sensitive = notebook.CurrentPage > 0;
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
                                if (geometryResourceKvp.Value == lod[notebook.CurrentPage])
                                {
                                    IResourceIndexEntry resourceIndexEntry = geometryResourceKvp.Key,
                                    tempResourceIndexEntry = ResourceUtils.AddResource(CurrentPackage, fileChooserDialog.Filename, resourceIndexEntry, false);
                                    ResourceUtils.ResolveResourceType(CurrentPackage, tempResourceIndexEntry);
                                    CurrentPackage.ReplaceResource(resourceIndexEntry, WrapperDealer.GetResource(0, CurrentPackage, tempResourceIndexEntry));
                                    CurrentPackage.DeleteResource(tempResourceIndexEntry);
                                    RefreshWidgets();
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
            lodPageVBox.PackStart(notebook, true, true, 0);
            lodPageVBox.ShowAll();
            ResourcePropertyNotebook.AppendPage(lodPageVBox, new Label
                {
                    Text = "LOD " + ResourcePropertyNotebook.NPages.ToString()
                });
            lod.ForEach(x => WidgetUtils.AddPropertiesToNotebook(CurrentPackage, x, notebook, Image, this));
        }
    }

    public void BuildResourceTable()
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
                GLWidget.Hide();
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
                            GLWidget.Show();
                            AddCASPartWidgets(CASParts[resourceIndexEntry]);
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

    public void InitProgram()
    {
        mProgramID = GL.CreateProgram();
        LoadShader(@"
            #version 110
            attribute vec3 vPosition;
            attribute vec3 vColor;
            varying vec4 color;
            uniform mat4 modelview;
 
            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                color = vec4(vColor, 1.0);
            }", OpenTK.Graphics.OpenGL.ShaderType.VertexShader, mProgramID, out mVSID);
        LoadShader(@"
            #version 110
            varying vec4 color;
 
            void main()
            {
                gl_FragColor = color;
            }", OpenTK.Graphics.OpenGL.ShaderType.FragmentShader, mProgramID, out mFSID);
        GL.LinkProgram(mProgramID);
        Console.WriteLine(GL.GetProgramInfoLog(mProgramID));
        mVPosition = GL.GetAttribLocation(mProgramID, "vPosition");
        mVColor = GL.GetAttribLocation(mProgramID, "vColor");
        mUniformModelview = GL.GetUniformLocation(mProgramID, "modelview");
        if (mVPosition == -1 || mVColor == -1 || mUniformModelview == -1)
        {
            Console.WriteLine("Error binding attributes");
        }
        GL.GenBuffers(1, out mVBOPosition);
        GL.GenBuffers(1, out mVBOColor);
        GL.GenBuffers(1, out mVBOModelview);
    }

    public void LoadShader(String glsl, OpenTK.Graphics.OpenGL.ShaderType type, int program, out int address)
    {
        address = GL.CreateShader(type);
        GL.ShaderSource(address, glsl);
        GL.CompileShader(address);
        GL.AttachShader(program, address);
        Console.WriteLine(GL.GetShaderInfoLog(address));
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
                    ImageUtils.PreloadedImages[resourceIndexEntry].Add(ImageUtils.PreloadedImages[resourceIndexEntry][0].ScaleSimple(WidgetUtils.SmallImageSize, WidgetUtils.SmallImageSize, Gdk.InterpType.Bilinear));
                    break;
                case "CASP":
                    CASParts.Add(resourceIndexEntry, new CASPart(CurrentPackage, resourceIndexEntry, GeometryResources, VPXYResources));
                    break;
                case "GEOM":
                    if (!GeometryResources.ContainsKey(resourceIndexEntry))
                    {
                        GeometryResources.Add(resourceIndexEntry, (GeometryResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry));
                    }
                    break;
                case "VPXY":
                    if (!VPXYResources.ContainsKey(resourceIndexEntry))
                    {
                        VPXYResources.Add(resourceIndexEntry, (GenericRCOLResource)WrapperDealer.GetResource(0, CurrentPackage, resourceIndexEntry));
                    }
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
        ResourceTreeView.Selection.SelectPath(new TreePath("0"));
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
        Move(((int)((float)monitorGeometry.Width / WidgetUtils.WineScaleDenominator) - WidthRequest) >> 1, ((int)((float)monitorGeometry.Height / WidgetUtils.WineScaleDenominator) - HeightRequest) >> 1);
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

    protected bool OnIdleProcessMain ()
    {
        if (GLInit)
        {
            RenderFrame();
            return true;
        }
        return false;
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
                IResourceIndexEntry resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4),
                tempResourceIndexEntry = ResourceUtils.AddResource(CurrentPackage, fileChooserDialog.Filename, resourceIndexEntry, false);
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
        if (ResourceTreeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path, out column, out x, out y))
        {
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

    protected void RenderFrame()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, mVBOPosition);
        GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mVertexData.Length * OpenTK.Vector3.SizeInBytes), mVertexData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(mVPosition, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, mVBOColor);
        GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mColorData.Length * OpenTK.Vector3.SizeInBytes), mColorData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(mVColor, 3, VertexAttribPointerType.Float, true, 0, 0);
        GL.UniformMatrix4(mUniformModelview, false, ref mModelviewData[0]);
        GL.UseProgram(mProgramID);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        int height = Image.HeightRequest,
        width = Image.WidthRequest;  
        float aspectRatio = width / height; 
        GL.Viewport(0, 0, width, height);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();
        GL.ShadeModel(ShadingModel.Smooth);         
        var projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspectRatio, 1, 64);
        GL.MatrixMode(MatrixMode.Projection);           
        GL.LoadMatrix(ref projection);          
        GL.ClearDepth(1);              
        GL.Disable(EnableCap.DepthTest);    
        GL.Enable(EnableCap.Texture2D); 
        GL.Enable(EnableCap.Blend);
        GL.DepthFunc(DepthFunction.Always);     
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest); 
        GL.ClearColor(System.Drawing.Color.CornflowerBlue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.EnableVertexAttribArray(mVPosition);
        GL.EnableVertexAttribArray(mVColor);
        /*
        int indiceat = 0;
        foreach (Volume v in mObjects)
        {
            GL.UniformMatrix4(uniform_mview, false, ref v.ModelViewProjectionMatrix);
            GL.DrawElements(BeginMode.Triangles, v.IndexCount, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
            indiceat += v.IndexCount;
        }
        */
        GL.DrawArrays(BeginMode.Triangles, 0, 3);
        GL.DisableVertexAttribArray(mVPosition);
        GL.DisableVertexAttribArray(mVColor);
        GL.Flush();
        OpenTK.Graphics.GraphicsContext.CurrentContext.SwapBuffers();
    }
}
