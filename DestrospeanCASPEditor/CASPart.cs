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
            Presets = presets.ConvertAll(new System.Converter<CASPartResource.CASPartResource.Preset, Preset>(x => new Preset(this, x)));
        }

        public class Complate
        {
            readonly XmlDocument XmlDocument;

            readonly Dictionary<string, Dictionary<string, XmlNode>> PatternPropertiesXmlNodes;

            readonly Dictionary<string, XmlNode> PropertiesXmlNodes;

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
                    return new List<string>(PatternPropertiesXmlNodes.Keys);
                }
            }

            public readonly Preset Preset;

            public List<List<string>> PatternPropertyNames
            {
                get
                {
                    return new List<Dictionary<string, XmlNode>>(PatternPropertiesXmlNodes.Values).ConvertAll(new Converter<Dictionary<string, XmlNode>, List<string>>(x => new List<string>(x.Keys)));
                }
            }

            public readonly Dictionary<string, string> PropertiesTyped;

            public List<string> PropertyNames
            {
                get
                {
                    return new List<string>(PropertiesXmlNodes.Keys);
                }
            }

            public Complate(Preset preset, XmlNode complateXmlNode)
            {
                Preset = preset;
                XmlDocument = new XmlDocument();
                XmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, CurrentPackage, ResourceUtils.EvaluateResourceKey(CurrentPackage, complateXmlNode)).Stream).ReadToEnd());
                PatternPropertiesXmlNodes = new Dictionary<string, Dictionary<string, XmlNode>>();
                PropertiesXmlNodes = new Dictionary<string, XmlNode>();
                foreach (var child in complateXmlNode.ChildNodes)
                {
                    var childNode = (XmlNode)child;
                    if (childNode.Name == "value")
                    {
                        AddPropertyXmlNode(childNode.Attributes["key"].Value, childNode);
                    }
                    if (childNode.Name == "pattern")
                    {
                        PatternPropertiesXmlNodes.Add(childNode.Attributes["variable"].Value, new Dictionary<string, XmlNode>());
                        foreach (var grandchild in childNode.ChildNodes)
                        {
                            var grandchildNode = (XmlNode)grandchild;
                            if (grandchildNode.Name == "value")
                            {
                                AddPatternPropertyXmlNode(childNode.Attributes["variable"].Value, grandchildNode.Attributes["key"].Value, grandchildNode);
                            }
                        }
                    }
                }
                PropertiesTyped = new Dictionary<string, string>();
                foreach (var child in XmlDocument.SelectSingleNode("complate").ChildNodes)
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

            void AddPatternPropertyXmlNode(int patternIndex, string propertyName, XmlNode xmlNode)
            {
                new List<Dictionary<string, XmlNode>>(PatternPropertiesXmlNodes.Values)[patternIndex].Add(propertyName, xmlNode);
            }

            void AddPatternPropertyXmlNode(string patternName, string propertyName, XmlNode xmlNode)
            {
                PatternPropertiesXmlNodes[patternName].Add(propertyName, xmlNode);
            }

            void AddPropertyXmlNode(string propertyName, XmlNode xmlNode)
            {
                PropertiesXmlNodes.Add(propertyName, xmlNode);
            }

            public List<string> GetPatternPropertyNames(int patternIndex)
            {
                return PatternPropertyNames[patternIndex];
            }

            public List<string> GetPatternPropertyNames(string patternName)
            {
                return PatternPropertyNames[PatternNames.IndexOf(patternName)];
            }

            public string GetPatternValue(int patternIndex, string propertyName)
            {
                return new List<Dictionary<string, XmlNode>>(PatternPropertiesXmlNodes.Values)[patternIndex][propertyName].Attributes["value"].Value;
            }

            public string GetPatternValue(string patternName, string propertyName)
            {
                return PatternPropertiesXmlNodes[patternName][propertyName].Attributes["value"].Value;
            }

            public string GetValue(string propertyName)
            {
                return PropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public void SetPatternValue(int patternIndex, string propertyName, string newValue)
            {
                new List<Dictionary<string, XmlNode>>(PatternPropertiesXmlNodes.Values)[patternIndex][propertyName].Attributes["value"].Value = newValue;
            }

            public void SetPatternValue(string patternName, string propertyName, string newValue)
            {
                PatternPropertiesXmlNodes[patternName][propertyName].Attributes["value"].Value = newValue;
            }

            public void SetValue(string propertyName, string newValue)
            {
                PropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
            }
        }

        public class Preset
        {
            readonly XmlDocument XmlDocument;

            public readonly CASPart CASPart;

            public readonly Complate Complate;

            public IPackage CurrentPackage
            {
                get
                {
                    return CASPart.CurrentPackage;
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
                foreach (var propertyName in preset.Complate.PropertyNames)
                {
                    Console.WriteLine(propertyName + ": " + preset.Complate.GetValue(propertyName));
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
