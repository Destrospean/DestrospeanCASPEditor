using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
    Light mActiveLight = new Light(new OpenTK.Vector3(), new OpenTK.Vector3(.9f, .8f, .8f));

    string mActiveShader = "default";

    Camera mCamera = new Camera();

    OpenTK.Vector3[] mColorData, mNormalData, mVertexData;

    bool mGLInitialized = false;

    int mIBOElements, mMouseButtonHeld;

    int[] mIndexData;

    OpenTK.Vector2 mLastMousePosition;

    readonly Dictionary<String, Material> mMaterials = new Dictionary<string, Material>();

    float mMouseX, mMouseY, mTime = 0;

    readonly List<Volume> mObjects = new List<Volume>();

    readonly Dictionary<string, ShaderProgram> mShaders = new Dictionary<string, ShaderProgram>();

    OpenTK.Vector2[] mTextureCoordinateData;

    readonly Dictionary<string, int> mTextureIDs = new Dictionary<string, int>();

    Matrix4 mViewMatrix = Matrix4.Identity;

    public IPackage CurrentPackage;

    public readonly Dictionary<IResourceIndexEntry, CASPart> CASParts = new Dictionary<IResourceIndexEntry, CASPart>();

    public readonly Dictionary<IResourceIndexEntry, GeometryResource> GeometryResources = new Dictionary<IResourceIndexEntry, GeometryResource>();

    public readonly Dictionary<IResourceIndexEntry, GenericRCOLResource> VPXYResources = new Dictionary<IResourceIndexEntry, GenericRCOLResource>();

    public readonly List<SwitchPageHandler> ResourcePropertyNotebookSwitchPageHandlers = new List<SwitchPageHandler>();

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
        GLWidget.AddEvents((int)(Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask));
        GLWidget.ButtonPressEvent += (o, args) =>
            {
                mMouseButtonHeld = (int)args.Event.Button;
            };
        GLWidget.ButtonReleaseEvent += (o, args) =>
            {
                mMouseButtonHeld = -1;
            };
        GLWidget.MotionNotifyEvent += (o, args) =>
            {
                if (args.Event.Device.Source == Gdk.InputSource.Mouse)
                {
                    double currentX = args.Event.X,
                    currentY = args.Event.Y;
                    var currentTime = args.Event.Time;
                    if (mTime > 0)
                    {
                        double deltaX = currentX - mMouseX,
                        deltaY = currentY - mMouseY,
                        distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY),
                        secondsElapsed = (currentTime - mTime) / 1000;
                        if (secondsElapsed > 0)
                        {
                            mCamera.MoveSpeed = (float)(distance / secondsElapsed) / 10000;
                        }
                    }
                    mMouseX = (float)currentX;
                    mMouseY = (float)currentY;
                    mTime = (float)currentTime;
                }
            };
        GLWidget.Initialized += (object sender, EventArgs e) => 
            {
                InitProgram();
                mGLInitialized = true;
                TreeIter iter;
                TreeModel model;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter))
                {
                    var resourceIndexEntry = (IResourceIndexEntry)model.GetValue(iter, 4);
                    if ((string)model.GetValue(iter, 0) == "CASP")
                    {
                        mObjects.Clear();
                        LoadGEOMs(CASParts[resourceIndexEntry]);
                    }
                }
                GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
            };
        MainTable.Attach(new Gtk.Image
            {
                HeightRequest = Image.HeightRequest,
                WidthRequest = Image.WidthRequest,
                Pixbuf = ImageUtils.CreateCheckerboard(Image.HeightRequest, (int)(8 * WidgetUtils.Scale), new Gdk.Color(191, 191, 191), new Gdk.Color(127, 127, 127))
            }, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        MainTable.Attach(GLWidget, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        MainTable.ShowAll();
        GLWidget.Hide();
    }

    void ProcessInput()
    {
        var delta = mLastMousePosition - new OpenTK.Vector2(mMouseX, mMouseY);
        mLastMousePosition += delta;
        switch (mMouseButtonHeld)
        {
            case 1:
                mCamera.AddRotation(delta.X, delta.Y);
                break;
            case 2:
                mCamera.Move(delta.X, 0, delta.Y);
                break;
            case 3:
                mCamera.Move(delta.X, delta.Y, 0);
                break;
        }
        mLastMousePosition = new OpenTK.Vector2(mMouseX, mMouseY);
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

    public void BuildLODNotebook(CASPart casPart, int startLODPageIndex = 0, int startGEOMPageIndex = 0)
    {
        foreach (var switchPageHandler in ResourcePropertyNotebookSwitchPageHandlers)
        {
            ResourcePropertyNotebook.SwitchPage -= switchPageHandler;
        }
        ResourcePropertyNotebookSwitchPageHandlers.Clear();
        ResourcePropertyNotebookSwitchPageHandlers.Insert(0, (o, args) =>
            {
                mObjects.Clear();
                LoadGEOMs(casPart);
            });
        ResourcePropertyNotebook.SwitchPage += ResourcePropertyNotebookSwitchPageHandlers[0];
        foreach (var lod in casPart.LODs)
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
                                if (geometryResourceKvp.Value == lod[selectedGEOMIndex])
                                {
                                    IResourceIndexEntry resourceIndexEntry = geometryResourceKvp.Key,
                                    tempResourceIndexEntry = ResourceUtils.AddResource(CurrentPackage, fileChooserDialog.Filename, resourceIndexEntry, false);
                                    ResourceUtils.ResolveResourceType(CurrentPackage, tempResourceIndexEntry);
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
            ResourcePropertyNotebook.AppendPage(lodPageVBox, new Label
                {
                    Text = "LOD " + ResourcePropertyNotebook.NPages.ToString()
                });
            lod.ForEach(x => WidgetUtils.AddPropertiesToNotebook(CurrentPackage, x, geomNotebook, Image, this));
            if (lod == casPart.LODs[startLODPageIndex])
            {
                ResourcePropertyNotebook.CurrentPage = startLODPageIndex;
                geomNotebook.CurrentPage = startGEOMPageIndex;
            }
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
                mObjects.Clear();
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
                            Image.Pixbuf = ImageUtils.PreloadedImagePixbufs[resourceIndexEntry][0];
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
        mObjects.Clear();
        foreach (var textureId in mTextureIDs.Values)
        {
            GL.DeleteTexture(textureId);
        }
        mTextureIDs.Clear();
        CASParts.Clear();
        GeometryResources.Clear();
        VPXYResources.Clear();
        ImageUtils.PreloadedGameImagePixbufs.Clear();
        ImageUtils.PreloadedGameImages.Clear();
        ImageUtils.PreloadedImagePixbufs.Clear();
        ImageUtils.PreloadedImages.Clear();
    }

    public void InitProgram()
    {
        GL.GenBuffers(1, out mIBOElements);
        mShaders.Add("default", new ShaderProgram(@"
            #version 120
            attribute vec3 vPosition;
            attribute vec3 vColor;
            varying vec4 color;
            uniform mat4 modelview;
 
            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                color = vec4(vColor, 1.0);
            }", @"
            #version 120
            varying vec4 color;
 
            void main()
            {
                gl_FragColor = color;
            }"));
        mShaders.Add("textured", new ShaderProgram(@"
            #version 120
            attribute vec3 vPosition;
            attribute vec2 texcoord;
            varying vec2 f_texcoord;
            uniform mat4 modelview;

            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                f_texcoord = texcoord;
            }", @"
            #version 120
            varying vec2 f_texcoord;
            uniform sampler2D maintexture;
 
            void main()
            {
                vec2 flipped_texcoord = vec2(f_texcoord.x, 1.0 - f_texcoord.y);
                gl_FragColor = texture2D(maintexture, flipped_texcoord);
            }"));
        mShaders.Add("normal", new ShaderProgram(@"
            #version 120
            attribute vec3 vPosition;
            attribute vec3 vNormal;
            varying vec3 v_norm;
            uniform mat4 modelview;
 
            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                v_norm = normalize(mat3(modelview) * vNormal);
                v_norm = vNormal;
            }", @"
            #version 120
            varying vec3 v_norm;
 
            void main()
            {
                vec3 n = normalize(v_norm);
                gl_FragColor = vec4(0.5 + 0.5 * n, 1.0);
            }"));
        mShaders.Add("lit", new ShaderProgram(@"
            #version 120
            attribute vec3 vPosition;
            attribute vec3 vNormal;
            attribute vec2 texcoord;
            varying vec3 v_norm;
            varying vec3 v_pos;
            varying vec2 f_texcoord;
            uniform mat4 modelview;
            uniform mat4 model;
            uniform mat4 view;

            mat3 inverse(mat3 m)
            {
                vec3 c0 = m[0];
                vec3 c1 = m[1];
                vec3 c2 = m[2];
                vec3 v0 = cross(c1, c2);
                vec3 v1 = cross(c2, c0);
                vec3 v2 = cross(c0, c1);
                float inv_det = 1.0 / dot(c0, v0);
                return mat3(
                    v0.x * inv_det, v0.y * inv_det, v0.z * inv_det,
                    v1.x * inv_det, v1.y * inv_det, v1.z * inv_det,
                    v2.x * inv_det, v2.y * inv_det, v2.z * inv_det
                );
            }

            mat4 inverse(mat4 m)
            {
                float Coef00 = m[2][2] * m[3][3] - m[3][2] * m[2][3];
                float Coef02 = m[1][2] * m[3][3] - m[3][2] * m[1][3];
                float Coef03 = m[1][2] * m[2][3] - m[2][2] * m[1][3];
                float Coef04 = m[2][1] * m[3][3] - m[3][1] * m[2][3];
                float Coef06 = m[1][1] * m[3][3] - m[3][1] * m[1][3];
                float Coef07 = m[1][1] * m[2][3] - m[2][1] * m[1][3];
                float Coef08 = m[2][1] * m[3][2] - m[3][1] * m[2][2];
                float Coef10 = m[1][1] * m[3][2] - m[3][1] * m[1][2];
                float Coef11 = m[1][1] * m[2][2] - m[2][1] * m[1][2];
                float Coef12 = m[2][0] * m[3][3] - m[3][0] * m[2][3];
                float Coef14 = m[1][0] * m[3][3] - m[3][0] * m[1][3];
                float Coef15 = m[1][0] * m[2][3] - m[2][0] * m[1][3];
                float Coef16 = m[2][0] * m[3][2] - m[3][0] * m[2][2];
                float Coef18 = m[1][0] * m[3][2] - m[3][0] * m[1][2];
                float Coef19 = m[1][0] * m[2][2] - m[2][0] * m[1][2];
                float Coef20 = m[2][0] * m[3][1] - m[3][0] * m[2][1];
                float Coef22 = m[1][0] * m[3][1] - m[3][0] * m[1][1];
                float Coef23 = m[1][0] * m[2][1] - m[2][0] * m[1][1];
                vec4 Fac0 = vec4(Coef00, Coef00, Coef02, Coef03);
                vec4 Fac1 = vec4(Coef04, Coef04, Coef06, Coef07);
                vec4 Fac2 = vec4(Coef08, Coef08, Coef10, Coef11);
                vec4 Fac3 = vec4(Coef12, Coef12, Coef14, Coef15);
                vec4 Fac4 = vec4(Coef16, Coef16, Coef18, Coef19);
                vec4 Fac5 = vec4(Coef20, Coef20, Coef22, Coef23);
                vec4 Vec0 = vec4(m[1][0], m[0][0], m[0][0], m[0][0]);
                vec4 Vec1 = vec4(m[1][1], m[0][1], m[0][1], m[0][1]);
                vec4 Vec2 = vec4(m[1][2], m[0][2], m[0][2], m[0][2]);
                vec4 Vec3 = vec4(m[1][3], m[0][3], m[0][3], m[0][3]);
                vec4 Inv0 = Vec1 * Fac0 - Vec2 * Fac1 + Vec3 * Fac2;
                vec4 Inv1 = Vec0 * Fac0 - Vec2 * Fac3 + Vec3 * Fac4;
                vec4 Inv2 = Vec0 * Fac1 - Vec1 * Fac3 + Vec3 * Fac5;
                vec4 Inv3 = Vec0 * Fac2 - Vec1 * Fac4 + Vec2 * Fac5;
                vec4 SignA = vec4(1.0, -1.0, 1.0, -1.0);
                vec4 SignB = vec4(-1.0, 1.0, -1.0, 1.0);
                mat4 Inverse = mat4(
                    Inv0 * SignA,
                    Inv1 * SignB,
                    Inv2 * SignA,
                    Inv3 * SignB
                );
                vec4 row0 = Inverse[0];
                float det = dot(m[0], row0);
                return Inverse * (1.0 / det);
            }

            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                f_texcoord = texcoord;

                mat3 normMatrix = transpose(inverse(mat3(model)));
                v_norm = normMatrix * vNormal;
                v_pos = (model * vec4(vPosition, 1.0)).xyz;
            }", @"
            #version 120
            varying vec3 v_norm;
            varying vec3 v_pos;
            varying vec2 f_texcoord;
            uniform sampler2D maintexture;
            uniform mat4 view;
            uniform vec3 material_ambient;
            uniform vec3 material_diffuse;
            uniform vec3 material_specular;
            uniform float material_specExponent;
            uniform vec3 light_position;
            uniform vec3 light_color;
            uniform float light_ambientIntensity;
            uniform float light_diffuseIntensity;
            
            mat3 inverse(mat3 m)
            {
                vec3 c0 = m[0];
                vec3 c1 = m[1];
                vec3 c2 = m[2];
                vec3 v0 = cross(c1, c2);
                vec3 v1 = cross(c2, c0);
                vec3 v2 = cross(c0, c1);
                float inv_det = 1.0 / dot(c0, v0);
                return mat3(
                    v0.x * inv_det, v0.y * inv_det, v0.z * inv_det,
                    v1.x * inv_det, v1.y * inv_det, v1.z * inv_det,
                    v2.x * inv_det, v2.y * inv_det, v2.z * inv_det
                );
            }

            mat4 inverse(mat4 m)
            {
                float Coef00 = m[2][2] * m[3][3] - m[3][2] * m[2][3];
                float Coef02 = m[1][2] * m[3][3] - m[3][2] * m[1][3];
                float Coef03 = m[1][2] * m[2][3] - m[2][2] * m[1][3];
                float Coef04 = m[2][1] * m[3][3] - m[3][1] * m[2][3];
                float Coef06 = m[1][1] * m[3][3] - m[3][1] * m[1][3];
                float Coef07 = m[1][1] * m[2][3] - m[2][1] * m[1][3];
                float Coef08 = m[2][1] * m[3][2] - m[3][1] * m[2][2];
                float Coef10 = m[1][1] * m[3][2] - m[3][1] * m[1][2];
                float Coef11 = m[1][1] * m[2][2] - m[2][1] * m[1][2];
                float Coef12 = m[2][0] * m[3][3] - m[3][0] * m[2][3];
                float Coef14 = m[1][0] * m[3][3] - m[3][0] * m[1][3];
                float Coef15 = m[1][0] * m[2][3] - m[2][0] * m[1][3];
                float Coef16 = m[2][0] * m[3][2] - m[3][0] * m[2][2];
                float Coef18 = m[1][0] * m[3][2] - m[3][0] * m[1][2];
                float Coef19 = m[1][0] * m[2][2] - m[2][0] * m[1][2];
                float Coef20 = m[2][0] * m[3][1] - m[3][0] * m[2][1];
                float Coef22 = m[1][0] * m[3][1] - m[3][0] * m[1][1];
                float Coef23 = m[1][0] * m[2][1] - m[2][0] * m[1][1];
                vec4 Fac0 = vec4(Coef00, Coef00, Coef02, Coef03);
                vec4 Fac1 = vec4(Coef04, Coef04, Coef06, Coef07);
                vec4 Fac2 = vec4(Coef08, Coef08, Coef10, Coef11);
                vec4 Fac3 = vec4(Coef12, Coef12, Coef14, Coef15);
                vec4 Fac4 = vec4(Coef16, Coef16, Coef18, Coef19);
                vec4 Fac5 = vec4(Coef20, Coef20, Coef22, Coef23);
                vec4 Vec0 = vec4(m[1][0], m[0][0], m[0][0], m[0][0]);
                vec4 Vec1 = vec4(m[1][1], m[0][1], m[0][1], m[0][1]);
                vec4 Vec2 = vec4(m[1][2], m[0][2], m[0][2], m[0][2]);
                vec4 Vec3 = vec4(m[1][3], m[0][3], m[0][3], m[0][3]);
                vec4 Inv0 = Vec1 * Fac0 - Vec2 * Fac1 + Vec3 * Fac2;
                vec4 Inv1 = Vec0 * Fac0 - Vec2 * Fac3 + Vec3 * Fac4;
                vec4 Inv2 = Vec0 * Fac1 - Vec1 * Fac3 + Vec3 * Fac5;
                vec4 Inv3 = Vec0 * Fac2 - Vec1 * Fac4 + Vec2 * Fac5;
                vec4 SignA = vec4(1.0, -1.0, 1.0, -1.0);
                vec4 SignB = vec4(-1.0, 1.0, -1.0, 1.0);
                mat4 Inverse = mat4(
                    Inv0 * SignA,
                    Inv1 * SignB,
                    Inv2 * SignA,
                    Inv3 * SignB
                );
                vec4 row0 = Inverse[0];
                float det = dot(m[0], row0);
                return Inverse * (1.0 / det);
            }

            void main()
            {
                vec2 flipped_texcoord = vec2(f_texcoord.x, 1.0 - f_texcoord.y);
                vec3 n = normalize(v_norm);

                // Colors
                vec4 texcolor = texture2D(maintexture, flipped_texcoord.xy);
                vec4 light_ambient = light_ambientIntensity * vec4(light_color, 0.0);
                vec4 light_diffuse = light_diffuseIntensity * vec4(light_color, 0.0);

                // Ambient lighting
                gl_FragColor = texcolor * light_ambient * vec4(material_ambient, 0.0);

                // Diffuse lighting
                vec3 lightvec = normalize(light_position - v_pos);
                float lambertmaterial_diffuse = max(dot(n, lightvec), 0.0);
                gl_FragColor = gl_FragColor + (light_diffuse * texcolor * vec4(material_diffuse, 0.0)) * lambertmaterial_diffuse;

                // Specular lighting
                vec3 reflectionvec = normalize(reflect(-lightvec, v_norm));
                vec3 viewvec = normalize(vec3(inverse(view) * vec4(0,0,0,1)) - v_pos); 
                float material_specularreflection = max(dot(v_norm, lightvec), 0.0) * pow(max(dot(reflectionvec, viewvec), 0.0), material_specExponent);
                gl_FragColor = gl_FragColor + vec4(material_specular * light_color, 0.0) * material_specularreflection;
            }"));
        mActiveShader = "textured";
    }

    public void LoadGEOMs(CASPart casPart)
    {
        foreach (var geometryResource in casPart.LODs[ResourcePropertyNotebook.CurrentPage])
        {
            var geom = (GEOM)geometryResource.ChunkEntries[0].RCOLBlock;
            List<OpenTK.Vector3> colors = new List<OpenTK.Vector3>(),
            normals = new List<OpenTK.Vector3>(),
            vertices = new List<OpenTK.Vector3>();
            var faces = new List<Tuple<int, int, int>>();
            var textureCoordinates = new List<OpenTK.Vector2>();
            foreach (var face in geom.Faces)
            {
                faces.Add(new Tuple<int, int, int>(face.VertexDataIndex0, face.VertexDataIndex1, face.VertexDataIndex2));
            }
            foreach (var vertexDataElement in geom.VertexData)
            {
                foreach (var vertexElement in vertexDataElement.Vertex)
                {
                    var positionElement = vertexElement as GEOM.PositionElement;
                    if (positionElement != null && positionElement as GEOM.TangentNormalElement == null)
                    {
                        if (positionElement as GEOM.NormalElement != null)
                        {
                            normals.Add(new OpenTK.Vector3(positionElement.X, positionElement.Y, positionElement.Z));
                            continue;
                        }
                        vertices.Add(new OpenTK.Vector3(positionElement.X, positionElement.Y, positionElement.Z));
                        colors.Add(new OpenTK.Vector3(1, 1, 1));
                        continue;
                    }
                    var uvElement = vertexElement as GEOM.UVElement;
                    if (uvElement != null)
                    {
                        textureCoordinates.Add(new OpenTK.Vector2(uvElement.U, uvElement.V));
                    }
                }
            }
            var key = "";
            foreach (var geometryResourceKvp in GeometryResources)
            {
                if (geometryResourceKvp.Value == geometryResource)
                {
                    key = ResourceUtils.ReverseEvaluateResourceKey(geometryResourceKvp.Key);
                    break;
                }
            }
            Material material;
            if (!mMaterials.TryGetValue(key, out material))
            {
                var materialColors = new Dictionary<FieldType, OpenTK.Vector3>();
                var materialMapKeys = new Dictionary<FieldType, string>();
                foreach (var element in new List<ShaderData>(geom.Mtnf.SData))
                {
                    var elementFloat3 = element as ElementFloat3;
                    if (elementFloat3 != null)
                    {
                        materialColors[element.Field] = new OpenTK.Vector3(elementFloat3.Data0, elementFloat3.Data1, elementFloat3.Data2);
                        continue;
                    }
                    var elementTextureRef = element as ElementTextureRef;
                    if (elementTextureRef != null)
                    {
                        materialMapKeys[element.Field] = ResourceUtils.ReverseEvaluateResourceKey(element.ParentTGIBlocks[elementTextureRef.Index]);
                    }
                }
                OpenTK.Vector3 diffuseColor, specularColor;
                string diffuseMapKey, normalMapKey, specularMapKey;
                material = new Material
                    {
                        DiffuseColor = materialColors.TryGetValue(FieldType.Diffuse, out diffuseColor) ? diffuseColor : new OpenTK.Vector3(1, 1, 1),
                        SpecularColor = materialColors.TryGetValue(FieldType.Specular, out specularColor) ? specularColor : new OpenTK.Vector3(1, 1, 1),
                        DiffuseMap = materialMapKeys.TryGetValue(FieldType.DiffuseMap, out diffuseMapKey) ? diffuseMapKey : null,
                        NormalMap = materialMapKeys.TryGetValue(FieldType.NormalMap, out normalMapKey) ? normalMapKey : null,
                        SpecularMap = materialMapKeys.TryGetValue(FieldType.SpecularMap, out specularMapKey) ? specularMapKey : null
                    };
                mMaterials.Add(key, material);
            }
            mObjects.Add(new Volume
                {
                    ColorData = colors.ToArray(),
                    Faces = faces,
                    Material = material,
                    Normals = normals.ToArray(),
                    TextureCoordinates = textureCoordinates.ToArray(),
                    TextureID = material.DiffuseMap == null ? -1 : LoadTexture(material.DiffuseMap),
                    Vertices = vertices.ToArray()
                });
        }
    }

    public int LoadTexture(string key)
    {
        Bitmap image;
        if (!mGLInitialized || !ImageUtils.PreloadedGameImages.TryGetValue(key, out image) && !ImageUtils.PreloadedImages.TryGetValue(key, out image))
        {
            return -1;
        }
        int textureID;
        if (!mTextureIDs.TryGetValue(key, out textureID))
        {
            GL.GenTextures(1, out textureID);
            mTextureIDs.Add(key, textureID);
        }
        GL.BindTexture(TextureTarget.Texture2D, textureID);
        BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        image.UnlockBits(data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        return textureID;
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
            var missingResourceKeyIndex = ResourceUtils.MissingResourceKeys.IndexOf(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry));
            switch (tag)
            {
                case "_IMG":
                    if (!ImageUtils.PreloadedImagePixbufs.ContainsKey(resourceIndexEntry) || missingResourceKeyIndex > -1)
                    {
                        ImageUtils.PreloadImage(CurrentPackage, resourceIndexEntry, Image);
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
        RefreshWidgets(false);
    }

    protected void OnGameFoldersActionActivated(object sender, EventArgs e)
    {
        new GameFoldersDialog(this).ShowAll();
    }

    protected bool OnIdleProcessMain()
    {
        if (mGLInitialized)
        {
            OnUpdateFrame();
            OnRenderFrame();
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
                RefreshWidgets(false);
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

    protected void OnRenderFrame()
    {
        GL.Viewport(0, 0, Image.WidthRequest, Image.HeightRequest);
        GL.ClearColor(System.Drawing.Color.CornflowerBlue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.UseProgram(mShaders[mActiveShader].ProgramID);
        mShaders[mActiveShader].EnableVertexAttribArrays();
        int indexAt = 0;
        foreach (var volume in mObjects)
        {
            GL.BindTexture(TextureTarget.Texture2D, volume.TextureID);
            GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("modelview"), false, ref volume.ModelViewProjectionMatrix);
            if (mShaders[mActiveShader].GetAttribute("maintexture") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetAttribute("maintexture"), volume.TextureID);
            }
            if (mShaders[mActiveShader].GetUniform("view") != -1)
            {
                GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("view"), false, ref mViewMatrix);
            }
            if (mShaders[mActiveShader].GetUniform("model") != -1)
            {
                GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("model"), false, ref volume.ModelMatrix);
            }
            if (mShaders[mActiveShader].GetUniform("material_ambient") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_ambient"), ref volume.Material.AmbientColor);
            }
            if (mShaders[mActiveShader].GetUniform("material_diffuse") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_diffuse"), ref volume.Material.DiffuseColor);
            }
            if (mShaders[mActiveShader].GetUniform("material_specular") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_specular"), ref volume.Material.SpecularColor);
            }
            if (mShaders[mActiveShader].GetUniform("material_specExponent") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("material_specExponent"), volume.Material.SpecularExponent);
            }
            if (mShaders[mActiveShader].GetUniform("light_position") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("light_position"), ref mActiveLight.Position);
            }
            if (mShaders[mActiveShader].GetUniform("light_color") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("light_color"), ref mActiveLight.Color);
            }
            if (mShaders[mActiveShader].GetUniform("light_diffuseIntensity") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("light_diffuseIntensity"), mActiveLight.DiffuseIntensity);
            }
            if (mShaders[mActiveShader].GetUniform("light_ambientIntensity") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("light_ambientIntensity"), mActiveLight.AmbientIntensity);
            }
            GL.DrawElements(BeginMode.Triangles, volume.IndexCount, DrawElementsType.UnsignedInt, indexAt * sizeof(uint));
            indexAt += volume.IndexCount;
        }
        mShaders[mActiveShader].DisableVertexAttribArrays();
        GL.Flush();
        OpenTK.Graphics.GraphicsContext.CurrentContext.SwapBuffers();
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
                ResourceUtils.MissingResourceKeys.Add(ResourceUtils.ReverseEvaluateResourceKey(resourceIndexEntry));
                RefreshWidgets(false);
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

    protected void OnUpdateFrame()
    {
        ProcessInput();
        List<OpenTK.Vector3> colors = new List<OpenTK.Vector3>(),
        normals = new List<OpenTK.Vector3>(),
        vertices = new List<OpenTK.Vector3>();
        var indices = new List<int>();
        var textureCoordinates = new List<OpenTK.Vector2>();
        var vertexCount = 0;
        foreach (var volume in mObjects)
        {
            colors.AddRange(volume.ColorData);
            indices.AddRange(volume.GetIndices(vertexCount));
            normals.AddRange(volume.Normals);
            textureCoordinates.AddRange(volume.TextureCoordinates);
            vertices.AddRange(volume.Vertices);
            vertexCount += volume.VertexCount;
        }
        mColorData = colors.ToArray();
        mIndexData = indices.ToArray();
        mNormalData = normals.ToArray();
        mTextureCoordinateData = textureCoordinates.ToArray();
        mVertexData = vertices.ToArray();
        GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vPosition"));
        GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mVertexData.Length * OpenTK.Vector3.SizeInBytes), mVertexData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);
        if (mShaders[mActiveShader].GetAttribute("vColor") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vColor"));
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mColorData.Length * OpenTK.Vector3.SizeInBytes), mColorData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
        }
        if (mShaders[mActiveShader].GetAttribute("texcoord") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("texcoord"));
            GL.BufferData<OpenTK.Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(mTextureCoordinateData.Length * OpenTK.Vector2.SizeInBytes), mTextureCoordinateData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
        }
        if (mShaders[mActiveShader].GetAttribute("vNormal") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vNormal"));
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mNormalData.Length * OpenTK.Vector3.SizeInBytes), mNormalData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vNormal"), 3, VertexAttribPointerType.Float, true, 0, 0);
        }
        foreach (var volume in mObjects)
        {
            volume.CalculateModelMatrix();
            volume.ViewProjectionMatrix = mCamera.ViewMatrix * Matrix4.CreatePerspectiveFieldOfView(1, GLWidget.WidthRequest / (float)GLWidget.HeightRequest, 1, 40);
            volume.ModelViewProjectionMatrix = volume.ModelMatrix * volume.ViewProjectionMatrix;
            volume.Position = new OpenTK.Vector3(0, -1, -2);
            volume.Scale = new OpenTK.Vector3(1, 1, 1);
        }
        GL.UseProgram(mShaders[mActiveShader].ProgramID);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIBOElements);
        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mIndexData.Length * sizeof(int)), mIndexData, BufferUsageHint.StaticDraw);
        mViewMatrix = mCamera.ViewMatrix;
        System.Threading.Thread.Sleep(1);
    }
}
