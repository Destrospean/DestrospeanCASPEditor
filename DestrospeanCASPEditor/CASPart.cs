using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using Destrospean.CmarNYCBorrowed;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

namespace Destrospean.DestrospeanCASPEditor
{
    public class CASPart
    {
        public List<Preset> AllPresets
        {
            get
            {
                var allPresets = new List<Preset>(Presets);
                if (DefaultPreset != null)
                {
                    allPresets.Insert(0, DefaultPreset);
                }
                return allPresets;
            }
        }
            
        public readonly CASPartResource.CASPartResource CASPartResource;

        public readonly Preset DefaultPreset;

        public readonly IResourceIndexEntry DefaultPresetResourceIndexEntry;

        public readonly Dictionary<int, List<GeometryResource>> LODs = new Dictionary<int, List<GeometryResource>>();

        public readonly IPackage ParentPackage;

        public readonly List<Preset> Presets;

        public readonly IResourceIndexEntry ResourceIndexEntry;

        public abstract class Complate
        {
            protected readonly IDictionary<string, string> mPropertiesTyped;

            protected readonly IDictionary<string, XmlNode> mPropertiesXmlNodes;

            protected readonly XmlDocument mXmlDocument;

            public abstract CASPart CASPart
            {
                get;
            }

            public abstract IPackage ParentPackage
            {
                get;
            }

            public virtual IDictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mPropertiesTyped;
                }
            }

            public virtual string[] PropertyNames
            {
                get
                {
                    return new List<string>(mPropertiesXmlNodes.Keys).ToArray();
                }
            }

            public string this[string propertyName]
            {
                get
                {
                    return GetValue(propertyName);
                }
                set
                {
                    SetValue(propertyName, value);
                }
            }

            protected class PropertyNameComparer : IComparer<string>
            {
                public int Compare(string a, string b)
                {
                    string aCopy = a,
                    bCopy = b;
                    while (aCopy.Length < bCopy.Length)
                    {
                        aCopy += " ";
                    }
                    while (aCopy.Length > bCopy.Length)
                    {
                        bCopy += " ";
                    }
                    for (var i = 0; i < aCopy.Length; i++)
                    {
                        if (aCopy[i] != bCopy[i] && aCopy.Substring(0, i) == bCopy.Substring(0, i))
                        {
                            bool aCharIsNum = '0' <= aCopy[i] && aCopy[i] <= '9',
                            bCharIsNum = '0' <= bCopy[i] && bCopy[i] <= '9';
                            if (aCharIsNum && !bCharIsNum)
                            {
                                return 1;
                            }
                            if (!aCharIsNum && bCharIsNum)
                            {
                                return -1;
                            }
                        }
                    }
                    return string.Compare(a, b);
                }
            }

            public Complate()
            {
                mXmlDocument = new XmlDocument();
                mPropertiesXmlNodes = new SortedDictionary<string, XmlNode>(new PropertyNameComparer());
                mPropertiesTyped = new SortedDictionary<string, string>(new PropertyNameComparer());
            }

            public static float[] GetColor(string fromString)
            {
                return new List<string>(fromString.Split(',')).ConvertAll(new Converter<string, float>(float.Parse)).ToArray();
            }

            public virtual string GetValue(string propertyName)
            {
                return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public virtual void SetValue(string propertyName, string newValue, Action beforeMarkUnsaved = null)
            {
                mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
                if (beforeMarkUnsaved != null)
                {
                    beforeMarkUnsaved();
                }
                MainWindow.Singleton.NextState = NextStateOptions.UnsavedChanges;
            }
        }

        public class Pattern : Complate
        {
            protected object mPatternImage;

            public override CASPart CASPart
            {
                get
                {
                    return Preset.CASPart;
                }
            }

            public readonly string Name;

            public override IPackage ParentPackage
            {
                get
                {
                    return Preset.ParentPackage;
                }
            }

            public object PatternImage
            {
                get
                {
                    if (mPatternImage == null)
                    {
                        switch (PatternInfo.Type)
                        {
                            case PatternType.Colored:
                                mPatternImage = ParentPackage.GetRGBPatternImage(PatternInfo);
                                break;
                            case PatternType.HSV:
                                mPatternImage = ParentPackage.GetHSVPatternImage(PatternInfo);
                                break;
                            case PatternType.Solid:
                                mPatternImage = PatternInfo.SolidColor;
                                break;
                        }
                    }
                    return mPatternImage;
                }
                private set
                {
                    mPatternImage = value;
                }
            }

            public PatternInfo PatternInfo
            {
                get;
                private set;
            }

            public readonly Preset Preset;

            public Pattern(Preset preset, XmlNode patternXmlNode) : base()
            {
                Name = patternXmlNode.Attributes["variable"].Value;
                PatternInfo = new PatternInfo
                    {
                        Name = patternXmlNode.Attributes["name"].Value
                    };
                Preset = preset;
                var evaluated = ParentPackage.EvaluateResourceKey(patternXmlNode);
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
                foreach (var child in patternXmlNode.ChildNodes)
                {
                    var childNode = (XmlNode)child;
                    if (childNode.Name == "value")
                    {
                        mPropertiesXmlNodes.Add(childNode.Attributes["key"].Value, childNode);
                    }
                }
                foreach (var child in mXmlDocument.SelectSingleNode("complate").ChildNodes)
                {
                    var childNode = (XmlNode)child;
                    if (childNode.Name == "variables")
                    {
                        foreach (var grandchild in childNode.ChildNodes)
                        {
                            var grandchildNode = (XmlNode)grandchild;
                            if (grandchildNode.Name == "param")
                            {
                                PropertiesTyped.Add(grandchildNode.Attributes["name"].Value, grandchildNode.Attributes["type"].Value);
                            }
                        }
                    }
                }
                RefreshPatternInfo(false);
            }

            public void RefreshPatternInfo(bool regeneratePresetTexture = true)
            {
                string background = null,
                rgbMask = null;
                float baseHueBackground = float.MinValue,
                baseSaturationBackground = float.MinValue,
                baseValueBackground = float.MinValue,
                hueBackground = float.MinValue,
                saturationBackground = float.MinValue,
                valueBackground = float.MinValue;
                List<float> baseHues = new List<float>(),
                baseSaturations = new List<float>(),
                baseValues = new List<float>(),
                hues = new List<float>(),
                saturations = new List<float>(),
                values = new List<float>();
                var channels = new List<string>();
                var channelsEnabled = new List<bool>();
                List<float[]> baseHSVColors = new List<float[]>(),
                hsvColors = new List<float[]>(),
                hsvShift = new List<float[]>(),
                rgbColors = new List<float[]>();
                float[] hsvShiftBackground = null,
                solidColor = null;
                foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                {
                    string key = propertyXmlNodeKvp.Key.ToLower(),
                    value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                    if (key.StartsWith("channel"))
                    {
                        if (key.EndsWith("enabled"))
                        {
                            channelsEnabled.Add(bool.Parse(value));
                        }
                        else
                        {
                            channels.Add(value);
                        }
                    }
                    else if (key.StartsWith("color"))
                    {
                        var color = GetColor(value);
                        color = new float[]
                            {
                                color[0],
                                color[1],
                                color[2]
                            };
                        if (PatternInfo.Name.Contains("solidColor") || PatternInfo.Name.Contains("Flat Color"))
                        {
                            solidColor = color;
                        }
                        else
                        {
                            rgbColors.Add(color);
                        }
                    }
                    else if (key.StartsWith("base h"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            baseHueBackground = float.Parse(value);
                        }
                        else
                        {
                            baseHues.Add(float.Parse(value));
                        }
                    }
                    else if (key.StartsWith("base s"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            baseSaturationBackground = float.Parse(value);
                        }
                        else
                        {
                            baseSaturations.Add(float.Parse(value));
                        }
                    }
                    else if (key.StartsWith("base v"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            baseValueBackground = float.Parse(value);
                        }
                        else
                        {
                            baseValues.Add(float.Parse(value));
                        }
                    }
                    else if (key.StartsWith("h "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            hueBackground = float.Parse(value);
                        }
                        else
                        {
                            hues.Add(float.Parse(value));
                        }
                    }
                    else if (key.StartsWith("hsvshift"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            hsvShiftBackground = GetColor(value);
                        }
                        else
                        {
                            hsvShift.Add(GetColor(value));
                        }
                    }
                    else if (key.StartsWith("s "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            saturationBackground = float.Parse(value);
                        }
                        else
                        {
                            saturations.Add(float.Parse(value));
                        }
                    }
                    else if (key.StartsWith("v "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            valueBackground = float.Parse(value);
                        }
                        else
                        {
                            values.Add(float.Parse(value));
                        }
                    }
                    else
                    {
                        switch (key)
                        {
                            case "background image":
                                background = value;
                                break;
                            case "rgbmask":
                                rgbMask = value;
                                break;
                        }
                    }
                }
                for (int i = 0; i < baseHues.Count && baseHues.Count == baseSaturations.Count && baseHues.Count == baseValues.Count; i++)
                {
                    baseHSVColors.Add(new float[]
                        {
                            baseHues[i],
                            baseSaturations[i],
                            baseValues[i]
                        });
                }
                for (int i = 0; i < hues.Count && hues.Count == saturations.Count && hues.Count == values.Count; i++)
                {
                    hsvColors.Add(new float[]
                        {
                            hues[i],
                            saturations[i],
                            values[i]
                        });
                }
                PatternInfo = new PatternInfo
                    {
                        Background = background,
                        Channels = channels.Count == 0 ? null : channels.ToArray(),
                        ChannelEnabled = channelsEnabled.Count == 0 ? null : channelsEnabled.ToArray(),
                        HSV = hsvColors.Count == 0 ? null : hsvColors.ToArray(),
                        HSVBase = baseHSVColors.Count == 0 ? null : baseHSVColors.ToArray(),
                        HSVBaseBG = baseHueBackground == float.MinValue || baseSaturationBackground == float.MinValue || baseValueBackground == float.MinValue ? null : new float[]
                            {
                                baseHueBackground,
                                baseSaturationBackground,
                                baseValueBackground
                            },
                        HSVBG = hueBackground == float.MinValue || saturationBackground == float.MinValue || valueBackground == float.MinValue ? null : new float[]
                            {
                                hueBackground,
                                saturationBackground,
                                valueBackground
                            },
                        HSVShift = hsvShift.Count == 0 ? null : hsvShift.ToArray(),
                        HSVShiftBG = hsvShiftBackground,
                        Name = PatternInfo.Name,
                        RGBColors = rgbColors.ToArray(),
                        RGBMask = rgbMask,
                        SolidColor = solidColor
                    };
                if (regeneratePresetTexture)
                {
                    Preset.RegenerateTexture();
                }
                PatternImage = null;
            }

            public override void SetValue(string propertyName, string newValue, Action beforeMarkUnsaved = null)
            {
                base.SetValue(propertyName, newValue, beforeMarkUnsaved ?? (() => RefreshPatternInfo()));
            }
        }

        public class Preset : Complate
        {
            protected readonly CASPart mCASPart;

            protected readonly PresetInternal mInternal;

            protected new readonly XmlDocument mXmlDocument;

            public string AmbientMap
            {
                get
                {
                    return mInternal.AmbientMap;
                }
            }

            public override CASPart CASPart
            {
                get
                {
                    return mCASPart;
                }
            }

            public override IPackage ParentPackage
            {
                get
                {
                    return CASPart.ParentPackage;
                }
            }

            public string[] PatternNames
            {
                get
                {
                    return mInternal.PatternNames;
                }
            }

            public List<Pattern> Patterns
            {
                get
                {
                    return mInternal.Patterns;
                }
            }

            public override IDictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mInternal.PropertiesTyped;
                }
            }

            public override string[] PropertyNames
            {
                get
                {
                    return mInternal.PropertyNames;
                }
            }

            public string SpecularMap
            {
                get
                {
                    return mInternal.SpecularMap;
                }
            }

            public Bitmap Texture
            {
                get
                {
                    return mInternal.Texture;
                }
            }

            public StringReader XmlFile
            {
                get
                {
                    return new StringReader(mXmlDocument.InnerXml);
                }
            }

            protected class PresetInternal : Complate
            {
                protected Bitmap mTexture;

                public string AmbientMap
                {
                    get;
                    private set;
                }

                public override CASPart CASPart
                {
                    get
                    {
                        return Preset.CASPart;
                    }
                }

                public Bitmap NewTexture
                {
                    get
                    {
                        uint[] controlMapArray = null,
                        maskArray = null;
                        Bitmap diffuseMap = null,
                        multiplier = null,
                        overlay = null;
                        float[] diffuseColor = null,
                        highlightColor = null,
                        rootColor = null,
                        tipColor = null;
                        int height = 1024,
                        width = 1024;
                        string partType = null;
                        foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                        {
                            var value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                            switch (propertyXmlNodeKvp.Key.ToLower())
                            {
                                case "clothing ambient":
                                    AmbientMap = value;
                                    break;
                                case "clothing specular":
                                    SpecularMap = value;
                                    break;
                                case "control map":
                                    controlMapArray = ParentPackage.GetTextureARGBArray(value, width, height);
                                    break;
                                case "diffuse color":
                                    diffuseColor = GetColor(value);
                                    break;
                                case "diffuse map":
                                    diffuseMap = ParentPackage.GetTexture(value, width, height);
                                    break;
                                case "highlight color":
                                    highlightColor = GetColor(value);
                                    break;
                                case "mask":
                                    maskArray = ParentPackage.GetTextureARGBArray(value, width, height);
                                    break;
                                case "multiplier":
                                    multiplier = ParentPackage.GetTexture(value, width, height);
                                    break;
                                case "overlay":
                                    overlay = ParentPackage.GetTexture(value, width, height);
                                    break;
                                case "parttype":
                                    partType = value;
                                    break;
                                case "root color":
                                    rootColor = GetColor(value);
                                    break;
                                case "tip color":
                                    tipColor = GetColor(value);
                                    break;
                            }
                        }
                        if (diffuseMap != null)
                        {
                            if (partType.ToLower() == "hair")
                            {
                                float[][] hairMatrix =
                                    {
                                        new float[]
                                        {
                                            diffuseColor[0],
                                            0,
                                            0,
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            diffuseColor[1],
                                            0,
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            diffuseColor[2],
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            0,
                                            1,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            0,
                                            0,
                                            1
                                        }
                                    };
                                using (Graphics graphics = Graphics.FromImage(diffuseMap))
                                {
                                    var convert = new ColorMatrix(hairMatrix);
                                    var attributes = new ImageAttributes();
                                    attributes.SetColorMatrix(convert, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                                    graphics.DrawImage(diffuseMap, new Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height), 0, 0, diffuseMap.Width, diffuseMap.Height, GraphicsUnit.Pixel, attributes);
                                }
                                if (controlMapArray != null && highlightColor != null && rootColor != null && tipColor != null)
                                {
                                    diffuseMap = diffuseMap.GetWithPatternsApplied(controlMapArray, new List<object>
                                        {
                                            rootColor,
                                            highlightColor,
                                            tipColor
                                        }, false);
                                }
                            }
                        }
                        var patternImages = Patterns.ConvertAll(new Converter<Pattern, object>(x => bool.Parse(GetValue(x.Name + " Enabled")) ? x.PatternImage : null));
                        if (maskArray != null)
                        {
                            if (multiplier != null)
                            {
                                multiplier = multiplier.GetWithPatternsApplied(maskArray, patternImages, false);
                            }
                            if (overlay != null)
                            {
                                overlay = overlay.GetWithPatternsApplied(maskArray, patternImages, true);
                            }
                        }
                        var texture = new Bitmap(width, height);
                        using (Graphics graphics = Graphics.FromImage(texture))
                        {
                            if (diffuseMap != null)
                            {
                                graphics.DrawImage(diffuseMap, 0, 0);
                            }
                            if (multiplier != null)
                            {
                                graphics.DrawImage(multiplier, 0, 0);
                            }
                            if (overlay != null)
                            {
                                graphics.DrawImage(overlay, 0, 0);
                            }
                        }
                        return texture;
                    }
                }

                public override IPackage ParentPackage
                {
                    get
                    {
                        return Preset.ParentPackage;
                    }
                }

                public string[] PatternNames
                {
                    get
                    {
                        return Patterns.ConvertAll(new Converter<Pattern, string>(x => x.Name)).ToArray();
                    }
                }

                public readonly List<Pattern> Patterns;

                public readonly Preset Preset;

                public string SpecularMap
                {
                    get;
                    private set;
                }

                public Bitmap Texture
                {
                    get
                    {
                        if (mTexture == null)
                        {
                            mTexture = NewTexture;
                        }
                        return mTexture;
                    }
                    set
                    {
                        mTexture = value;
                    }
                }

                public PresetInternal(Preset preset, XmlNode complateXmlNode) : base()
                {
                    Preset = preset;
                    var evaluated = ParentPackage.EvaluateResourceKey(complateXmlNode);
                    mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
                    Patterns = new List<Pattern>();
                    foreach (var child in complateXmlNode.ChildNodes)
                    {
                        var childNode = (XmlNode)child;
                        if (childNode.Name == "value")
                        {
                            mPropertiesXmlNodes.Add(childNode.Attributes["key"].Value, childNode);
                        }
                        if (childNode.Name == "pattern")
                        {
                            Patterns.Add(new Pattern(Preset, childNode));
                        }
                    }
                    foreach (var child in mXmlDocument.SelectSingleNode("complate").ChildNodes)
                    {
                        var childNode = (XmlNode)child;
                        if (childNode.Name == "variables")
                        {
                            foreach (var grandchild in childNode.ChildNodes)
                            {
                                var grandchildNode = (XmlNode)grandchild;
                                if (grandchildNode.Name == "param")
                                {
                                    PropertiesTyped.Add(grandchildNode.Attributes["name"].Value, grandchildNode.Attributes["type"].Value);
                                }
                            }
                        }
                    }
                }
            }

            public Preset(CASPart casPart, CASPartResource.CASPartResource.Preset preset) : this(casPart, preset.XmlFile)
            {
            }

            public Preset(CASPart casPart, TextReader xmlFile)
            {
                mCASPart = casPart;
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(xmlFile.ReadToEnd());
                mInternal = new PresetInternal(this, mXmlDocument.SelectSingleNode("preset").SelectSingleNode("complate"));
            }

            public override string GetValue(string propertyName)
            {
                return mInternal.GetValue(propertyName);
            }

            public void RegenerateTexture()
            {
                new System.Threading.Thread(() =>
                    {
                        mInternal.Texture = mInternal.NewTexture;
                        MainWindow.Singleton.ModelsNeedUpdated = true;
                    }).Start();
            }

            public override void SetValue(string propertyName, string newValue, Action beforeMarkUnsaved = null)
            {
                mInternal.SetValue(propertyName, newValue, beforeMarkUnsaved ?? RegenerateTexture);
            }
        }

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry, Dictionary<IResourceIndexEntry, GeometryResource> geometryResources, Dictionary<IResourceIndexEntry, GenericRCOLResource> vpxyResources)
        {
            CASPartResource = (CASPartResource.CASPartResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            ParentPackage = package;
            var defaultPresetResourceIndexEntries = ParentPackage.FindAll(x => x.ResourceType == ResourceUtils.GetResourceType("_XML") && x.ResourceGroup == resourceIndexEntry.ResourceGroup && x.Instance == resourceIndexEntry.Instance);
            if (defaultPresetResourceIndexEntries.Count == 0)
            {
                DefaultPresetResourceIndexEntry = null;
                DefaultPreset = null;
            }
            else
            {
                DefaultPresetResourceIndexEntry = defaultPresetResourceIndexEntries[0];
                DefaultPreset = new Preset(this, new StreamReader(WrapperDealer.GetResource(0, ParentPackage, DefaultPresetResourceIndexEntry).Stream));
            }
            Presets = CASPartResource.Presets.ConvertAll(new Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
            ResourceIndexEntry = resourceIndexEntry;
            LoadLODs(geometryResources, vpxyResources);
        }

        public void AdjustPresetCount()
        {
            while (CASPartResource.Presets.Count < Presets.Count)
            {
                CASPartResource.Presets.Add((CASPartResource.CASPartResource.Preset)CASPartResource.Presets[0].Clone((sender, e) =>
                    {
                    }));
            }
            while (CASPartResource.Presets.Count > Presets.Count)
            {
                CASPartResource.Presets.RemoveAt(0);
            }
        }

        public void LoadLODs(Dictionary<IResourceIndexEntry, GeometryResource> geometryResources, Dictionary<IResourceIndexEntry, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyResourceIndexEntry, out vpxyResource))
            {
                vpxyResources.Add(vpxyResourceIndexEntry, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyResourceIndexEntry];
            }
            foreach (var entry in ((VPXY)vpxyResource.ChunkEntries[0].RCOLBlock).Entries)
            {
                var entry00 = entry as VPXY.Entry00;
                if (entry00 != null)
                {
                    LODs[entry00.EntryID] = new List<GeometryResource>();
                    foreach (var tgiIndex in entry00.TGIIndexes)
                    {
                        var geometryResourceIndexEntry = ParentPackage.GetResourceIndexEntry(entry00.ParentTGIBlocks[tgiIndex]);
                        GeometryResource geometryResource;
                        if (!geometryResources.TryGetValue(geometryResourceIndexEntry, out geometryResource))
                        {
                            geometryResources.Add(geometryResourceIndexEntry, (GeometryResource)WrapperDealer.GetResource(0, ParentPackage, geometryResourceIndexEntry));
                            geometryResource = geometryResources[geometryResourceIndexEntry];
                        }
                        LODs[entry00.EntryID].Add(geometryResource);
                    }
                    continue;
                }
                var entry01 = entry as VPXY.Entry01;
                if (entry01 != null)
                {
                }
            }
        }

        public void SaveDefaultPreset()
        {   
            if (DefaultPreset == null || DefaultPresetResourceIndexEntry == null)
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(AllPresets[0].XmlFile.ReadToEnd())))
            {
                var tempResourceIndexEntry = ParentPackage.AddResource(DefaultPresetResourceIndexEntry, stream, false);
                ParentPackage.ReplaceResource(DefaultPresetResourceIndexEntry, WrapperDealer.GetResource(0, ParentPackage, tempResourceIndexEntry));
                ParentPackage.DeleteResource(tempResourceIndexEntry);
            }
        }

        public void SavePreset(int index)
        {
            CASPartResource.Presets[index].XmlFile = Presets[index].XmlFile;
        }

        public void SavePresets()
        {
            SaveDefaultPreset();
            AdjustPresetCount();
            for (var i = 0; i < CASPartResource.Presets.Count; i++)
            {
                SavePreset(i);
            }
        }
    }
}
