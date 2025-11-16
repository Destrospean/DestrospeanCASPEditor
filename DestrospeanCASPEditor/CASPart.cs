using System;
using System.Collections.Generic;
using System.Drawing;
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
        public readonly CASPartResource.CASPartResource CASPartResource;

        public readonly Dictionary<int, List<GeometryResource>> LODs = new Dictionary<int, List<GeometryResource>>();

        public readonly IPackage ParentPackage;

        public readonly List<Preset> Presets;

        public readonly IResourceIndexEntry ResourceIndexEntry;

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry, Dictionary<IResourceIndexEntry, GeometryResource> geometryResources, Dictionary<IResourceIndexEntry, GenericRCOLResource> vpxyResources)
        {
            CASPartResource = (CASPartResource.CASPartResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            ParentPackage = package;
            Presets = CASPartResource.Presets.ConvertAll(new Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
            ResourceIndexEntry = resourceIndexEntry;
            LoadLODs(geometryResources, vpxyResources);
        }

        class Complate : AComplate
        {
            Bitmap mTexture;

            public override CASPart CASPart
            {
                get
                {
                    return Preset.CASPart;
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

            public Bitmap Texture
            {
                get
                {
                    if (mTexture == null)
                    {
                        uint[] maskArray = null,
                        overlayArray = null; 
                        Bitmap multiplier = null;
                        foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                        {
                            var value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                            switch (propertyXmlNodeKvp.Key.ToLower())
                            {
                                case "mask":
                                    maskArray = ParentPackage.GetTextureARGBArray(value);
                                    break;
                                case "multiplier":
                                    multiplier = ParentPackage.GetTexture(value);
                                    break;
                                case "overlay":
                                    overlayArray = ParentPackage.GetTextureARGBArray(value);
                                    break;
                            }
                        }
                        if (multiplier != null)
                        {
                            mTexture = multiplier.GetWithPatternsApplied(maskArray, Patterns.ConvertAll(new Converter<Pattern, object>(x => x.PatternImage)), false);
                            if (overlayArray != null)
                            {
                                mTexture = mTexture.GetWithPatternsApplied(overlayArray, Patterns.ConvertAll(new Converter<Pattern, object>(x => x.PatternImage)), true);
                            }
                        }
                    }
                    return mTexture;
                }
                set
                {
                    mTexture = value;
                }
            }

            public Complate(Preset preset, XmlNode complateXmlNode) : base()
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

        class StringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return string.Compare(a, b);
            }
        }

        public abstract class AComplate
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

            public AComplate()
            {
                mXmlDocument = new XmlDocument();
                mPropertiesXmlNodes = new SortedDictionary<string, XmlNode>(new StringComparer());
                mPropertiesTyped = new SortedDictionary<string, string>(new StringComparer());
            }

            public virtual string GetValue(string propertyName)
            {
                return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public virtual void SetValue(string propertyName, string newValue, Action beforeRerender = null)
            {
                mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
                if (beforeRerender != null)
                {
                    beforeRerender();
                }
                MainWindow.Singleton.NextState = NextStateOptions.UnsavedChangesToRerender;
            }
        }

        public class Pattern : AComplate
        {
            object mPatternImage;

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
                PatternInfo = new PatternInfo
                    {
                        Name = patternXmlNode.Attributes["name"].Value
                    };
                Name = patternXmlNode.Attributes["variable"].Value;
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

            public void RefreshPatternInfo(bool clearPresetTexture = true)
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
                        var color = new List<string>(value.Substring(0, value.LastIndexOf(',')).Split(',')).ConvertAll(new Converter<string, float>(float.Parse)).ToArray();
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
                        var color = new List<string>(value.Split(',')).ConvertAll(new Converter<string, float>(float.Parse)).ToArray();
                        if (key.EndsWith("bg"))
                        {
                            hsvShiftBackground = color;
                        }
                        else
                        {
                            hsvShift.Add(color);
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
                if (clearPresetTexture)
                {
                    Preset.ClearTexture();
                }
                PatternImage = null;
            }

            public override void SetValue(string propertyName, string newValue, Action beforeRerender = null)
            {
                base.SetValue(propertyName, newValue, beforeRerender ?? (() => RefreshPatternInfo()));
            }
        }

        public class Preset : AComplate
        {
            readonly CASPart mCASPart;

            readonly Complate mComplate;

            protected new readonly XmlDocument mXmlDocument;

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
                    return mComplate.PatternNames;
                }
            }

            public List<Pattern> Patterns
            {
                get
                {
                    return mComplate.Patterns;
                }
            }

            public override IDictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mComplate.PropertiesTyped;
                }
            }

            public override string[] PropertyNames
            {
                get
                {
                    return mComplate.PropertyNames;
                }
            }

            public Bitmap Texture
            {
                get
                {
                    return mComplate.Texture;
                }
            }

            public StringReader XmlFile
            {
                get
                {
                    return new StringReader(mXmlDocument.InnerXml);
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
                mComplate = new Complate(this, mXmlDocument.SelectSingleNode("preset").SelectSingleNode("complate"));
            }

            public void ClearTexture()
            {
                mComplate.Texture = null;
            } 

            public override string GetValue(string propertyName)
            {
                return mComplate.GetValue(propertyName);
            }

            public override void SetValue(string propertyName, string newValue, Action beforeRerender = null)
            {
                mComplate.SetValue(propertyName, newValue, beforeRerender ?? ClearTexture);
            }
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

        public void SavePreset(int index)
        {
            CASPartResource.Presets[index].XmlFile = Presets[index].XmlFile;
        }

        public void SavePresets()
        {
            AdjustPresetCount();
            for (var i = 0; i < CASPartResource.Presets.Count; i++)
            {
                SavePreset(i);
            }
        }
    }
}
