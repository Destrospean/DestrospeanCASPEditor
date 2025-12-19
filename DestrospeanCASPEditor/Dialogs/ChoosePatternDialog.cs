using System;
using System.Collections.Generic;
using System.Xml;
using Destrospean.S3PIAbstractions;
using Gtk;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class ChoosePatternDialog : Gtk.Dialog
    {
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

        public ChoosePatternDialog(Window parent, s3pi.Interfaces.IPackage package) : this("Choose Pattern", parent, package)
        {
        }

        public ChoosePatternDialog(string title, Window parent, s3pi.Interfaces.IPackage package) : base(title, parent, DialogFlags.Modal)
        {
            Build();
            this.RescaleAndReposition(parent);
            PatternIconView.ColumnSpacing = (int)(PatternIconView.ColumnSpacing * WidgetUtils.Scale);
            PatternIconView.Margin = (int)(PatternIconView.Margin * WidgetUtils.Scale);
            PatternIconView.RowSpacing = (int)(PatternIconView.RowSpacing * WidgetUtils.Scale);
            List<string> categories = new List<string>(),
            patternKeys = new List<string>(),
            patternPaths = new List<string>();
            var categoryCell = new CellRendererText();
            ListStore categoryListStore = new ListStore(typeof(string)),
            patternListStore = new ListStore(typeof(string), typeof(Gdk.Pixbuf));
            var gamePatternListEvaluated = package.EvaluateResourceKey("key:D4D9FBE5:00000000:1BDE14D18B416FEC");
            var patternsByCategory = new Dictionary<string, List<string>>();
            System.Action<s3pi.Interfaces.IResource> addPatternsByCategory = (s3pi.Interfaces.IResource patternListResource) =>
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(new System.IO.StreamReader(patternListResource.Stream).ReadToEnd());
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
                                    patternsByCategory[category].Add(key);
                                    patternsByCategory[category].Add(patternNode.HasAttribute("name") ? "Materials\\" + category + "\\" + patternNode.Attributes["name"].Value : key);
                                }
                            }
                        }
                    }
                };
            System.Action setPatternKeysAndPaths = delegate
                {
                    patternKeys.Clear();
                    patternPaths.Clear();
                    patternListStore.Clear();
                    var patternKeysPaths = patternsByCategory[categories[CategoryComboBox.Active == -1 ? 0 : CategoryComboBox.Active]];
                    var patternNamesKeysPaths = new List<string[]>();
                    for (var i = 0; i < patternKeysPaths.Count; i += 2)
                    {
                        patternNamesKeysPaths.Add(new string[]
                            {
                                patternKeysPaths[i + 1].Substring(patternKeysPaths[i + 1].LastIndexOf("\\") + 1),
                                patternKeysPaths[i],
                                patternKeysPaths[i + 1],
                            });
                    }
                    patternNamesKeysPaths.Sort((a, b) => a[0].CompareTo(b[0]));
                    foreach (var patternNameKeyPath in patternNamesKeysPaths)
                    {
                        List<Gdk.Pixbuf> pixbufs = null;
                        var key = patternNameKeyPath[1];
                        var rgbMaskKey = GetRGBMaskKey(package, key);
                        if (rgbMaskKey != null && !ImageUtils.PreloadedPatternImagePixbufs.TryGetValue(rgbMaskKey, out pixbufs))
                        {
                            var evaluated = package.EvaluateImageResourceKey(rgbMaskKey);
                            evaluated.Package.PreloadPatternImage(evaluated.ResourceIndexEntry, WidgetUtils.SmallImageSize << 1, WidgetUtils.SmallImageSize << 1);
                            pixbufs = ImageUtils.PreloadedPatternImagePixbufs[rgbMaskKey];
                        }
                        patternListStore.AppendValues(patternNameKeyPath[0], pixbufs == null ? ImageUtils.CreateCheckerboard(WidgetUtils.SmallImageSize << 1, 1, new Gdk.Color(byte.MaxValue, 0, 0), new Gdk.Color(byte.MaxValue, 0, 0)) : pixbufs[0]);
                        patternKeys.Add(key);
                        patternPaths.Add(patternNameKeyPath[2]);
                    }
                    PatternIconView.SelectPath(new TreePath("0"));
                };
            addPatternsByCategory(s3pi.WrapperDealer.WrapperDealer.GetResource(0, gamePatternListEvaluated.Package, gamePatternListEvaluated.ResourceIndexEntry));
            foreach (var patternListResourceIndexEntry in package.FindAll(x => x.ResourceType == ResourceUtils.GetResourceType("PTRN")))
            {
                addPatternsByCategory(s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, patternListResourceIndexEntry));
            }
            categories.AddRange(patternsByCategory.Keys);
            categories.Sort();
            if (categories.Contains("Old"))
            {
                categories.Remove("Old");
                //categories.Add("Old");
            }
            categories.ForEach(x => categoryListStore.AppendValues(x));
            CategoryComboBox.Model = categoryListStore;
            CategoryComboBox.PackStart(categoryCell, true);
            CategoryComboBox.Active = 0;
            CategoryComboBox.Changed += (sender, e) => setPatternKeysAndPaths();
            PatternIconView.Model = patternListStore;
            PatternIconView.PixbufColumn = 1;
            PatternIconView.TooltipColumn = 0;
            PatternIconView.SelectionChanged += (sender, e) => OKButton.Sensitive = PatternIconView.SelectedItems.Length > 0;
            setPatternKeysAndPaths();
            Response += (o, args) =>
                {
                    if (args.ResponseId == ResponseType.Ok)
                    {
                        PatternPath = patternPaths[PatternIconView.SelectedItems[0].Indices[0]];
                        ResourceKey = patternKeys[PatternIconView.SelectedItems[0].Indices[0]];
                    }
                };
        }

        public string GetRGBMaskKey(s3pi.Interfaces.IPackage package, string patternResourceKey)
        {
            var evaluated = package.EvaluateResourceKey(patternResourceKey);
            var patternXmlDocument = new XmlDocument();
            patternXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
            foreach (var patternChild in patternXmlDocument.SelectSingleNode("complate").ChildNodes)
            {
                var patternChildNode = (XmlNode)patternChild;
                if (patternChildNode.Name == "variables")
                {
                    foreach (var patternGrandchild in patternChildNode.ChildNodes)
                    {
                        var patternGrandchildNode = (XmlNode)patternGrandchild;
                        if (patternGrandchildNode.Name == "param")
                        {
                            var defaultValue = patternGrandchildNode.Attributes["default"].Value;
                            if (patternGrandchildNode.Attributes["name"].Value.ToLower() == "rgbmask")
                            {
                                return defaultValue.StartsWith("($assetRoot)") ? "key:00B2D882:00000000:" + System.Security.Cryptography.FNV64.GetHash(defaultValue.Substring(defaultValue.LastIndexOf("\\") + 1, defaultValue.LastIndexOf(".tga") - defaultValue.LastIndexOf("\\") - 1)).ToString("X16") : patternGrandchildNode.Attributes["default"].Value;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
