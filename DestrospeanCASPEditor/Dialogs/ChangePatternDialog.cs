using System.Collections.Generic;
using System.Xml;
using Destrospean.S3PIAbstractions;
using Gtk;

namespace Destrospean.DestrospeanCASPEditor
{
    public sealed class ChangePatternDialog : AddGEOMPropertyDialog
    {
        new System.Type DataType
        {
            get;
            set;
        }

        new s3pi.GenericRCOLResource.FieldType Field
        {
            get;
            set;
        }

        public string PatternPath
        {
            get;
            private set;
        }

        public string ResourceKey
        {
            get;
            private set;
        }

        public ChangePatternDialog(string title, Window parent, s3pi.Interfaces.IPackage package) : base(title, parent, true)
        {
            Build();
            DataTypeLabel.Text = "Pattern";
            FieldLabel.Text = "Category";
            this.RescaleAndReposition(parent);
            List<string> categories = new List<string>(),
            patternKeys = new List<string>(),
            patternPaths = new List<string>();
            CellRendererText categoryCell = new CellRendererText(),
            patternNameCell = new CellRendererText();
            ListStore categoryListStore = new ListStore(typeof(string)),
            patternNameListStore = new ListStore(typeof(string));
            var gamePatternListEvaluated = package.EvaluateResourceKey("key:D4D9FBE5:00000000:1BDE14D18B416FEC");
            var patternsByCategory = new Dictionary<string, List<string>>();
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, gamePatternListEvaluated.Package, gamePatternListEvaluated.ResourceIndexEntry).Stream).ReadToEnd());
            foreach (var categoryNodeObject in xmlDocument.SelectSingleNode("patternlist").ChildNodes)
            {
                var categoryNode = (XmlNode)categoryNodeObject;
                if (categoryNode.Name == "category")
                {
                    var category = categoryNode.Attributes["name"].Value;
                    patternsByCategory.Add(category, new List<string>());
                    foreach (var patternNodeObject in categoryNode.ChildNodes)
                    {
                        var patternNode = (XmlElement)patternNodeObject;
                        if (patternNode.Name == "pattern")
                        {
                            var key = "";
                            if (patternNode.HasAttribute("reskey"))
                            {
                                key = patternNode.Attributes["reskey"].Value;
                            }
                            else if (patternNode.HasAttribute("name"))
                            {
                                key = "key:0333406C:00000000:" + System.Security.Cryptography.FNV64.GetHash(patternNode.Attributes["name"].Value).ToString("X16");
                            }
                            else
                            {
                                throw new ResourceUtils.AttributeNotFoundException("The pattern XML node given does not have a \"reskey\" or \"name\" attribute.");
                            }
                            //package.EvaluateResourceKey(key);
                            patternsByCategory[category].Add(key);
                            patternsByCategory[category].Add(patternNode.HasAttribute("name") ? "Materials\\" + category + "\\" + patternNode.Attributes["name"].Value : key);
                        }
                    }
                }
            }
            System.Action setPatternKeysAndPaths = delegate
                {
                    patternKeys.Clear();
                    patternPaths.Clear();
                    patternNameListStore.Clear();
                    var patternKeysAndPaths = patternsByCategory[categories[FieldComboBox.Active == -1 ? 0 : FieldComboBox.Active]];
                    var patternNamesKeysPaths = new List<string[]>();
                    for (var i = 0; i < patternKeysAndPaths.Count; i += 2)
                    {
                        patternNamesKeysPaths.Add(new string[]
                            {
                                patternKeysAndPaths[i + 1].Substring(patternKeysAndPaths[i + 1].LastIndexOf("\\") + 1),
                                patternKeysAndPaths[i],
                                patternKeysAndPaths[i + 1]
                            });

                    }
                    patternNamesKeysPaths.Sort((a, b) => a[0].CompareTo(b[0]));
                    foreach (var patternNameKeyPath in patternNamesKeysPaths)
                    {
                        patternNameListStore.AppendValues(patternNameKeyPath[0]);
                        patternKeys.Add(patternNameKeyPath[1]);
                        patternPaths.Add(patternNameKeyPath[2]);
                    }
                    DataTypeComboBox.Active = 0;
                };
            categories.AddRange(patternsByCategory.Keys);
            categories.Sort();
            if (categories.Contains("Old"))
            {
                categories.Remove("Old");
                categories.Add("Old");
            }
            categories.ForEach(x => categoryListStore.AppendValues(x));
            FieldComboBox.Model = categoryListStore;
            FieldComboBox.PackStart(categoryCell, true);
            FieldComboBox.Active = 0;
            FieldComboBox.Changed += (sender, e) => setPatternKeysAndPaths();
            DataTypeComboBox.Model = patternNameListStore;
            DataTypeComboBox.PackStart(patternNameCell, true);
            setPatternKeysAndPaths();
            Response += (o, args) =>
                {
                    if (args.ResponseId == ResponseType.Ok)
                    {
                        PatternPath = patternPaths[DataTypeComboBox.Active];
                        ResourceKey = patternKeys[DataTypeComboBox.Active];
                    }
                };
        }

        public ChangePatternDialog(Window parent, s3pi.Interfaces.IPackage package) : this("Change Pattern", parent, package)
        {
        }
    }
}
