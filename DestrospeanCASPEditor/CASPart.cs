using System;
using System.Collections.Generic;
using System.Xml;
using CASPartResource;
using CASPartResourceExtensions = CASPartResource.Extensions;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public class CASPart
    {
        public readonly CASPartResource.CASPartResource CASPartResource;

        public readonly IPackage CurrentPackage;

        public readonly List<Preset> Presets;

        public CASPart(IPackage package, CASPartResource.CASPartResource casPartResource, List<CASPartResource.CASPartResource.Preset> presets)
        {
            CASPartResource = casPartResource;
            CurrentPackage = package;
            Presets = presets.ConvertAll(new Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
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
                    return Patterns.ConvertAll(new Converter<Pattern, string>(x => x.Name));
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
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, CurrentPackage, ResourceUtils.EvaluateResourceKey(CurrentPackage, complateXmlNode)).Stream).ReadToEnd());
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

            readonly Dictionary<string, string> mPropertiesTyped;

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
                mXmlDocument = new XmlDocument();
                mXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, CurrentPackage, ResourceUtils.EvaluateResourceKey(CurrentPackage, patternXmlNode)).Stream).ReadToEnd());
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

            public Preset(CASPart casPart, CASPartResource.CASPartResource.Preset preset)
            {
                CASPart = casPart;
                XmlDocument = new XmlDocument();
                XmlDocument.LoadXml(preset.XmlFile.ReadToEnd());
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

        internal static void DEBUG_ConsoleLogCASPartDetails(CASPart casPart)
        {
            var casPartResource = casPart.CASPartResource;
            foreach (var flag in Enum.GetValues(typeof(ClothingCategoryFlags)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.ClothingCategory.HasFlag((ClothingCategoryFlags)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(ClothingType)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.Clothing.HasFlag((ClothingType)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(DataTypeFlags)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.DataType.HasFlag((DataTypeFlags)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(AgeFlags)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.AgeGender.Age.HasFlag((AgeFlags)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(GenderFlags)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.AgeGender.Gender.HasFlag((GenderFlags)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(SpeciesType)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.AgeGender.Species.HasFlag((SpeciesType)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(HandednessFlags)))
            {
                Console.WriteLine(flag.ToString() + ": " + casPartResource.AgeGender.Handedness.HasFlag((HandednessFlags)flag));
            }
            Console.WriteLine();
            foreach (var flag in Enum.GetValues(typeof(RegionType)))
            {
                Console.WriteLine(flag.ToString() + ": " + CASPartResourceExtensions.GetRegionType(casPartResource.ClothingCategory).HasFlag((RegionType)flag));
            }
            Console.WriteLine();
            foreach (var preset in casPart.Presets)
            {
                foreach (var propertyName in preset.PropertyNames)
                {
                    Console.WriteLine(propertyName + ": " + preset.GetValue(propertyName));
                }
            }
        }

        internal static void DEBUG_ConsoleLogCASPartsDetails(Dictionary<IResourceIndexEntry, CASPart> casParts)
        {
            foreach (var casPartKvp in casParts)
            {
                Console.WriteLine(ResourceUtils.GetResourceTypeTag(casPartKvp.Key) + " 0x" + casPartKvp.Key.Instance.ToString("X"));
                Console.WriteLine();
                DEBUG_ConsoleLogCASPartDetails(casPartKvp.Value);
            }
        }
    }
}
