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
            Dictionary<string, Bitmap> mDisplayableTextures;

            public override CASPart CASPart
            {
                get
                {
                    return Preset.CASPart;
                }
            }

            public Dictionary<string, Bitmap> DisplayableTextures
            {
                get
                {
                    if (mDisplayableTextures == null)
                    {
                        mDisplayableTextures = new Dictionary<string, Bitmap>();
                        uint[] maskArray = null; 
                        Bitmap multiplier = null,
                        overlay = null;
                        foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                        {
                            var value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                            switch (propertyXmlNodeKvp.Key.ToLower())
                            {
                                case "mask":
                                    maskArray = TextureUtils.GetImageARGBArray(ParentPackage, value, null);
                                    break;
                                case "multiplier":
                                    multiplier = TextureUtils.GetImage(ParentPackage, value, null);
                                    break;
                                case "overlay":
                                    overlay = TextureUtils.GetImage(ParentPackage, value, null);
                                    break;
                            }
                        }
                        if (multiplier != null)
                        {
                            mDisplayableTextures.Add("main", (Bitmap)TextureUtils.DisplayableTexture(multiplier, maskArray, Patterns.ConvertAll(new Converter<Pattern, object>(x => x.DisplayablePattern)), false));
                        }
                        if (overlay != null)
                        {
                            mDisplayableTextures.Add("overlay", (Bitmap)TextureUtils.DisplayableTexture(overlay, maskArray, Patterns.ConvertAll(new Converter<Pattern, object>(x => x.DisplayablePattern)), true));
                        }
                    }
                    return mDisplayableTextures;
                }
                set
                {
                    mDisplayableTextures = value;
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

            public Complate(Preset preset, XmlNode complateXmlNode) : base()
            {
                Preset = preset;
                var evaluated = ResourceUtils.EvaluateResourceKey(ParentPackage, complateXmlNode);
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
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

        public abstract class AComplate
        {
            protected readonly Dictionary<string, string> mPropertiesTyped;

            protected readonly Dictionary<string, XmlNode> mPropertiesXmlNodes;

            protected readonly XmlDocument mXmlDocument;

            public abstract CASPart CASPart
            {
                get;
            }

            public abstract IPackage ParentPackage
            {
                get;
            }

            public virtual Dictionary<string, string> PropertiesTyped
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

            public AComplate()
            {
                mXmlDocument = new XmlDocument();
                mPropertiesXmlNodes = new Dictionary<string, XmlNode>();
                mPropertiesTyped = new Dictionary<string, string>();
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
            object mDisplayablePattern;

            public override CASPart CASPart
            {
                get
                {
                    return Preset.CASPart;
                }
            }

            public object DisplayablePattern
            {
                get
                {
                    if (mDisplayablePattern == null)
                    {
                        switch (PatternInfo.Type)
                        {
                            case PatternType.Colored:
                                mDisplayablePattern = TextureUtils.DisplayableRGBPattern(ParentPackage, PatternInfo);
                                break;
                            case PatternType.HSV:
                                mDisplayablePattern = TextureUtils.DisplayableHSVPattern(ParentPackage, PatternInfo);
                                break;
                            case PatternType.Solid:
                                mDisplayablePattern = PatternInfo.SolidColor;
                                break;
                        }
                    }
                    return mDisplayablePattern;
                }
                private set
                {
                    mDisplayablePattern = value;
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

            public TextureUtils.PatternInfo PatternInfo
            {
                get;
                private set;
            }

            public readonly Preset Preset;

            public Pattern(Preset preset, XmlNode patternXmlNode) : base()
            {
                PatternInfo = new TextureUtils.PatternInfo
                    {
                        Name = patternXmlNode.Attributes["name"].Value
                    };
                Name = patternXmlNode.Attributes["variable"].Value;
                Preset = preset;
                var evaluated = ResourceUtils.EvaluateResourceKey(ParentPackage, patternXmlNode);
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
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

            public void RefreshPatternInfo(bool complateConstructed = true)
            {
                var colors = new List<float[]>();
                string rgbMask = null;
                float[] solidColor = null;
                foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                {
                    var value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                    if (propertyXmlNodeKvp.Key.ToLower().Contains("color"))
                    {
                        var color = new List<string>(value.Substring(0, value.LastIndexOf(',')).Split(',')).ConvertAll(new Converter<string, float>(float.Parse)).ToArray();
                        if (PatternInfo.Name.Contains("solidColor") || PatternInfo.Name.Contains("Flat Color"))
                        {
                            solidColor = color;
                        }
                        else
                        {
                            colors.Add(color);
                        }
                    }
                    else if (propertyXmlNodeKvp.Key.ToLower() == "rgbmask")
                    {
                        rgbMask = value;
                    }
                }
                PatternInfo = new TextureUtils.PatternInfo
                    {
                        Name = PatternInfo.Name,
                        RGBColors = colors.ToArray(),
                        RGBMask = rgbMask,
                        SolidColor = solidColor
                    };
                if (complateConstructed)
                {
                    Preset.ClearDisplayableTextures();
                }
                DisplayablePattern = null;
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

            public Bitmap DisplayableMainTexture
            {
                get
                {
                    Bitmap texture;
                    return DisplayableTextures.TryGetValue("main", out texture) ? texture : null;
                }
            }

            public Bitmap DisplayableOverlayTexture
            {
                get
                {
                    Bitmap texture;
                    return DisplayableTextures.TryGetValue("overlay", out texture) ? texture : null;
                }
            }

            public Dictionary<string, Bitmap> DisplayableTextures
            {
                get
                {
                    return mComplate.DisplayableTextures;
                }
            }

            public Bitmap[] DisplayableTexturesArray
            {
                get
                {
                    return new List<Bitmap>(mComplate.DisplayableTextures.Values).ToArray();
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

            public override Dictionary<string, string> PropertiesTyped
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

            public void ClearDisplayableTextures()
            {
                mComplate.DisplayableTextures = null;
            } 

            public override string GetValue(string propertyName)
            {
                return mComplate.GetValue(propertyName);
            }

            public override void SetValue(string propertyName, string newValue, Action beforeRerender = null)
            {
                mComplate.SetValue(propertyName, newValue, beforeRerender ?? ClearDisplayableTextures);
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
            var vpxyResourceIndexEntry = ResourceUtils.GetResourceIndexEntry(ParentPackage, CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
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
                        var geometryResourceIndexEntry = ResourceUtils.GetResourceIndexEntry(ParentPackage, entry00.ParentTGIBlocks[tgiIndex]);
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
