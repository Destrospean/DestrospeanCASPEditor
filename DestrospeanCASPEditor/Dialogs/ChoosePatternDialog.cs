using System;
using System.Collections.Generic;
using System.Xml;
using Destrospean.S3PIAbstractions;
using Gtk;
using s3pi.Interfaces;

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

        public ChoosePatternDialog(Window parent, IPackage package) : this("Choose Pattern", parent, package)
        {
        }

        public ChoosePatternDialog(string title, Window parent, IPackage package) : base(title, parent, DialogFlags.Modal)
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
            System.Action<IResource> addPatternsByCategory = (IResource patternListResource) =>
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(new System.IO.StreamReader(patternListResource.Stream).ReadToEnd());
                    foreach (XmlNode childNode in xmlDocument.SelectSingleNode("patternlist").ChildNodes)
                    {
                        if (childNode.Name == "category")
                        {
                            var category = childNode.Attributes["name"].Value;
                            patternsByCategory.Add(category, new List<string>());
                            foreach (XmlElement grandchildNode in childNode.ChildNodes)
                            {
                                if (grandchildNode.Name == "pattern")
                                {
                                    var key = "";
                                    if (grandchildNode.HasAttribute("reskey"))
                                    {
                                        key = grandchildNode.Attributes["reskey"].Value;
                                    }
                                    else if (grandchildNode.HasAttribute("name"))
                                    {
                                        key = "key:0333406C:00000000:" + System.Security.Cryptography.FNV64.GetHash(grandchildNode.Attributes["name"].Value).ToString("X16");
                                    }
                                    else
                                    {
                                        throw new ResourceUtils.AttributeNotFoundException("The pattern XML node given does not have a \"reskey\" or \"name\" attribute.");
                                    }
                                    patternsByCategory[category].Add(key);
                                    patternsByCategory[category].Add(grandchildNode.HasAttribute("name") ? "Materials\\" + category + "\\" + grandchildNode.Attributes["name"].Value : key);
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
                        var maskKey = GetMaskKey(package, patternNameKeyPath[1]);
                        if (maskKey != null && !ImageUtils.PreloadedPatternImagePixbufs.TryGetValue(maskKey, out pixbufs))
                        {
                            var evaluated = package.EvaluateImageResourceKey(maskKey);
                            evaluated.Package.PreloadPatternImage(evaluated.ResourceIndexEntry, WidgetUtils.SmallImageSize << 1, WidgetUtils.SmallImageSize << 1);
                            pixbufs = ImageUtils.PreloadedPatternImagePixbufs[maskKey];
                        }
                        patternListStore.AppendValues(patternNameKeyPath[0], pixbufs == null ? ImageUtils.CreateCheckerboard(WidgetUtils.SmallImageSize << 1, 1, new Gdk.Color(byte.MaxValue, 0, 0), new Gdk.Color(byte.MaxValue, 0, 0)) : pixbufs[0]);
                        patternKeys.Add(patternNameKeyPath[1]);
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

        public static string GetMaskKey(IPackage package, string patternKey)
        {
            var evaluated = package.EvaluateResourceKey(patternKey);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
            foreach (XmlNode childNode in xmlDocument.SelectSingleNode("complate").ChildNodes)
            {
                if (childNode.Name == "variables")
                {
                    foreach (XmlNode grandchildNode in childNode.ChildNodes)
                    {
                        if (grandchildNode.Name == "param")
                        {
                            var defaultValue = grandchildNode.Attributes["default"].Value;
                            if (grandchildNode.Attributes["name"].Value.ToLower() == "rgbmask")
                            {
                                return defaultValue.StartsWith("($assetRoot)") ? "key:00B2D882:00000000:" + System.Security.Cryptography.FNV64.GetHash(defaultValue.Substring(defaultValue.LastIndexOf("\\") + 1, defaultValue.LastIndexOf(".tga") - defaultValue.LastIndexOf("\\") - 1)).ToString("X16") : grandchildNode.Attributes["default"].Value;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
