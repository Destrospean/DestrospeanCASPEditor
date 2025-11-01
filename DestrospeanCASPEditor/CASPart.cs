using System.Collections.Generic;
using System.Xml;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public class CASPart
    {
        public readonly CASPartResource.CASPartResource CASPartResource;

        public readonly IPackage CurrentPackage;

        public readonly List<Preset> Presets;

        public readonly IResourceIndexEntry ResourceIndexEntry;

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry)
        {
            CASPartResource = (CASPartResource.CASPartResource)s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry);
            CurrentPackage = package;
            ResourceIndexEntry = resourceIndexEntry;
            Presets = CASPartResource.Presets.ConvertAll(new System.Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
        }

        public interface IComplate
        {
            IPackage CurrentPackage
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
            readonly XmlDocument mXmlDocument;

            readonly Dictionary<string, string> mPropertiesTyped;

            readonly Dictionary<string, XmlNode> mPropertiesXmlNodes;

            public IPackage CurrentPackage
            {
                get
                {
                    return Preset.CurrentPackage;
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
                var evaluated = ResourceUtils.EvaluateResourceKey(CurrentPackage, complateXmlNode);
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
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
                        Patterns.Add(new Pattern(this, childNode));
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
            readonly XmlDocument mXmlDocument;

            readonly Dictionary<string, string> mPropertiesTyped;

            readonly Dictionary<string, XmlNode> mPropertiesXmlNodes;

            public readonly Complate Complate;

            public IPackage CurrentPackage
            {
                get
                {
                    return Complate.CurrentPackage;
                }
            }

            public readonly string Name;

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

            public Pattern(Complate complate, XmlNode patternXmlNode)
            {
                Complate = complate;
                Name = patternXmlNode.Attributes["variable"].Value;
                var evaluated = ResourceUtils.EvaluateResourceKey(CurrentPackage, patternXmlNode);
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).Stream).ReadToEnd());
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
            readonly XmlDocument XmlDocument;

            readonly Complate Complate;

            public readonly CASPart CASPart;

            public IPackage CurrentPackage
            {
                get
                {
                    return CASPart.CurrentPackage;
                }
            }

            public List<string> PatternNames
            {
                get
                {
                    return Complate.PatternNames;
                }
            }

            public List<Pattern> Patterns
            {
                get
                {
                    return Complate.Patterns;
                }
            }

            public Dictionary<string, string> PropertiesTyped
            {
                get
                {
                    return Complate.PropertiesTyped;
                }
            }

            public List<string> PropertyNames
            {
                get
                {
                    return Complate.PropertyNames;
                }
            }


            public System.IO.StringReader XmlFile
            {
                get
                {
                    return new System.IO.StringReader(XmlDocument.InnerXml);
                }
            }

            public Preset(CASPart casPart, CASPartResource.CASPartResource.Preset preset) : this(casPart, preset.XmlFile)
            {
            }

            public Preset(CASPart casPart, System.IO.TextReader xmlFile)
            {
                CASPart = casPart;
                XmlDocument = new XmlDocument();
                XmlDocument.LoadXml(xmlFile.ReadToEnd());
                Complate = new Complate(this, XmlDocument.SelectSingleNode("preset").SelectSingleNode("complate"));
            }

            public string GetValue(string propertyName)
            {
                return Complate.GetValue(propertyName);
            }

            public void SetValue(string propertyName, string newValue)
            {
                Complate.SetValue(propertyName, newValue);
            }
        }

        public void SavePreset(int index)
        {
            CASPartResource.Presets[index].XmlFile = Presets[index].XmlFile;
        }

        public void SavePresets()
        {
            for (int i = 0; i < CASPartResource.Presets.Count; i++)
            {
                SavePreset(i);
            }
        }
    }
}
