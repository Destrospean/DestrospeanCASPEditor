using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Destrospean.CmarNYCBorrowed;
using Destrospean.DestrospeanCASPEditor.Abstractions;
using Destrospean.S3PIExtensions;
using Gtk;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class ChoosePatternDialog : Dialog
    {
        public static readonly string CacheFilePath = string.Format("{0}{1}Destrospean{1}PatternThumbnailCache", Platform.IsMacOS ? Environment.GetFolderPath(Environment.SpecialFolder.InternetCache) : Platform.IsUnix ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.cache" : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.IO.Path.DirectorySeparatorChar);

        public string PatternPath
        {
            get;
            private set;
        }

        public static readonly Dictionary<string, Gdk.Pixbuf> PreloadedPatternImagePixbufs = new Dictionary<string, Gdk.Pixbuf>();

        public static readonly Dictionary<string, PatternInfo> PreloadedPatterns = new Dictionary<string, PatternInfo>();

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
            if (parent != null)
            {
                this.RescaleAndReposition(parent);
            }
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
                    xmlDocument.LoadXml(new StreamReader(patternListResource.Stream).ReadToEnd());
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
                                        throw new AttributeNotFoundException("The pattern XML node given does not have a \"reskey\" or \"name\" attribute.");
                                    }
                                    patternsByCategory[category].Add(key);
                                    patternsByCategory[category].Add(grandchildNode.HasAttribute("name") ? "Materials\\" + category + "\\" + grandchildNode.Attributes["name"].Value : key);
                                }
                            }
                        }
                    }
                };
            System.Action<int> setPatternKeysAndPaths = (int categoryIndex) =>
                {
                    patternKeys.Clear();
                    patternPaths.Clear();
                    patternListStore.Clear();
                    var patternKeysPaths = patternsByCategory[categories[categoryIndex]];
                    var patternNamesKeysPaths = new List<string[]>();
                    var uncachedPatternExists = false;
                    for (var i = 0; i < patternKeysPaths.Count; i += 2)
                    {
                        patternNamesKeysPaths.Add(new string[]
                            {
                                patternKeysPaths[i + 1].Substring(patternKeysPaths[i + 1].LastIndexOf("\\") + 1),
                                patternKeysPaths[i],
                                patternKeysPaths[i + 1]
                            });
                    }
                    patternNamesKeysPaths.Sort((a, b) => a[0].CompareTo(b[0]));
                    foreach (var patternNameKeyPath in patternNamesKeysPaths)
                    {
                        Gdk.Pixbuf pixbuf = null;
                        if (!PreloadedPatternImagePixbufs.TryGetValue(patternNameKeyPath[1], out pixbuf))
                        {
                            System.Drawing.Bitmap patternImage = null;
                            PatternInfo patternInfo;
                            if (!PreloadedPatterns.TryGetValue(patternNameKeyPath[1], out patternInfo))
                            {
                                patternInfo = PreloadedPatterns[patternNameKeyPath[1]] = GetPatternInfo(package, patternNameKeyPath[1]);
                            }
                            switch (patternInfo.Type)
                            {
                                case PatternType.Colored:
                                    patternImage = package.GetRGBPatternImage(patternInfo, ImageUtils.GetTexture);
                                    break;
                                case PatternType.HSV:
                                    patternImage = package.GetHSVPatternImage(patternInfo, ImageUtils.GetTexture);
                                    break;
                            }
                            if (patternImage != null)
                            {
                                pixbuf = PreloadedPatternImagePixbufs[patternNameKeyPath[1]] = patternImage.ToPixbuf().ScaleSimple(64, 64, Gdk.InterpType.Bilinear);
                            }
                            else if (patternInfo.Type == PatternType.Solid)
                            {
                                pixbuf = PreloadedPatternImagePixbufs[patternNameKeyPath[1]] = ImageUtils.CreateCheckerboard(64, 1, new Gdk.Color((byte)(patternInfo.SolidColor[0] * byte.MaxValue), (byte)(patternInfo.SolidColor[1] * byte.MaxValue), (byte)(patternInfo.SolidColor[2] * byte.MaxValue)), new Gdk.Color((byte)(patternInfo.SolidColor[0] * byte.MaxValue), (byte)(patternInfo.SolidColor[1] * byte.MaxValue), (byte)(patternInfo.SolidColor[2] * byte.MaxValue)));
                            }
                            uncachedPatternExists = true;
                        }
                        patternListStore.AppendValues(patternNameKeyPath[0], pixbuf.ScaleSimple(WidgetUtils.SmallImageSize << 1, WidgetUtils.SmallImageSize << 1, Gdk.InterpType.Bilinear));
                        patternKeys.Add(patternNameKeyPath[1]);
                        patternPaths.Add(patternNameKeyPath[2]);
                    }
                    if (uncachedPatternExists)
                    {
                        SaveCache();
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
            }
            categories.ForEach(x => categoryListStore.AppendValues(x));
            CategoryComboBox.Model = categoryListStore;
            CategoryComboBox.PackStart(categoryCell, true);
            CategoryComboBox.Active = 0;
            CategoryComboBox.Changed += (sender, e) => setPatternKeysAndPaths(CategoryComboBox.Active);
            PatternIconView.Model = patternListStore;
            PatternIconView.PixbufColumn = 1;
            PatternIconView.TooltipColumn = 0;
            PatternIconView.SelectionChanged += (sender, e) => OKButton.Sensitive = PatternIconView.SelectedItems.Length > 0;
            setPatternKeysAndPaths(0);
            Response += (o, args) =>
                {
                    if (args.ResponseId == ResponseType.Ok)
                    {
                        PatternPath = patternPaths[PatternIconView.SelectedItems[0].Indices[0]];
                        ResourceKey = patternKeys[PatternIconView.SelectedItems[0].Indices[0]];
                    }
                };
        }

        public static void GenerateCache(IPackage package)
        {
            List<string> categories = new List<string>();
            EvaluatedResourceKey gamePatternListEvaluated;
            try
            {
                 gamePatternListEvaluated = package.EvaluateResourceKey("key:D4D9FBE5:00000000:1BDE14D18B416FEC");
            }
            catch (ResourceIndexEntryNotFoundException)
            {
                return;
            }
            var patternsByCategory = new Dictionary<string, List<string>>();
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(new StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, gamePatternListEvaluated.Package, gamePatternListEvaluated.ResourceIndexEntry).Stream).ReadToEnd());
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
                                throw new AttributeNotFoundException("The pattern XML node given does not have a \"reskey\" or \"name\" attribute.");
                            }
                            patternsByCategory[category].Add(key);
                            patternsByCategory[category].Add(grandchildNode.HasAttribute("name") ? "Materials\\" + category + "\\" + grandchildNode.Attributes["name"].Value : key);
                        }
                    }
                }
            }
            categories.AddRange(patternsByCategory.Keys);
            categories.Sort();
            if (categories.Contains("Old"))
            {
                categories.Remove("Old");
            }
            for (var i = 0; i < categories.Count; i++)
            {
                var patternKeysPaths = patternsByCategory[categories[i]];
                var patternNamesKeysPaths = new List<string[]>();
                var uncachedPatternExists = false;
                for (var j = 0; j < patternKeysPaths.Count; j += 2)
                {
                    patternNamesKeysPaths.Add(new string[]
                        {
                            patternKeysPaths[j + 1].Substring(patternKeysPaths[j + 1].LastIndexOf("\\") + 1),
                            patternKeysPaths[j],
                            patternKeysPaths[j + 1]
                        });
                }
                patternNamesKeysPaths.Sort((a, b) => a[0].CompareTo(b[0]));
                foreach (var patternNameKeyPath in patternNamesKeysPaths)
                {
                    Gdk.Pixbuf pixbuf = null;
                    if (!PreloadedPatternImagePixbufs.TryGetValue(patternNameKeyPath[1], out pixbuf))
                    {
                        System.Drawing.Bitmap patternImage = null;
                        PatternInfo patternInfo;
                        if (!PreloadedPatterns.TryGetValue(patternNameKeyPath[1], out patternInfo))
                        {
                            patternInfo = PreloadedPatterns[patternNameKeyPath[1]] = GetPatternInfo(package, patternNameKeyPath[1]);
                        }
                        switch (patternInfo.Type)
                        {
                            case PatternType.Colored:
                                patternImage = package.GetRGBPatternImage(patternInfo, ImageUtils.GetTexture);
                                break;
                            case PatternType.HSV:
                                patternImage = package.GetHSVPatternImage(patternInfo, ImageUtils.GetTexture);
                                break;
                        }
                        if (patternImage != null)
                        {
                            pixbuf = PreloadedPatternImagePixbufs[patternNameKeyPath[1]] = patternImage.ToPixbuf().ScaleSimple(64, 64, Gdk.InterpType.Bilinear);
                        }
                        else if (patternInfo.Type == PatternType.Solid)
                        {
                            pixbuf = PreloadedPatternImagePixbufs[patternNameKeyPath[1]] = ImageUtils.CreateCheckerboard(64, 1, new Gdk.Color((byte)(patternInfo.SolidColor[0] * byte.MaxValue), (byte)(patternInfo.SolidColor[1] * byte.MaxValue), (byte)(patternInfo.SolidColor[2] * byte.MaxValue)), new Gdk.Color((byte)(patternInfo.SolidColor[0] * byte.MaxValue), (byte)(patternInfo.SolidColor[1] * byte.MaxValue), (byte)(patternInfo.SolidColor[2] * byte.MaxValue)));
                        }
                        uncachedPatternExists = true;
                    }
                }
                if (uncachedPatternExists)
                {
                    SaveCache();
                }
            }
        }

        public static PatternInfo GetPatternInfo(IPackage package, string patternKey)
        {
            var evaluated = package.EvaluateResourceKey(patternKey);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(new StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
            var propertiesXmlNodes = new Dictionary<string, string>();
            foreach (XmlNode childNode in xmlDocument.SelectSingleNode("complate").ChildNodes)
            {
                if (childNode.Name == "variables")
                {
                    foreach (XmlNode grandchildNode in childNode.ChildNodes)
                    {
                        if (grandchildNode.Name == "param")
                        {
                            var defaultValue = grandchildNode.Attributes["default"].Value;
                            propertiesXmlNodes.Add(grandchildNode.Attributes["name"].Value, grandchildNode.Attributes["type"].Value == "texture" && defaultValue.StartsWith("($assetRoot)") ? "key:00B2D882:00000000:" + System.Security.Cryptography.FNV64.GetHash(defaultValue.Substring(defaultValue.LastIndexOf("\\") + 1, defaultValue.LastIndexOf(".") - defaultValue.LastIndexOf("\\") - 1)).ToString("X16") : defaultValue);
                        }
                    }
                }
            }
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
            float[] hsvShiftBackground = null;
            foreach (var propertyXmlNodeKvp in propertiesXmlNodes)
            {
                string key = propertyXmlNodeKvp.Key.ToLower(),
                value = propertyXmlNodeKvp.Value;
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
                    var color = Pattern.ParseCommaSeparatedValues(value);
                    rgbColors.Add(new float[]
                        {
                            color[0],
                            color[1],
                            color[2]
                        });
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
                        hsvShiftBackground = Pattern.ParseCommaSeparatedValues(value);
                    }
                    else
                    {
                        hsvShift.Add(Pattern.ParseCommaSeparatedValues(value));
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
            for (var i = 0; i < baseHues.Count && baseHues.Count == baseSaturations.Count && baseHues.Count == baseValues.Count; i++)
            {
                baseHSVColors.Add(new float[]
                    {
                        baseHues[i],
                        baseSaturations[i],
                        baseValues[i]
                    });
            }
            for (var i = 0; i < hues.Count && hues.Count == saturations.Count && hues.Count == values.Count; i++)
            {
                hsvColors.Add(new float[]
                    {
                        hues[i],
                        saturations[i],
                        values[i]
                    });
            }
            return new PatternInfo
                {
                    Background = background,
                    Channels = channels.Count == 0 ? null : channels.ToArray(),
                    ChannelsEnabled = channelsEnabled.Count == 0 ? null : channelsEnabled.ToArray(),
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
                    RGBColors = rgbColors.ToArray(),
                    RGBMask = rgbMask,
                    SolidColor = rgbColors.Count == 1 ? rgbColors[0] : null
                };
        }

        public static void LoadCache()
        {
            if (File.Exists(CacheFilePath))
            {
                using (var reader = new Newtonsoft.Json.Bson.BsonReader(new FileStream(CacheFilePath, FileMode.Open)))
                {
                    foreach (var patternImageBase64StringKvp in new Newtonsoft.Json.JsonSerializer().Deserialize<Dictionary<string, string>>(reader))
                    {
                        PreloadedPatternImagePixbufs.Add(patternImageBase64StringKvp.Key, ImageUtils.Base64StringToBitmap(patternImageBase64StringKvp.Value).ToPixbuf());
                    }
                }
            }
        }

        public static void SaveCache()
        {
            var patternThumbnailCache = new Dictionary<string, string>();
            foreach (var patternImagePixbufKvp in PreloadedPatternImagePixbufs)
            {
                patternThumbnailCache.Add(patternImagePixbufKvp.Key, Convert.ToBase64String(patternImagePixbufKvp.Value.SaveToBuffer("png")));
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CacheFilePath));
            using (var writer = new Newtonsoft.Json.Bson.BsonWriter(new FileStream(CacheFilePath, FileMode.Create)))
            {
                new Newtonsoft.Json.JsonSerializer().Serialize(writer, patternThumbnailCache);
            }
        }
    }
}
