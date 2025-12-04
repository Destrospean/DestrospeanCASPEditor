using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Destrospean.CmarNYCBorrowed;
using Destrospean.DestrospeanCASPEditor;
using Destrospean.DestrospeanCASPEditor.OpenGL;
using Gtk;
using meshExpImp.ModelBlocks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using s3pi.GenericRCOLResource;
using s3pi.WrapperDealer;
using Shader = Destrospean.DestrospeanCASPEditor.OpenGL.Shader;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;

public partial class MainWindow : Window
{
    const int kMaxLights = 5;

    string mActiveShader = "default";

    Camera mCamera = new Camera();

    Vector3[] mColorData, mNormalData, mVertexData;

    Vector3 mCurrentRotation = Vector3.Zero;

    bool mGLInitialized = false;

    float mFOV = MathHelper.DegreesToRadians(30),
    mFat = 0,
    mFit = 0,
    mMouseX,
    mMouseY,
    mSpecial = 0,
    mThin = 0,
    mTime = 0;

    GLWidget mGLWidget;

    int mIBOElements;

    int[] mIndexData;

    List<Gdk.Key> mKeysHeld = new List<Gdk.Key>();

    Vector2 mLastMousePosition;

    List<Light> mLights = new List<Light>();

    readonly List<Volume> mMeshes = new List<Volume>();

    MouseButtonsHeld mMouseButtonsHeld = MouseButtonsHeld.None;

    readonly Dictionary<string, Shader> mShaders = new Dictionary<string, Shader>();

    Vector2[] mTextureCoordinateData;

    Matrix4 mViewMatrix = Matrix4.Identity;

    public bool ModelsNeedUpdated = false;

    public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    public readonly Dictionary<string, int> TextureIDs = new Dictionary<string, int>();

    [Flags]
    enum MouseButtonsHeld : byte
    {
        None,
        Left,
        Middle,
        Right = 4,
    }

    void InitProgram()
    {
        GL.GenBuffers(1, out mIBOElements);
        string backportedFunctions = @"
            mat3 inverse(mat3 m)
            {
                vec3 c0 = m[0];
                vec3 c1 = m[1];
                vec3 c2 = m[2];
                vec3 v0 = cross(c1, c2);
                vec3 v1 = cross(c2, c0);
                vec3 v2 = cross(c0, c1);
                float inv_det = 1.0 / dot(c0, v0);
                return mat3(v0.x * inv_det, v0.y * inv_det, v0.z * inv_det, v1.x * inv_det, v1.y * inv_det, v1.z * inv_det, v2.x * inv_det, v2.y * inv_det, v2.z * inv_det);
            }

            mat4 inverse(mat4 m)
            {
                float c00 = m[2][2] * m[3][3] - m[3][2] * m[2][3];
                float c01 = m[1][2] * m[3][3] - m[3][2] * m[1][3];
                float c02 = m[1][2] * m[2][3] - m[2][2] * m[1][3];
                float c03 = m[2][1] * m[3][3] - m[3][1] * m[2][3];
                float c04 = m[1][1] * m[3][3] - m[3][1] * m[1][3];
                float c05 = m[1][1] * m[2][3] - m[2][1] * m[1][3];
                float c06 = m[2][1] * m[3][2] - m[3][1] * m[2][2];
                float c07 = m[1][1] * m[3][2] - m[3][1] * m[1][2];
                float c08 = m[1][1] * m[2][2] - m[2][1] * m[1][2];
                float c09 = m[2][0] * m[3][3] - m[3][0] * m[2][3];
                float c10 = m[1][0] * m[3][3] - m[3][0] * m[1][3];
                float c11 = m[1][0] * m[2][3] - m[2][0] * m[1][3];
                float c12 = m[2][0] * m[3][2] - m[3][0] * m[2][2];
                float c13 = m[1][0] * m[3][2] - m[3][0] * m[1][2];
                float c14 = m[1][0] * m[2][2] - m[2][0] * m[1][2];
                float c15 = m[2][0] * m[3][1] - m[3][0] * m[2][1];
                float c16 = m[1][0] * m[3][1] - m[3][0] * m[1][1];
                float c17 = m[1][0] * m[2][1] - m[2][0] * m[1][1];
                vec4 f0 = vec4(c00, c00, c01, c02);
                vec4 f1 = vec4(c03, c03, c04, c05);
                vec4 f2 = vec4(c06, c06, c07, c08);
                vec4 f3 = vec4(c09, c09, c10, c11);
                vec4 f4 = vec4(c12, c12, c13, c14);
                vec4 f5 = vec4(c15, c15, c16, c17);
                vec4 v0 = vec4(m[1][0], m[0][0], m[0][0], m[0][0]);
                vec4 v1 = vec4(m[1][1], m[0][1], m[0][1], m[0][1]);
                vec4 v2 = vec4(m[1][2], m[0][2], m[0][2], m[0][2]);
                vec4 v3 = vec4(m[1][3], m[0][3], m[0][3], m[0][3]);
                vec4 i0 = v1 * f0 - v2 * f1 + v3 * f2;
                vec4 i1 = v0 * f0 - v2 * f3 + v3 * f4;
                vec4 i2 = v0 * f1 - v1 * f3 + v3 * f5;
                vec4 i3 = v0 * f2 - v1 * f4 + v2 * f5;
                vec4 signA = vec4(1.0, -1.0, 1.0, -1.0);
                vec4 signB = vec4(-1.0, 1.0, -1.0, 1.0);
                mat4 inv = mat4(i0 * signA, i1 * signB, i2 * signA, i3 * signB);
                return inv * 1.0 / dot(m[0], inv[0]);
            }

            mat3 transpose(mat3 m)
            {
                return mat3(vec3(m[0].x, m[1].x, m[2].x), vec3(m[0].y, m[1].y, m[2].y), vec3(m[0].z, m[1].z, m[2].z));
            }

            mat4 transpose(mat4 m)
            {
                mat4 result;
                result[0][0] = m[0][0];
                result[0][1] = m[1][0];
                result[0][2] = m[2][0];
                result[0][3] = m[3][0];
                result[1][0] = m[0][1];
                result[1][1] = m[1][1];
                result[1][2] = m[2][1];
                result[1][3] = m[3][1];
                result[2][0] = m[0][2];
                result[2][1] = m[1][2];
                result[2][2] = m[2][2];
                result[2][3] = m[3][2];
                result[3][0] = m[0][3];
                result[3][1] = m[1][3];
                result[3][2] = m[2][3];
                result[3][3] = m[3][3];
                return result;
            }",
        litVertexShader = string.Format(@"
            #version 100

            precision highp float;

            attribute vec3 vPosition;
            attribute vec3 vNormal;
            attribute vec2 texcoord;
            varying vec3 v_norm;
            varying vec3 v_pos;
            varying vec2 f_texcoord;
            uniform mat4 modelview;
            uniform mat4 model;
            uniform mat4 view;

            {0}

            void main()
            {{
                gl_Position = modelview * vec4(vPosition, 1.0);
                f_texcoord = texcoord;

                mat3 normMatrix = transpose(inverse(mat3(model[0].xyz, model[1].xyz, model[2].xyz)));
                v_norm = normMatrix * vNormal;
                v_pos = (model * vec4(vPosition, 1.0)).xyz;
            }}", backportedFunctions);
        mShaders.Add("default", new Shader(@"
            #version 100

            precision highp float;

            attribute vec3 vPosition;
            attribute vec3 vColor;
            varying vec4 color;
            uniform mat4 modelview;
 
            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                color = vec4(vColor, 1.0);
            }", @"
            #version 100

            precision highp float;

            varying vec4 color;
 
            void main()
            {
                gl_FragColor = color;
            }"));
        mShaders.Add("textured", new Shader(@"
            #version 100

            precision highp float;

            attribute vec3 vPosition;
            attribute vec2 texcoord;
            varying vec2 f_texcoord;
            uniform mat4 modelview;

            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                f_texcoord = texcoord;
            }", @"
            #version 100

            precision highp float;

            varying vec2 f_texcoord;
            uniform sampler2D maintexture;
 
            void main()
            {
                vec4 texcolor = texture2D(maintexture, f_texcoord);
                if (texcolor.a < 0.1)
                {{
                    discard;
                }}
                gl_FragColor = texcolor;
            }"));
        mShaders.Add("normal", new Shader(@"
            #version 100

            precision highp float;

            attribute vec3 vPosition;
            attribute vec3 vNormal;
            varying vec3 v_norm;
            uniform mat4 modelview;
 
            void main()
            {
                gl_Position = modelview * vec4(vPosition, 1.0);
                v_norm = normalize(mat3(modelview[0].xyz, modelview[1].xyz, modelview[2].xyz) * vNormal);
                v_norm = vNormal;
            }", @"
            #version 100

            precision highp float;

            varying vec3 v_norm;
 
            void main()
            {
                vec3 n = normalize(v_norm);
                gl_FragColor = vec4(0.5 + 0.5 * n, 1.0);
            }"));
        mShaders.Add("lit", new Shader(litVertexShader, string.Format(@"
            #version 100

            precision highp float;

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

            {0}

            void main()
            {{
                vec3 n = normalize(v_norm);
                vec4 texcolor = texture2D(maintexture, f_texcoord);
                if (texcolor.a < 0.1)
                {{
                    discard;
                }}
                vec3 lightvec = normalize(light_position - v_pos);
                vec4 light_ambient = light_ambientIntensity * vec4(light_color, 0.0);
                vec4 light_diffuse = light_diffuseIntensity * vec4(light_color, 0.0);
                gl_FragColor = texcolor * light_ambient * vec4(material_ambient, 0.0);
                float lambertmaterial_diffuse = max(dot(n, lightvec), 0.0);
                gl_FragColor = gl_FragColor + light_diffuse * texcolor * vec4(material_diffuse, 0.0) * lambertmaterial_diffuse;
                vec3 reflectionvec = normalize(reflect(-lightvec, v_norm));
                vec3 viewvec = normalize(vec3(inverse(view) * vec4(0.0, 0.0, 0.0, 1.0)) - v_pos); 
                float material_specularreflection = max(dot(v_norm, lightvec), 0.0) * pow(max(dot(reflectionvec, viewvec), 0.0), material_specExponent);
                gl_FragColor = gl_FragColor + vec4(material_specular * light_color, 0.0) * material_specularreflection;
            }}", backportedFunctions)));
        mShaders.Add("lit_advanced", new Shader(litVertexShader, string.Format(@"
            #version 100

            precision highp float;

            struct Light
            {{
                vec3 position;
                vec3 color;
                float ambientIntensity;
                float diffuseIntensity;
                int type;
                vec3 direction;
                float coneAngle;
                float linearAttenuation;
                float quadraticAttenuation;
                float radius;
            }};
            varying vec3 v_norm;
            varying vec3 v_pos;
            varying vec2 f_texcoord;
            uniform sampler2D maintexture;
            uniform bool hasSpecularMap;
            uniform sampler2D map_specular;
            uniform mat4 view;
            uniform vec3 material_ambient;
            uniform vec3 material_diffuse;
            uniform vec3 material_specular;
            uniform float material_specExponent;
            uniform Light lights[5];

            {0}

            void main()
            {{
                vec3 n = normalize(v_norm);
                vec4 texcolor = texture2D(maintexture, f_texcoord);
                if (texcolor.a < 0.1)
                {{
                    discard;
                }}
                gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
                for (int i = 0; i < 5; i++)
                {{
                    if (lights[i].color == vec3(0.0, 0.0, 0.0))
                    {{
                        continue;
                    }}
                    vec3 lightvec = normalize(lights[i].position - v_pos);
                    vec4 lightcolor = vec4(0.0, 0.0, 0.0, 1.0);
                    if (lights[i].type == 0)
                    {{
                        lightvec = lights[i].direction;
                    }}
                    vec4 light_ambient = lights[i].ambientIntensity * vec4(lights[i].color, 0.0);
                    vec4 light_diffuse = lights[i].diffuseIntensity * vec4(lights[i].color, 0.0);
                    lightcolor = lightcolor + texcolor * light_ambient * vec4(material_ambient, 0.0);
                    float lambertmaterial_diffuse = max(dot(n, lightvec), 0.0);
                    bool inConeOrNotSpotlight = lights[i].type != 2 || degrees(acos(dot(lightvec, lights[i].direction))) < lights[i].coneAngle;
                    if (inConeOrNotSpotlight)
                    {{
                        lightcolor = lightcolor + light_diffuse * texcolor * vec4(material_diffuse, 0.0) * lambertmaterial_diffuse;
                    }}
                    vec3 reflectionvec = normalize(reflect(-lightvec, v_norm));
                    vec3 viewvec = normalize(vec3(inverse(view) * vec4(0.0, 0.0, 0.0, 1.0)) - v_pos); 
                    float material_specularreflection = max(dot(v_norm, lightvec), 0.0) * pow(max(dot(reflectionvec, viewvec), 0.0), material_specExponent);
                    if (hasSpecularMap)
                    {{
                        material_specularreflection = material_specularreflection * texture2D(map_specular, f_texcoord).r;
                    }}
                    if (inConeOrNotSpotlight)
                    {{
                        lightcolor = lightcolor + vec4(material_specular * lights[i].color, 0.0) * material_specularreflection;
                    }}
                    float distancefactor = distance(lights[i].position, v_pos);
                    gl_FragColor = gl_FragColor + lightcolor * 1.0 / (1.0 + distancefactor * lights[i].linearAttenuation + distancefactor * distancefactor * lights[i].quadraticAttenuation);
                }}
            }}", backportedFunctions)));
        mActiveShader = ApplicationSpecificSettings.UseAdvancedOpenGLShaders ? "lit_advanced" : "textured";
        mLights.Add(new Light(new Vector3(0, 1, 3), Vector3.One)
            {
                QuadraticAttenuation = .05f
            });
        mLights.Add(new Light(new Vector3(0, 1, -3), Vector3.One)
            {
                Direction = new Vector3(0, 0, -1),
                QuadraticAttenuation = .05f
            });
        mCamera.Position = new Vector3(0, 1, 4);
    }

    void LoadGEOMs(CASPart casPart)
    {
        mMeshes.Clear();
        if (!CASParts.ContainsValue(casPart))
        {
            return;
        }
        var lod = new List<int>(casPart.LODs.Keys)[ResourcePropertyNotebook.CurrentPage];
        foreach (var geometryResource in new List<List<GeometryResource>>(casPart.LODs.Values)[ResourcePropertyNotebook.CurrentPage])
        {
            var geom = geometryResource.ToGEOM();
            byte[] bblnIndices =
                {
                    casPart.CASPartResource.BlendInfoFatIndex,
                    casPart.CASPartResource.BlendInfoFitIndex,
                    casPart.CASPartResource.BlendInfoThinIndex,
                    casPart.CASPartResource.BlendInfoSpecialIndex
                };
            float[] scales =
                {
                    mFat,
                    mFit,
                    mThin,
                    mSpecial
                };
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
                    bbln = null;
                }
                if (bbln == null)
                {
                    continue;
                }
                BGEO bgeo;
                try
                {
                    evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.BGEOTGI).ReverseEvaluateResourceKey());
                    bgeo = ((CASPartResource.BlendGeometryResource)WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry)).ToBGEO();
                }
                catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                {
                    bgeo = null;
                }
                foreach (var entry in bbln.Entries)
                {
                    foreach (var geomMorph in entry.GEOMMorphs)
                    {
                        if (bgeo != null)
                        {
                            bgeo.Weight = scales[i] * geomMorph.Amount;
                            geom = geom.LoadBGEOMorph(bgeo, lod, casPart.AdjustedSpecies, (AgeGender)(uint)casPart.CASPartResource.AgeGender.Age, (AgeGender)((uint)casPart.CASPartResource.AgeGender.Gender << 12));
                        }
                        else if (bbln.TGIList != null && bbln.TGIList.Length > geomMorph.TGIIndex && geom.HasVertexIDs)
                        {
                            Destrospean.CmarNYCBorrowed.VPXY vpxy;
                            try
                            {
                                var vpxyEvaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.TGIList[geomMorph.TGIIndex]).ReverseEvaluateResourceKey());
                                vpxy = new Destrospean.CmarNYCBorrowed.VPXY(new BinaryReader(WrapperDealer.GetResource(0, vpxyEvaluated.Package, vpxyEvaluated.ResourceIndexEntry).Stream));
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                                vpxy = null;
                            }
                            if (vpxy != null)
                            {
                                var deltaLinks = vpxy.MeshLinks(lod);
                                var deltas = new List<Destrospean.CmarNYCBorrowed.GEOM>();
                                foreach (var link in deltaLinks)
                                {
                                    Destrospean.CmarNYCBorrowed.GEOM delta;
                                    try
                                    {
                                        var deltaEvaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(link).ReverseEvaluateResourceKey());
                                        delta = ((GeometryResource)WrapperDealer.GetResource(0, deltaEvaluated.Package, deltaEvaluated.ResourceIndexEntry)).ToGEOM();
                                    }
                                    catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                                    {
                                        delta = null;
                                    }
                                    if (delta != null)
                                    {
                                        deltas.Add(delta);
                                    }
                                }
                                geom = geom.LoadGEOMMorph(deltas.ToArray(), scales[i]);
                            }
                        }
                    }
                    foreach (var boneMorph in entry.BoneMorphs)
                    {
                        Destrospean.CmarNYCBorrowed.VPXY vpxy;
                        try
                        {   
                            var vpxyEvaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(bbln.TGIList[boneMorph.TGIIndex]).ReverseEvaluateResourceKey());
                            vpxy = new Destrospean.CmarNYCBorrowed.VPXY(new BinaryReader(WrapperDealer.GetResource(0, vpxyEvaluated.Package, vpxyEvaluated.ResourceIndexEntry).Stream));
                        }
                        catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                        {
                            vpxy = null;
                        }
                        if (vpxy == null)
                        {
                            continue;
                        }
                        foreach (var link in vpxy.AllLinks)
                        {
                            BOND bond;
                            try
                            {   
                                var bondEvaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceUtils.ResourceKey(link).ReverseEvaluateResourceKey());
                                bond = new BOND(new BinaryReader(WrapperDealer.GetResource(0, bondEvaluated.Package, bondEvaluated.ResourceIndexEntry).Stream));
                            }
                            catch (ResourceUtils.ResourceIndexEntryNotFoundException)
                            {
                                bond = null;
                            }
                            if (bond != null)
                            {
                                bond.Weight = scales[i] * boneMorph.Amount;
                                geom = geom.LoadBONDMorph(bond, casPart.CurrentRig);
                            }
                        }
                    }
                }
            }
            List<Vector3> colors = new List<Vector3>(),
            normals = new List<Vector3>(),
            vertices = new List<Vector3>();
            var faces = new List<Tuple<int, int, int>>();
            var textureCoordinates = new List<Vector2>();
            for (var i = 0; i < geom.FaceCount; i++)
            {
                var indices = geom.GetFaceIndices(i);
                faces.Add(new Tuple<int, int, int>(indices[0], indices[1], indices[2]));
            }
            for (var i = 0; i < geom.VertexCount; i++)
            {
                colors.Add(Vector3.One);
                float[] normal = geom.GetNormal(i),
                position = geom.GetPosition(i);
                normals.Add(new Vector3(normal[0], normal[1], normal[2]));
                vertices.Add(new Vector3(position[0], position[1], position[2]));
                for (var j = 0; j < geom.UVCount; j++)
                {
                    var uv = geom.GetUV(i, j);
                    textureCoordinates.Add(new Vector2(uv[0], uv[1]));
                }
            }
            var key = "";
            foreach (var geometryResourceKvp in GeometryResources)
            {
                if (geometryResourceKvp.Value == geometryResource)
                {
                    key = geometryResourceKvp.Key.ReverseEvaluateResourceKey();
                    break;
                }
            }
            Material material;
            if (!Materials.TryGetValue(key, out material))
            {
                var materialColors = new Dictionary<FieldType, Vector3>();
                var materialMaps = new Dictionary<FieldType, string>();
                foreach (var element in ((meshExpImp.ModelBlocks.GEOM)geometryResource.ChunkEntries[0].RCOLBlock).Mtnf.SData)
                {
                    var elementFloat3 = element as ElementFloat3;
                    if (elementFloat3 != null)
                    {
                        materialColors[element.Field] = new Vector3(elementFloat3.Data0, elementFloat3.Data1, elementFloat3.Data2);
                        continue;
                    }
                    var elementTextureRef = element as ElementTextureRef;
                    if (elementTextureRef != null)
                    {
                        materialMaps[element.Field] = element.ParentTGIBlocks[elementTextureRef.Index].ReverseEvaluateResourceKey();
                    }
                }
                Vector3 color;
                string map;
                material = new Material
                    {
#pragma warning disable 0618
                        AmbientColor = materialColors.TryGetValue(FieldType.Ambient, out color) ? color : Vector3.One,
#pragma warning restore 0618
                        AmbientMap = materialMaps.TryGetValue(FieldType.AmbientOcclusionMap, out map) ? map : "",
                        DiffuseColor = materialColors.TryGetValue(FieldType.Diffuse, out color) ? color : Vector3.One,
                        DiffuseMap = materialMaps.TryGetValue(FieldType.DiffuseMap, out map) ? map : "",
                        NormalMap = materialMaps.TryGetValue(FieldType.NormalMap, out map) ? map : "",
                        SpecularColor = materialColors.TryGetValue(FieldType.Specular, out color) ? color : Vector3.One,
                        SpecularMap = materialMaps.TryGetValue(FieldType.SpecularMap, out map) ? map : ""
                    };
                Materials.Add(key, material);
            }
            var currentPreset = casPart.AllPresets[mPresetNotebook.CurrentPage == -1 ? 0 : mPresetNotebook.CurrentPage];
            if (currentPreset.AmbientMap != null)
            {
                LoadTexture(currentPreset.AmbientMap);
            }
            if (currentPreset.SpecularMap != null)
            {
                LoadTexture(currentPreset.SpecularMap);
            }
            mMeshes.Add(new Volume
                {
                    ColorData = colors.ToArray(),
                    Faces = faces,
                    Material = material,
                    Normals = normals.ToArray(),
                    TextureCoordinates = textureCoordinates.ToArray(),
                    TextureID = LoadTexture(key, currentPreset.Texture),
                    Vertices = vertices.ToArray()
                });
        }
    }

    void PrepareGLWidget()
    {
        mGLWidget = new GLWidget
            {
                HeightRequest = Image.HeightRequest,
                WidthRequest = Image.WidthRequest
            };
        mGLWidget.AddEvents((int)(Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask | Gdk.EventMask.ScrollMask));
        mGLWidget.ButtonPressEvent += (o, args) => mMouseButtonsHeld |= (MouseButtonsHeld)Math.Pow(2, args.Event.Button - 1);
        mGLWidget.ButtonReleaseEvent += (o, args) => mMouseButtonsHeld &= (MouseButtonsHeld)(byte.MaxValue - Math.Pow(2, args.Event.Button - 1));
        mGLWidget.ScrollEvent += (o, args) =>
            {
                var delta = MathHelper.DegreesToRadians(1);
                switch ((int)args.Event.Direction)
                {
                    case 0:
                        mFOV -= mFOV - delta > 0 ? delta : 0;
                        break;
                    case 1:
                        mFOV += mFOV + delta <= MathHelper.DegreesToRadians(110) ? delta : 0;
                        break;
                }
            };
        mGLWidget.MotionNotifyEvent += (o, args) =>
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
                        secondsElapsed = (currentTime - mTime) * .001;
                        if (secondsElapsed > 0)
                        {
                            mCamera.MoveSpeed = (float)(distance / secondsElapsed * .0001);
                        }
                    }
                    mMouseX = (float)currentX;
                    mMouseY = (float)currentY;
                    mTime = (float)currentTime;
                }
            };
        mGLWidget.Initialized += (sender, e) => 
            {
                InitProgram();
                mGLInitialized = true;
                NextState = NextStateOptions.UpdateModels;
                GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
            };
        KeyPressEvent += OnKeyPress;
        KeyReleaseEvent += (o, args) => mKeysHeld.RemoveAll(x => x == args.Event.Key);
    }

    void ProcessInput()
    {
        var delta = mLastMousePosition - new Vector2(mMouseX, mMouseY);
        mLastMousePosition += delta;
        if (mMouseButtonsHeld.HasFlag(MouseButtonsHeld.Left))
        {
            if (mKeysHeld.Contains(Gdk.Key.Control_L))
            {
                if (mKeysHeld.Contains(Gdk.Key.Alt_L))
                {
                    mCurrentRotation.X -= delta.Y * mCamera.MouseSensitivity;
                    mCurrentRotation.Z += delta.X * mCamera.MouseSensitivity;
                }
                else
                {
                    mCurrentRotation.Y += delta.X * mCamera.MouseSensitivity;
                }
            }
            else if (mKeysHeld.Contains(Gdk.Key.Alt_L))
            {
                mFOV += delta.Y > 0 && mFOV + delta.Y * mCamera.MouseSensitivity <= MathHelper.DegreesToRadians(110) || delta.Y < 0 && mFOV + delta.Y * mCamera.MouseSensitivity > 0 ? delta.Y * mCamera.MouseSensitivity : 0;
            }
            else
            {
                mCamera.AddTranslation(delta.X, -delta.Y, 0);
            }
        }
        if (mMouseButtonsHeld.HasFlag(MouseButtonsHeld.Middle))
        {
            mCurrentRotation.X -= delta.Y * mCamera.MouseSensitivity;
            mCurrentRotation.Z += delta.X * mCamera.MouseSensitivity;
        }
        if (mMouseButtonsHeld.HasFlag(MouseButtonsHeld.Right))
        {
            if (mKeysHeld.Contains(Gdk.Key.Alt_L))
            {
                mCurrentRotation.X -= delta.Y * mCamera.MouseSensitivity;
                mCurrentRotation.Z += delta.X * mCamera.MouseSensitivity;
            }
            else
            {
                mCurrentRotation.Y += delta.X * mCamera.MouseSensitivity;
            }
        }
        mLastMousePosition = new Vector2(mMouseX, mMouseY);
    }

    public void DeleteTexture(string key)
    {
        int textureID;
        if (TextureIDs.TryGetValue(key, out textureID))
        {
            GL.DeleteTexture(textureID);
            TextureIDs.Remove(key);
        }
    }

    public void DeleteTextures()
    {
        foreach (var textureID in TextureIDs.Values)
        {
            GL.DeleteTexture(textureID);
        }
        TextureIDs.Clear();
    }

    public int LoadTexture(string key)
    {
        Bitmap image;
        return ImageUtils.PreloadedGameImages.TryGetValue(key, out image) || ImageUtils.PreloadedImages.TryGetValue(key, out image) ? LoadTexture(key, image) : -1;
    }

    public int LoadTexture(string key, Bitmap image)
    {
        if (!mGLInitialized)
        {
            return -1;
        }
        int textureID;
        if (!TextureIDs.TryGetValue(key, out textureID))
        {
            GL.GenTextures(1, out textureID);
            TextureIDs.Add(key, textureID);
        }
        GL.BindTexture(TextureTarget.Texture2D, textureID);
        var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
        image.UnlockBits(bitmapData);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        return textureID;
    }

    protected bool OnIdleProcessMain()
    {
        if (ModelsNeedUpdated)
        {
            ModelsNeedUpdated = false;
            NextState = NextStateOptions.UpdateModels;
        }
        if (mGLInitialized)
        {
            OnUpdateFrame();
            OnRenderFrame();
            return true;
        }
        return false;
    }

    [GLib.ConnectBefore]
    protected void OnKeyPress(object sender, Gtk.KeyPressEventArgs args)
    {
        if (!mKeysHeld.Contains(args.Event.Key))
        {
            mKeysHeld.Add(args.Event.Key);
        }
        //args.RetVal = true;
    }

    protected void OnRenderFrame()
    {
        GL.Viewport(0, 0, (int)(mGLWidget.WidthRequest * WidgetUtils.WineScaleDenominator), (int)(mGLWidget.HeightRequest * WidgetUtils.WineScaleDenominator));
        GL.ClearColor(Color.CornflowerBlue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.UseProgram(mShaders[mActiveShader].ProgramID);
        mShaders[mActiveShader].EnableVertexAttribArrays();
        var indexAt = 0;
        foreach (var mesh in mMeshes)
        {
            GL.BindTexture(TextureTarget.Texture2D, mesh.TextureID);
            GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("modelview"), false, ref mesh.ModelViewProjectionMatrix);
            if (mShaders[mActiveShader].GetUniform("light_ambientIntensity") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("light_ambientIntensity"), mLights[0].AmbientIntensity);
            }
            if (mShaders[mActiveShader].GetUniform("light_color") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("light_color"), ref mLights[0].Color);
            }
            if (mShaders[mActiveShader].GetUniform("light_diffuseIntensity") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("light_diffuseIntensity"), mLights[0].DiffuseIntensity);
            }
            if (mShaders[mActiveShader].GetUniform("light_position") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("light_position"), ref mLights[0].Position);
            }
            for (var i = 0; i < Math.Min(mLights.Count, kMaxLights); i++)
            {
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].ambientIntensity") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].ambientIntensity"), mLights[i].AmbientIntensity);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].color") != -1)
                {
                    GL.Uniform3(mShaders[mActiveShader].GetUniform("lights[" + i + "].color"), ref mLights[i].Color);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].coneAngle") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].coneAngle"), mLights[i].ConeAngle);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].diffuseIntensity") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].diffuseIntensity"), mLights[i].DiffuseIntensity);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].direction") != -1)
                {
                    GL.Uniform3(mShaders[mActiveShader].GetUniform("lights[" + i + "].direction"), ref mLights[i].Direction);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].linearAttenuation") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].linearAttenuation"), mLights[i].LinearAttenuation);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].position") != -1)
                {
                    GL.Uniform3(mShaders[mActiveShader].GetUniform("lights[" + i + "].position"), ref mLights[i].Position);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].quadraticAttenuation") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].quadraticAttenuation"), mLights[i].QuadraticAttenuation);
                }
                if (mShaders[mActiveShader].GetUniform("lights[" + i + "].type") != -1)
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("lights[" + i + "].type"), (int)mLights[i].Type);
                }
            }
            if (mShaders[mActiveShader].GetAttribute("maintexture") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetAttribute("maintexture"), mesh.TextureID);
            }
            if (mShaders[mActiveShader].GetUniform("map_specular") != -1)
            {
                TreeIter iter;
                TreeModel model;
                CASPart.Preset currentPreset = null;
                if (ResourceTreeView.Selection.GetSelected(out model, out iter) && (string)model.GetValue(iter, 0) == "CASP")
                {
                    currentPreset = CASParts[(s3pi.Interfaces.IResourceIndexEntry)model.GetValue(iter, 4)].AllPresets[mPresetNotebook.CurrentPage == -1 ? 0 : mPresetNotebook.CurrentPage];
                }
                if (currentPreset != null && currentPreset.SpecularMap != null)
                {
                    int textureID;
                    if (TextureIDs.TryGetValue(currentPreset.SpecularMap, out textureID))
                    {
                        GL.ActiveTexture(TextureUnit.Texture1);
                        GL.BindTexture(TextureTarget.Texture2D, textureID);
                        GL.Uniform1(mShaders[mActiveShader].GetUniform("map_specular"), 1);
                        GL.Uniform1(mShaders[mActiveShader].GetUniform("hasSpecularMap"), 1);
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }
                }
                else
                {
                    GL.Uniform1(mShaders[mActiveShader].GetUniform("hasSpecularMap"), 0);
                }
            }
            if (mShaders[mActiveShader].GetUniform("material_ambient") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_ambient"), ref mesh.Material.AmbientColor);
            }
            if (mShaders[mActiveShader].GetUniform("material_diffuse") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_diffuse"), ref mesh.Material.DiffuseColor);
            }
            if (mShaders[mActiveShader].GetUniform("material_specExponent") != -1)
            {
                GL.Uniform1(mShaders[mActiveShader].GetUniform("material_specExponent"), mesh.Material.SpecularExponent);
            }
            if (mShaders[mActiveShader].GetUniform("material_specular") != -1)
            {
                GL.Uniform3(mShaders[mActiveShader].GetUniform("material_specular"), ref mesh.Material.SpecularColor);
            }
            if (mShaders[mActiveShader].GetUniform("model") != -1)
            {
                GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("model"), false, ref mesh.ModelMatrix);
            }
            if (mShaders[mActiveShader].GetUniform("view") != -1)
            {
                GL.UniformMatrix4(mShaders[mActiveShader].GetUniform("view"), false, ref mViewMatrix);
            }
            GL.DrawElements(BeginMode.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, indexAt * sizeof(uint));
            indexAt += mesh.IndexCount;
        }
        mShaders[mActiveShader].DisableVertexAttribArrays();
        GL.Flush();
        OpenTK.Graphics.GraphicsContext.CurrentContext.SwapBuffers();
    }

    protected void OnUpdateFrame()
    {
        ProcessInput();
        List<Vector3> colors = new List<Vector3>(),
        normals = new List<Vector3>(),
        vertices = new List<Vector3>();
        var indices = new List<int>();
        var textureCoordinates = new List<Vector2>();
        var vertexCount = 0;
        foreach (var mesh in mMeshes)
        {
            colors.AddRange(mesh.ColorData);
            indices.AddRange(mesh.GetIndices(vertexCount));
            normals.AddRange(mesh.Normals);
            textureCoordinates.AddRange(mesh.TextureCoordinates);
            vertices.AddRange(mesh.Vertices);
            vertexCount += mesh.VertexCount;
        }
        mColorData = colors.ToArray();
        mIndexData = indices.ToArray();
        mNormalData = normals.ToArray();
        mTextureCoordinateData = textureCoordinates.ToArray();
        mVertexData = vertices.ToArray();
        GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vPosition"));
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mVertexData.Length * Vector3.SizeInBytes), mVertexData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);
        if (mShaders[mActiveShader].GetAttribute("vColor") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vColor"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mColorData.Length * Vector3.SizeInBytes), mColorData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
        }
        if (mShaders[mActiveShader].GetAttribute("texcoord") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("texcoord"));
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(mTextureCoordinateData.Length * Vector2.SizeInBytes), mTextureCoordinateData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
        }
        if (mShaders[mActiveShader].GetAttribute("vNormal") != -1)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mShaders[mActiveShader].GetBuffer("vNormal"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mNormalData.Length * Vector3.SizeInBytes), mNormalData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(mShaders[mActiveShader].GetAttribute("vNormal"), 3, VertexAttribPointerType.Float, true, 0, 0);
        }
        foreach (var mesh in mMeshes)
        {
            mesh.Rotation = mCurrentRotation;
            mesh.CalculateModelMatrix();
            mesh.ViewProjectionMatrix = mCamera.ViewMatrix * Matrix4.CreatePerspectiveFieldOfView(mFOV, (float)mGLWidget.WidthRequest / mGLWidget.HeightRequest, 1, 40);
            mesh.ModelViewProjectionMatrix = mesh.ModelMatrix * mesh.ViewProjectionMatrix;
        }
        GL.UseProgram(mShaders[mActiveShader].ProgramID);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIBOElements);
        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mIndexData.Length * sizeof(int)), mIndexData, BufferUsageHint.StaticDraw);
        mViewMatrix = mCamera.ViewMatrix;
        System.Threading.Thread.Sleep(1);
    }
}
