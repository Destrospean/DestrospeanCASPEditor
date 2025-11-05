using System.Collections.Generic;
using System.IO;
using System.Xml;
using meshExpImp.ModelBlocks;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

namespace Destrospean.DestrospeanCASPEditor
{
    public class CASPart
    {
        public readonly CASPartResource.CASPartResource CASPartResource;

        public readonly List<GeometryResource>[] LODs = new List<GeometryResource>[4];

        public readonly IPackage ParentPackage;

        public readonly List<Preset> Presets;

        public readonly IResourceIndexEntry ResourceIndexEntry;

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry, Dictionary<IResourceIndexEntry, GeometryResource> geometryResources, Dictionary<IResourceIndexEntry, GenericRCOLResource> vpxyResources)
        {
            CASPartResource = (CASPartResource.CASPartResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            ParentPackage = package;
            ResourceIndexEntry = resourceIndexEntry;
            Presets = CASPartResource.Presets.ConvertAll(new System.Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
            LoadLODs(geometryResources, vpxyResources);
        }

        public interface IComplate
        {
            IPackage ParentPackage
            {
                get;
            }

            Dictionary<string, string> PropertiesTyped
            {
                get;
            }

            List<string> PropertyNames
            {
                get;
            }
                
            string GetValue(string propertyName);

            void SetValue(string propertyName, string newValue);
        }

        public class Complate : IComplate
        {
            readonly Dictionary<string, string> mPropertiesTyped;

            readonly Dictionary<string, XmlNode> mPropertiesXmlNodes;

            readonly XmlDocument mXmlDocument;

            public IPackage ParentPackage
            {
                get
                {
                    return Preset.ParentPackage;
                }
            }

            public List<string> PatternNames
            {
                get
                {
                    return Patterns.ConvertAll(new System.Converter<Pattern, string>(x => x.Name));
                }
            }

            public readonly List<Pattern> Patterns;

            public readonly Preset Preset;

            public Dictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mPropertiesTyped;
                }
            }

            public List<string> PropertyNames
            {
                get
                {
                    return new List<string>(mPropertiesXmlNodes.Keys);
                }
            }

            public Complate(Preset preset, XmlNode complateXmlNode)
            {
                Preset = preset;
                var evaluated = ResourceUtils.EvaluateResourceKey(ParentPackage, complateXmlNode);
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
                Patterns = new List<Pattern>();
                mPropertiesXmlNodes = new Dictionary<string, XmlNode>();
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
                mPropertiesTyped = new Dictionary<string, string>();
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

            public string GetValue(string propertyName)
            {
                return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public void SetValue(string propertyName, string newValue)
            {
                mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
            }
        }

        public class Pattern : IComplate
        {
            readonly Dictionary<string, string> mPropertiesTyped;

            readonly Dictionary<string, XmlNode> mPropertiesXmlNodes;

            readonly XmlDocument mXmlDocument;

            public readonly string Name;

            public IPackage ParentPackage
            {
                get
                {
                    return Preset.ParentPackage;
                }
            }

            public readonly Preset Preset;

            public Dictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mPropertiesTyped;
                }
            }

            public List<string> PropertyNames
            {
                get
                {
                    return new List<string>(mPropertiesXmlNodes.Keys);
                }
            }

            public Pattern(Preset preset, XmlNode patternXmlNode)
            {
                Preset = preset;
                Name = patternXmlNode.Attributes["variable"].Value;
                var evaluated = ResourceUtils.EvaluateResourceKey(ParentPackage, patternXmlNode);
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
                mPropertiesXmlNodes = new Dictionary<string, XmlNode>();
                foreach (var child in patternXmlNode.ChildNodes)
                {
                    var childNode = (XmlNode)child;
                    if (childNode.Name == "value")
                    {
                        mPropertiesXmlNodes.Add(childNode.Attributes["key"].Value, childNode);
                    }
                }
                mPropertiesTyped = new Dictionary<string, string>();
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

            public string GetValue(string propertyName)
            {
                return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public void SetValue(string propertyName, string newValue)
            {
                mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
            }
        }

        public class Preset : IComplate
        {
            readonly Complate mComplate;

            readonly XmlDocument mXmlDocument;

            public readonly CASPart CASPart;

            public IPackage ParentPackage
            {
                get
                {
                    return CASPart.ParentPackage;
                }
            }

            public List<string> PatternNames
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

            public Dictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mComplate.PropertiesTyped;
                }
            }

            public List<string> PropertyNames
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
                CASPart = casPart;
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(xmlFile.ReadToEnd());
                mComplate = new Complate(this, mXmlDocument.SelectSingleNode("preset").SelectSingleNode("complate"));
            }

            public string GetValue(string propertyName)
            {
                return mComplate.GetValue(propertyName);
            }

            public void SetValue(string propertyName, string newValue)
            {
                mComplate.SetValue(propertyName, newValue);
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
