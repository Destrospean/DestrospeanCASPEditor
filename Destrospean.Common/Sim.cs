using System;
using System.Collections.Generic;
using System.IO;
using Destrospean.CmarNYCBorrowed;
using Destrospean.Common.Abstractions;
using Destrospean.Common.OpenGL;
using Destrospean.S3PIExtensions;
using s3pi.GenericRCOLResource;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;

namespace Destrospean.Common
{
    public class Sim
    {
        readonly Dictionary<CASPartResource.ClothingType, CASPart> mCASParts = new Dictionary<CASPartResource.ClothingType, CASPart>();

        public Dictionary<CASPartResource.ClothingType, CASPart> CASParts
        {
            get
            {
                var casParts = new Dictionary<CASPartResource.ClothingType, CASPart>();
                foreach (var casPartKvp in mCASParts)
                {
                    casParts.Add(casPartKvp.Key, CurrentCASPart != null && casPartKvp.Key == CurrentCASPart.CASPartResource.Clothing ? CurrentCASPart : casPartKvp.Value);
                }
                return casParts;
            }
        }

        public CASPart CurrentCASPart = null;

        public Rig CurrentRig
        {
            get
            {
                return CurrentCASPart.CurrentRig;
            }
        }

        public float Fat = 0,
        Fit = 0,
        Special = 0,
        Thin = 0;

        public readonly Dictionary<string, PreloadedLODMorphed> PreloadedLODsMorphed = new Dictionary<string, PreloadedLODMorphed>(StringComparer.InvariantCultureIgnoreCase);

        public struct PreloadedLODMorphed
        {
            public BBLN BBLN;

            public GEOM[] GEOMs;

            public PreloadedLODMorphed(BBLN bbln, GEOM[] geoms)
            {
                BBLN = bbln;
                GEOMs = geoms;
            }
        }

        public Sim()
        {
            foreach (CASPartResource.ClothingType clothingType in Enum.GetValues(typeof(CASPartResource.ClothingType)))
            {
                mCASParts[clothingType] = null;
            }
        }

        void LoadGEOMs(CASPart casPart, int presetIndex, int lodIndex, Func<string, System.Drawing.Bitmap, int> loadTextureCallback)
        {
            var preloadedData = PreloadedData.Singleton;
            if (!CASParts.ContainsValue(casPart) || casPart.LODs.Count == 0)
            {
                return;
            }
            var lod = new List<int>(casPart.LODs.Keys)[lodIndex];
            foreach (var geometryResource in new List<List<GEOM>>(casPart.LODs.Values)[lodIndex])
            {
                var geom = geometryResource;
                byte[] bblnIndices =
                    {
                        casPart.CASPartResource.BlendInfoFatIndex,
                        casPart.CASPartResource.BlendInfoFitIndex,
                        casPart.CASPartResource.BlendInfoThinIndex,
                        casPart.CASPartResource.BlendInfoSpecialIndex
                    };
                float[] weights =
                    {
                        Fat,
                        Fit,
                        Thin,
                        Special
                    };
                for (var i = 0; i < bblnIndices.Length; i++)
                {
                    BBLN bbln;
                    string bblnKey;
                    EvaluatedResourceKey evaluated;
                    PreloadedLODMorphed preloadedLODMorphed;
                    try
                    {
                        bblnKey = casPart.CASPartResource.TGIBlocks[bblnIndices[i]].ReverseEvaluateResourceKey();
                        if (PreloadedLODsMorphed.TryGetValue(bblnKey, out preloadedLODMorphed))
                        {
                            bbln = preloadedLODMorphed.BBLN;
                        }
                        else
                        {
                            evaluated = casPart.ParentPackage.EvaluateResourceKey(bblnKey);
                            bbln = new BBLN(new BinaryReader(((s3pi.Interfaces.APackage)evaluated.Package).GetResource(evaluated.ResourceIndexEntry)));
                        }
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                        continue;
                    }
                    BGEO bgeo = null;
                    try
                    {
                        if (!PreloadedLODsMorphed.TryGetValue(bblnKey, out preloadedLODMorphed))
                        {
                            evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceKey(bbln.BGEOTGI.Type, bbln.BGEOTGI.Group, bbln.BGEOTGI.Instance).ReverseEvaluateResourceKey());
                            bgeo = new BGEO(new BinaryReader(((s3pi.Interfaces.APackage)evaluated.Package).GetResource(evaluated.ResourceIndexEntry)));
                        }
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                    }
                    foreach (var entry in bbln.Entries)
                    {
                        foreach (var geomMorph in entry.GEOMMorphs)
                        {
                            if (!PreloadedLODsMorphed.TryGetValue(bblnKey, out preloadedLODMorphed) && bgeo != null)
                            {
                                //bgeo.Weight = weights[i] * geomMorph.Amount;
                                //geom = geom.LoadBGEOMorph(bgeo, lod, casPart.AdjustedSpecies, (AgeGender)(uint)casPart.CASPartResource.AgeGender.Age, (AgeGender)((uint)casPart.CASPartResource.AgeGender.Gender << 12));
                                preloadedLODMorphed = new PreloadedLODMorphed(bbln, new GEOM[]
                                    {
                                        new GEOM(geom, bgeo, 0, lod)
                                    });
                                PreloadedLODsMorphed.Add(bblnKey, preloadedLODMorphed);
                            }
                            else if (!PreloadedLODsMorphed.TryGetValue(bblnKey, out preloadedLODMorphed) && bbln.TGIList != null && bbln.TGIList.Length > geomMorph.TGIIndex && geom.HasVertexIDs)
                            {
                                try
                                {
                                    var geoms = new List<GEOM>();
                                    foreach (var link in new CmarNYCBorrowed.VPXY(new BinaryReader(preloadedData.VPXYs[new ResourceKey(bbln.TGIList[geomMorph.TGIIndex].Type, bbln.TGIList[geomMorph.TGIIndex].Group, bbln.TGIList[geomMorph.TGIIndex].Instance).ReverseEvaluateResourceKey()].Stream)).GetMeshLinks(lod))
                                    {
                                        try
                                        {
                                            geoms.Add(preloadedData.GEOMs[new ResourceKey(link.Type, link.Group, link.Instance).ReverseEvaluateResourceKey()]);
                                        }
                                        catch (ResourceIndexEntryNotFoundException)
                                        {
                                        }
                                    }
                                    preloadedLODMorphed = new PreloadedLODMorphed(bbln, geoms.ToArray());
                                    PreloadedLODsMorphed.Add(bblnKey, preloadedLODMorphed);
                                }
                                catch (ResourceIndexEntryNotFoundException)
                                {
                                }
                            }
                            geom = geom.LoadGEOMMorph(preloadedLODMorphed.GEOMs, weights[i]);
                        }
                        foreach (var boneMorph in entry.BoneMorphs)
                        {
                            try
                            {   
                                foreach (var link in new CmarNYCBorrowed.VPXY(new BinaryReader(preloadedData.VPXYs[new ResourceKey(bbln.TGIList[boneMorph.TGIIndex].Type, bbln.TGIList[boneMorph.TGIIndex].Group, bbln.TGIList[boneMorph.TGIIndex].Instance).ReverseEvaluateResourceKey()].Stream)).AllLinks)
                                {
                                    try
                                    {   
                                        evaluated = casPart.ParentPackage.EvaluateResourceKey(new ResourceKey(link.Type, link.Group, link.Instance).ReverseEvaluateResourceKey());
                                        var bond = new BOND(new BinaryReader(((s3pi.Interfaces.APackage)evaluated.Package).GetResource(evaluated.ResourceIndexEntry)));
                                        bond.Weight = weights[i] * boneMorph.Amount;
                                        geom = geom.LoadBONDMorph(bond, CurrentRig);
                                    }
                                    catch (ResourceIndexEntryNotFoundException)
                                    {
                                    }
                                }
                            }
                            catch (ResourceIndexEntryNotFoundException)
                            {
                            }
                        }
                    }
                }
                List<Vector3> colors = new List<Vector3>(),
                normals = new List<Vector3>(),
                vertices = new List<Vector3>();
                var faces = new List<int[]>();
                var textureCoordinates = new List<Vector2>();
                for (var i = 0; i < geom.FaceCount; i++)
                {
                    var indices = geom.GetFaceIndices(i);
                    faces.Add(new int[]
                        {
                            indices[0],
                            indices[1],
                            indices[2]
                        });
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
                foreach (var geometryResourceKvp in preloadedData.GEOMs)
                {
                    if (geometryResourceKvp.Value == geometryResource)
                    {
                        key = geometryResourceKvp.Key;
                        break;
                    }
                }
                Material material;
                if (!preloadedData.Materials.TryGetValue(key, out material))
                {
                    var materialColors = new Dictionary<FieldType, Vector3>();
                    var materialMaps = new Dictionary<FieldType, string>();
                    foreach (var field in geom.Shader.GetFields())
                    {
                        int valueType;
                        var element = geom.Shader.GetFieldValue(field, out valueType);
                        if (valueType == 1 && (element.Length == 3 || element.Length == 4))
                        {
                            materialColors[(FieldType)field] = new Vector3((float)element[0], (float)element[1], (float)element[2]);
                        }
                        else if (valueType == 4)
                        {
                            materialMaps[(FieldType)field] = new ResourceKey(geom.TGIList[(uint)element[0]].Type, geom.TGIList[(uint)element[0]].Group, geom.TGIList[(uint)element[0]].Instance).ReverseEvaluateResourceKey();
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
                    preloadedData.Materials.Add(key, material);
                }
                var currentPreset = casPart == CurrentCASPart ? casPart.AllPresets[presetIndex] : casPart.AllPresets[0];
                preloadedData.Meshes.Add(new Volume
                    {
                        ColorData = colors.ToArray(),
                        Faces = faces,
                        Material = material,
                        Normals = normals.ToArray(),
                        TextureCoordinates = textureCoordinates.ToArray(),
                        AmbientMapID = loadTextureCallback(currentPreset.AmbientMap == null ? material.AmbientMap : currentPreset.AmbientMap, null),
                        MainTextureID = loadTextureCallback(key, currentPreset.Texture),
                        SpecularMapID = loadTextureCallback(currentPreset.SpecularMap == null ? material.SpecularMap : currentPreset.SpecularMap, null),
                        Vertices = vertices.ToArray()
                    });
            }
        }

        public void LoadGEOMs(int presetIndex, int lodIndex, Func<string, System.Drawing.Bitmap, int> loadTextureCallback)
        {
            Array.ForEach(new List<CASPart>(CASParts.Values).FindAll(x => x != null).ToArray(), x => LoadGEOMs(x, presetIndex, lodIndex, loadTextureCallback));
        }
    }
}
