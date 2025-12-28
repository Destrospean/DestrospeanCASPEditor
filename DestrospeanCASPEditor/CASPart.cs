using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using Destrospean.CmarNYCBorrowed;
using Destrospean.S3PIAbstractions;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;
using System.Globalization;

namespace Destrospean.DestrospeanCASPEditor
{
    public class CASPart
    {
        RIG mCurrentRig;

        public AgeGender AdjustedAge
        {
            get
            {
                var age = (AgeGender)(uint)CASPartResource.AgeGender.Age;
                return age >= AgeGender.Teen && age <= AgeGender.Elder ? AgeGender.Adult : age;
            }
        }

        public Species AdjustedSpecies
        {
            get
            {
                var species = (Species)((uint)CASPartResource.AgeGender.Species << 8);
                return species == 0 ? Species.Human : species;
            }
        }

        public List<Preset> AllPresets
        {
            get
            {
                var allPresets = new List<Preset>(Presets);
                if (DefaultPreset != null)
                {
                    allPresets.Insert(0, DefaultPreset);
                }
                return allPresets;
            }
        }
            
        public readonly CASPartResource.CASPartResource CASPartResource;

        public RIG CurrentRig
        {
            get
            {
                if (mCurrentRig == null)
                {
                    mCurrentRig = MeshUtils.GetRig(ParentPackage, AdjustedSpecies, AdjustedAge);
                }
                return mCurrentRig;
            }
            private set
            {
                mCurrentRig = value;
            }
        }

        public readonly Preset DefaultPreset;

        public readonly string DefaultPresetKey;

        public readonly Dictionary<int, List<GEOM>> LODs = new Dictionary<int, List<GEOM>>();

        public readonly IPackage ParentPackage;

        public readonly List<Preset> Presets;

        public abstract class Complate
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

            protected class PropertyNameComparer : IComparer<string>
            {
                public int Compare(string a, string b)
                {
                    string aCopy = a,
                    bCopy = b;
                    while (aCopy.Length < bCopy.Length)
                    {
                        aCopy += " ";
                    }
                    while (aCopy.Length > bCopy.Length)
                    {
                        bCopy += " ";
                    }
                    for (var i = 0; i < aCopy.Length; i++)
                    {
                        if (aCopy[i] != bCopy[i] && aCopy.Substring(0, i) == bCopy.Substring(0, i))
                        {
                            bool aCharIsNum = '0' <= aCopy[i] && aCopy[i] <= '9',
                            bCharIsNum = '0' <= bCopy[i] && bCopy[i] <= '9';
                            if (aCharIsNum && !bCharIsNum)
                            {
                                return 1;
                            }
                            if (!aCharIsNum && bCharIsNum)
                            {
                                return -1;
                            }
                        }
                    }
                    return string.Compare(a, b);
                }
            }

            public Complate()
            {
                mXmlDocument = new XmlDocument();
                mPropertiesXmlNodes = new SortedDictionary<string, XmlNode>(new PropertyNameComparer());
                mPropertiesTyped = new SortedDictionary<string, string>(new PropertyNameComparer());
            }

            public virtual string GetValue(string propertyName)
            {
                return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
            }

            public static float[] ParseCommaSeparatedValues(string text)
            {
                return Array.ConvertAll(text.Split(','), x => float.Parse(x, CultureInfo.InvariantCulture));
            }

            public virtual void SetValue(string propertyName, string newValue, System.Action beforeMarkUnsaved = null)
            {
                mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
                if (beforeMarkUnsaved != null)
                {
                    beforeMarkUnsaved();
                }
                MainWindow.Singleton.NextState = NextStateOptions.UnsavedChanges;
            }
        }

        public class Pattern : Complate
        {
            protected object mPatternImage;

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

            public object PatternImage
            {
                get
                {
                    if (mPatternImage == null)
                    {
                        switch (PatternInfo.Type)
                        {
                            case PatternType.Colored:
                                mPatternImage = ParentPackage.GetRGBPatternImage(PatternInfo, ImageUtils.GetTexture);
                                break;
                            case PatternType.HSV:
                                mPatternImage = ParentPackage.GetHSVPatternImage(PatternInfo, ImageUtils.GetTexture);
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

            public readonly string SlotName;

            public Pattern(Preset preset, XmlNode patternXmlNode) : base()
            {
                SlotName = patternXmlNode.Attributes["variable"].Value;
                PatternInfo = new PatternInfo
                    {
                        Name = patternXmlNode.Attributes["name"].Value
                    };
                Preset = preset;
                var evaluated = ParentPackage.EvaluateResourceKey(patternXmlNode);
                mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
                foreach (XmlNode childNode in patternXmlNode.ChildNodes)
                {
                    if (childNode.Name == "value")
                    {
                        mPropertiesXmlNodes.Add(childNode.Attributes["key"].Value, childNode);
                    }
                }
                foreach (XmlNode childNode in mXmlDocument.SelectSingleNode("complate").ChildNodes)
                {
                    if (childNode.Name == "variables")
                    {
                        foreach (XmlNode grandchildNode in childNode.ChildNodes)
                        {
                            if (grandchildNode.Name == "param")
                            {
                                PropertiesTyped.Add(grandchildNode.Attributes["name"].Value, grandchildNode.Attributes["type"].Value);
                            }
                        }
                    }
                }
                RefreshPatternInfo(false);
            }

            public void RefreshPatternInfo(bool regeneratePresetTexture = true)
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
                float[] hsvShiftBackground = null;
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
                        var color = ParseCommaSeparatedValues(value);
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
                            baseHueBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            baseHues.Add(float.Parse(value, CultureInfo.InvariantCulture));
                        }
                    }
                    else if (key.StartsWith("base s"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            baseSaturationBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            baseSaturations.Add(float.Parse(value, CultureInfo.InvariantCulture));
                        }
                    }
                    else if (key.StartsWith("base v"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            baseValueBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            baseValues.Add(float.Parse(value, CultureInfo.InvariantCulture));
                        }
                    }
                    else if (key.StartsWith("h "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            hueBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            hues.Add(float.Parse(value, CultureInfo.InvariantCulture));
                        }
                    }
                    else if (key.StartsWith("hsvshift"))
                    {
                        if (key.EndsWith("bg"))
                        {
                            hsvShiftBackground = ParseCommaSeparatedValues(value);
                        }
                        else
                        {
                            hsvShift.Add(ParseCommaSeparatedValues(value));
                        }
                    }
                    else if (key.StartsWith("s "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            saturationBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            saturations.Add(float.Parse(value, CultureInfo.InvariantCulture));
                        }
                    }
                    else if (key.StartsWith("v "))
                    {
                        if (key.EndsWith("bg"))
                        {
                            valueBackground = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            values.Add(float.Parse(value, CultureInfo.InvariantCulture));
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
                PatternInfo = new PatternInfo
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
                        Name = PatternInfo.Name,
                        RGBColors = rgbColors.ToArray(),
                        RGBMask = rgbMask,
                        SolidColor = rgbColors.Count == 1 ? rgbColors[0] : null
                    };
                if (regeneratePresetTexture)
                {
                    Preset.RegenerateTexture();
                }
                PatternImage = null;
            }

            public override void SetValue(string propertyName, string newValue, System.Action beforeMarkUnsaved = null)
            {
                base.SetValue(propertyName, newValue, beforeMarkUnsaved ?? (() => RefreshPatternInfo()));
            }
        }

        public class Preset : Complate
        {
            protected readonly CASPart mCASPart;

            protected readonly PresetInternal mInternal;

            protected new readonly XmlDocument mXmlDocument;

            public string AmbientMap
            {
                get
                {
                    return mInternal.AmbientMap;
                }
            }

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

            public List<Pattern> Patterns
            {
                get
                {
                    return mInternal.Patterns;
                }
            }

            public string[] PatternSlotNames
            {
                get
                {
                    return mInternal.PatternSlotNames;
                }
            }

            public override IDictionary<string, string> PropertiesTyped
            {
                get
                {
                    return mInternal.PropertiesTyped;
                }
            }

            public override string[] PropertyNames
            {
                get
                {
                    return mInternal.PropertyNames;
                }
            }

            public string SpecularMap
            {
                get
                {
                    return mInternal.SpecularMap;
                }
            }

            public Bitmap Texture
            {
                get
                {
                    return mInternal.Texture;
                }
            }

            public StringReader XmlFile
            {
                get
                {
                    var stream = new MemoryStream();
                    mXmlDocument.Save(new XmlTextWriter(stream, System.Text.Encoding.UTF8)
                        {
                            Formatting = Formatting.Indented
                        });
                    stream.Position = 0;
                    var text = new StreamReader(stream).ReadToEnd();
                    return new StringReader(text.Substring(text.IndexOf("<preset>")));
                }
            }

            protected class PresetInternal : Complate
            {
                protected Bitmap mTexture;

                public string AmbientMap
                {
                    get;
                    private set;
                }

                public override CASPart CASPart
                {
                    get
                    {
                        return Preset.CASPart;
                    }
                }

                public Bitmap NewTexture
                {
                    get
                    {
                        uint[] controlMapArray = null,
                        maskArray = null;
                        Bitmap diffuseMap = null,
                        multiplier = null,
                        overlay = null;
                        float[] diffuseColor = null,
                        highlightColor = null,
                        rootColor = null,
                        tipColor = null;
                        int height = 1024,
                        width = 1024;
                        List<Bitmap> logos = new List<Bitmap>(),
                        stencils = new List<Bitmap>();
                        List<bool> logosEnabled = new List<bool>(),
                        stencilsEnabled = new List<bool>();
                        List<float[]> logosLowerRight = new List<float[]>(),
                        logosUpperLeft = new List<float[]>();
                        List<float> logosRotation = new List<float>(),
                        stencilsRotation = new List<float>();
                        foreach (var propertyXmlNodeKvp in mPropertiesXmlNodes)
                        {
                            string key = propertyXmlNodeKvp.Key.ToLower(),
                            value = propertyXmlNodeKvp.Value.Attributes["value"].Value;
                            if (key.StartsWith("logo"))
                            {
                                if (key.EndsWith("enabled"))
                                {
                                    logosEnabled.Add(bool.Parse(value));
                                }
                                else if (key.EndsWith("lowerright"))
                                {
                                    logosLowerRight.Add(ParseCommaSeparatedValues(value));
                                }
                                else if (key.EndsWith("upperleft"))
                                {
                                    logosUpperLeft.Add(ParseCommaSeparatedValues(value));
                                }
                                else if (key.EndsWith("rotation"))
                                {
                                    logosRotation.Add(float.Parse(value, CultureInfo.InvariantCulture));
                                }
                                else if (key.EndsWith("texture"))
                                {
                                    logos.Add(ParentPackage.GetTexture(value, ImageUtils.GetTexture, width, height));
                                }
                            }
                            else if (key.StartsWith("stencil"))
                            {
                                if (key.Length == 9)
                                {
                                    stencils.Add(ParentPackage.GetTexture(value, ImageUtils.GetTexture, width, height));
                                }
                                else if (key.EndsWith("enabled"))
                                {
                                    stencilsEnabled.Add(bool.Parse(value));
                                }
                                else if (key.EndsWith("rotation"))
                                {
                                    stencilsRotation.Add(float.Parse(value, CultureInfo.InvariantCulture));
                                }
                            }
                            else
                            {
                                switch (key)
                                {
                                    case "clothing ambient":
                                        AmbientMap = value;
                                        break;
                                    case "clothing specular":
                                        SpecularMap = value;
                                        break;
                                    case "control map":
                                        controlMapArray = ParentPackage.GetTextureARGBArray(value, ImageUtils.GetTexture, width, height);
                                        break;
                                    case "diffuse color":
                                        diffuseColor = ParseCommaSeparatedValues(value);
                                        break;
                                    case "diffuse map":
                                        diffuseMap = ParentPackage.GetTexture(value, ImageUtils.GetTexture, width, height);
                                        break;
                                    case "highlight color":
                                        highlightColor = ParseCommaSeparatedValues(value);
                                        break;
                                    case "mask":
                                        maskArray = ParentPackage.GetTextureARGBArray(value, ImageUtils.GetTexture, width, height);
                                        break;
                                    case "multiplier":
                                        multiplier = ParentPackage.GetTexture(value, ImageUtils.GetTexture, width, height);
                                        break;
                                    case "overlay":
                                        overlay = ParentPackage.GetTexture(value, ImageUtils.GetTexture, width, height);
                                        break;
                                    case "root color":
                                        rootColor = ParseCommaSeparatedValues(value);
                                        break;
                                    case "tip color":
                                        tipColor = ParseCommaSeparatedValues(value);
                                        break;
                                }
                            }
                        }
                        if (diffuseMap != null)
                        {
                            if (mXmlDocument.SelectSingleNode("complate").Attributes["name"].Value.ToLower() == "hairuniversal")
                            {
                                float[][] hairMatrix =
                                    {
                                        new float[]
                                        {
                                            diffuseColor[0],
                                            0,
                                            0,
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            diffuseColor[1],
                                            0,
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            diffuseColor[2],
                                            0,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            0,
                                            1,
                                            0
                                        },
                                        new float[]
                                        {
                                            0,
                                            0,
                                            0,
                                            0,
                                            1
                                        }
                                    };
                                using (var graphics = Graphics.FromImage(diffuseMap))
                                {
                                    var attributes = new ImageAttributes();
                                    var colorMatrix = new ColorMatrix(hairMatrix);
                                    attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                                    graphics.DrawImage(diffuseMap, new Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height), 0, 0, diffuseMap.Width, diffuseMap.Height, GraphicsUnit.Pixel, attributes);
                                }
                                if (controlMapArray != null && highlightColor != null && rootColor != null && tipColor != null)
                                {
                                    try
                                    {
                                        diffuseMap = diffuseMap.GetWithPatternsApplied(controlMapArray, new List<object>
                                            {
                                                rootColor,
                                                highlightColor,
                                                tipColor
                                            }, false);
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                    }
                                }
                            }
                        }
                        var patternImages = Patterns.ConvertAll(x => bool.Parse(GetValue(x.SlotName + " Enabled")) ? x.PatternImage : null);
                        if (maskArray != null)
                        {
                            if (multiplier != null)
                            {
                                try
                                {
                                    multiplier = multiplier.GetWithPatternsApplied(maskArray, patternImages, false);
                                }
                                catch (IndexOutOfRangeException)
                                {
                                }
                            }
                            if (overlay != null)
                            {
                                try
                                {
                                    overlay = overlay.GetWithPatternsApplied(maskArray, patternImages, true);
                                }
                                catch (IndexOutOfRangeException)
                                {
                                }
                            }
                        }
                        var texture = new Bitmap(width, height);
                        using (var graphics = Graphics.FromImage(texture))
                        {
                            if (diffuseMap != null)
                            {
                                graphics.DrawImage(diffuseMap, 0, 0);
                            }
                            if (multiplier != null)
                            {
                                graphics.DrawImage(multiplier, 0, 0);
                            }
                            if (overlay != null)
                            {
                                graphics.DrawImage(overlay, 0, 0);
                            }
                            for (var i = 0; i < stencils.Count; i++)
                            {
                                if (stencilsEnabled[i])
                                {
                                    graphics.DrawImage(RotateImage(QuadrupleCanvasSize(stencils[i]), stencilsRotation[i] * 360), -stencils[i].Width >> 1, -stencils[i].Height >> 1);
                                }
                            }
                            for (var i = 0; i < logos.Count; i++)
                            {
                                if (logosEnabled[i])
                                {
                                    int logoHeight = (int)((logosLowerRight[i][1] - logosUpperLeft[i][1]) * height),
                                    logoWidth = (int)((logosLowerRight[i][0] - logosUpperLeft[i][0]) * width);
                                    graphics.DrawImage(RotateImage(QuadrupleCanvasSize(logos[i]), logosRotation[i] * 360), logosUpperLeft[i][0] * width - (logoWidth >> 1), logosUpperLeft[i][1] * height - (logoHeight >> 1), logoWidth << 1, logoHeight << 1);
                                }
                            }
                        }
                        return texture;
                    }
                }

                public override IPackage ParentPackage
                {
                    get
                    {
                        return Preset.ParentPackage;
                    }
                }

                public readonly List<Pattern> Patterns;

                public string[] PatternSlotNames
                {
                    get
                    {
                        return Patterns.ConvertAll(x => x.SlotName).ToArray();
                    }
                }

                public readonly Preset Preset;

                public string SpecularMap
                {
                    get;
                    private set;
                }

                public Bitmap Texture
                {
                    get
                    {
                        if (mTexture == null)
                        {
                            mTexture = NewTexture;
                        }
                        return mTexture;
                    }
                    set
                    {
                        mTexture = value;
                    }
                }

                public PresetInternal(Preset preset, XmlNode complateXmlNode) : base()
                {
                    Preset = preset;
                    var evaluated = ParentPackage.EvaluateResourceKey(complateXmlNode);
                    mXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
                    Patterns = new List<Pattern>();
                    foreach (XmlNode childNode in complateXmlNode.ChildNodes)
                    {
                        if (childNode.Name == "value")
                        {
                            mPropertiesXmlNodes.Add(childNode.Attributes["key"].Value, childNode);
                        }
                        if (childNode.Name == "pattern")
                        {
                            Patterns.Add(new Pattern(Preset, childNode));
                        }
                    }
                    foreach (XmlNode childNode in mXmlDocument.SelectSingleNode("complate").ChildNodes)
                    {
                        if (childNode.Name == "variables")
                        {
                            foreach (XmlNode grandchildNode in childNode.ChildNodes)
                            {
                                if (grandchildNode.Name == "param")
                                {
                                    PropertiesTyped.Add(grandchildNode.Attributes["name"].Value, grandchildNode.Attributes["type"].Value);
                                }
                            }
                        }
                    }
                }

                public static Bitmap QuadrupleCanvasSize(Bitmap image)
                {
                    var imageCopy = new Bitmap(image.Width << 1, image.Height << 1); 
                    using (var graphics = Graphics.FromImage(imageCopy))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, image.Width >> 1, image.Height >> 1);
                    }
                    return imageCopy;
                }

                public static Bitmap RotateImage(Bitmap image, float angle)
                {
                    var imageCopy = new Bitmap(image.Width, image.Height); 
                    using (var graphics = Graphics.FromImage(imageCopy))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.TranslateTransform(image.Width >> 1, image.Height >> 1);
                        graphics.RotateTransform(angle);
                        graphics.TranslateTransform(-image.Width >> 1, -image.Height >> 1);
                        graphics.DrawImage(image, 0, 0);
                    }
                    return imageCopy;
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
                mInternal = new PresetInternal(this, mXmlDocument.SelectSingleNode("preset").SelectSingleNode("complate"));
            }

            public override string GetValue(string propertyName)
            {
                return mInternal.GetValue(propertyName);
            }

            public void RegenerateTexture()
            {
                new System.Threading.Thread(() =>
                    {
                        System.Threading.Thread.Sleep(1);
                        mInternal.Texture = mInternal.NewTexture;
                        MainWindow.Singleton.ModelsNeedUpdated = true;
                    }).Start();
            }

            public void ReplacePattern(string patternSlotName, string patternKey)
            {
                int i = 0,
                patternIndex = Patterns.FindIndex(x => x.SlotName == patternSlotName);
                var evaluated = ParentPackage.EvaluateResourceKey(patternKey);
                var patternXmlDocument = new XmlDocument();
                patternXmlDocument.LoadXml(new StreamReader(WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
                foreach (XmlNode presetChildNode in mXmlDocument.SelectSingleNode("preset").SelectSingleNode("complate").ChildNodes)
                {
                    if (presetChildNode.Name == "pattern" && i++ == patternIndex)
                    {
                        presetChildNode.Attributes["name"].Value = patternXmlDocument.SelectSingleNode("complate").Attributes["name"].Value;
                        presetChildNode.Attributes["reskey"].Value = patternKey;
                        for (var j = presetChildNode.ChildNodes.Count - 1; j > -1; j--)
                        {
                            switch (presetChildNode.ChildNodes[j].Attributes["key"].Value.ToLower())
                            {
                                case "assetroot":
                                case "filename":
                                    break;
                                default:
                                    presetChildNode.RemoveChild(presetChildNode.ChildNodes[j]);
                                    break;
                            }
                        }
                        foreach (XmlNode patternChildNode in patternXmlDocument.SelectSingleNode("complate").ChildNodes)
                        {
                            if (patternChildNode.Name == "variables")
                            {
                                foreach (XmlNode patternGrandchildNode in patternChildNode.ChildNodes)
                                {
                                    if (patternGrandchildNode.Name == "param")
                                    {
                                        var defaultValue = patternGrandchildNode.Attributes["default"].Value;
                                        var valueElement = mXmlDocument.CreateElement("value");
                                        valueElement.SetAttribute("key", patternGrandchildNode.Attributes["name"].Value);
                                        valueElement.SetAttribute("value", patternGrandchildNode.Attributes["type"].Value == "texture" && defaultValue.StartsWith("($assetRoot)") ? "key:00B2D882:00000000:" + System.Security.Cryptography.FNV64.GetHash(defaultValue.Substring(defaultValue.LastIndexOf("\\") + 1, defaultValue.LastIndexOf(".") - defaultValue.LastIndexOf("\\") - 1)).ToString("X16") : defaultValue);
                                        presetChildNode.AppendChild(valueElement);
                                    }
                                }
                            }
                        }
                        Patterns[patternIndex] = new Pattern(this, presetChildNode);
                        break;
                    }
                }
            }

            public override void SetValue(string propertyName, string newValue, System.Action beforeMarkUnsaved = null)
            {
                mInternal.SetValue(propertyName, newValue, beforeMarkUnsaved ?? RegenerateTexture);
            }
        }

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            CASPartResource = (CASPartResource.CASPartResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            ParentPackage = package;
            var defaultPresetResourceIndexEntries = ParentPackage.FindAll(x => x.ResourceType == ResourceUtils.GetResourceType("_XML") && x.ResourceGroup == resourceIndexEntry.ResourceGroup && x.Instance == resourceIndexEntry.Instance);
            if (defaultPresetResourceIndexEntries.Count == 0)
            {
                DefaultPresetKey = null;
                DefaultPreset = null;
            }
            else
            {
                DefaultPresetKey = defaultPresetResourceIndexEntries[0].ReverseEvaluateResourceKey();
                DefaultPreset = new Preset(this, new StreamReader(WrapperDealer.GetResource(0, ParentPackage, defaultPresetResourceIndexEntries[0]).Stream));
            }
            Presets = CASPartResource.Presets.ConvertAll(x => new Preset(this, x));
            LoadLODs(geometryResources, vpxyResources);
        }

        public void AddMeshGroup(int lod, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            var vpxy = new CmarNYCBorrowed.VPXY(new BinaryReader(vpxyResource.Stream));
            var geomTGIs = new TGI[4][];
            for (var i = 0; i < geomTGIs.GetLength(0); i++)
            {
                var geomTGIList = new List<TGI>(vpxy.MeshLinks(i));
                if (i == lod || lod == -1)
                {
                    var temp = "_lod" + i.ToString() + "-" + (geomTGIList.Count + 1).ToString();
                    var newGEOMTGI = new TGI(ResourceUtils.GetResourceType("GEOM"), geomTGIList[geomTGIList.Count - 1].Group, System.Security.Cryptography.FNV64.GetHash(CASPartResource.Unknown1 + temp + Environment.UserName + Environment.TickCount.ToString() + temp));
                    var geomStream = new MemoryStream();
                    var geom = geometryResources[new ResourceUtils.ResourceKey(geomTGIList[geomTGIList.Count - 1].Type, geomTGIList[geomTGIList.Count - 1].Group, geomTGIList[geomTGIList.Count - 1].Instance).ReverseEvaluateResourceKey()];
                    geom.Write(new BinaryWriter(geomStream));
                    var newGEOMResourceIndexEntry = ParentPackage.AddResource(new ResourceUtils.ResourceKey(newGEOMTGI.Type, newGEOMTGI.Group, newGEOMTGI.Instance), geomStream, true);
                    geometryResources.Add(newGEOMResourceIndexEntry.ReverseEvaluateResourceKey(), geom);
                    geomTGIList.Add(newGEOMTGI);
                }
                geomTGIs[i] = geomTGIList.ToArray();
            }
            var vpxyStream = new MemoryStream();
            new CmarNYCBorrowed.VPXY(new TGI(vpxyResourceIndexEntry.ResourceType, vpxyResourceIndexEntry.ResourceGroup, vpxyResourceIndexEntry.Instance), vpxy.BondLinks, geomTGIs).Write(new BinaryWriter(vpxyStream));
            vpxyResource = new GenericRCOLResource(0, vpxyStream);
            ParentPackage.ReplaceResource(vpxyResourceIndexEntry, vpxyResource);
            vpxyResources[vpxyKey] = vpxyResource;
        }

        public void DeleteMeshGroup(int lod, int groupIndex, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            var vpxy = new CmarNYCBorrowed.VPXY(new BinaryReader(vpxyResource.Stream));
            var geomTGIs = new TGI[4][];
            for (var i = 0; i < geomTGIs.GetLength(0); i++)
            {
                var geomTGIList = new List<TGI>(vpxy.MeshLinks(i));
                if (i == lod || lod == -1)
                {
                    var geomKey = new ResourceUtils.ResourceKey(geomTGIList[groupIndex].Type, geomTGIList[groupIndex].Group, geomTGIList[groupIndex].Instance).ReverseEvaluateResourceKey();
                    ParentPackage.DeleteResource(ParentPackage.EvaluateResourceKey(geomKey).ResourceIndexEntry);
                    geometryResources.Remove(geomKey);
                    geomTGIList.RemoveAt(groupIndex);
                }
                geomTGIs[i] = geomTGIList.ToArray();
            }
            var vpxyStream = new MemoryStream();
            new CmarNYCBorrowed.VPXY(new TGI(vpxyResourceIndexEntry.ResourceType, vpxyResourceIndexEntry.ResourceGroup, vpxyResourceIndexEntry.Instance), vpxy.BondLinks, geomTGIs).Write(new BinaryWriter(vpxyStream));
            vpxyResource = new GenericRCOLResource(0, vpxyStream);
            ParentPackage.ReplaceResource(vpxyResourceIndexEntry, vpxyResource);
            vpxyResources[vpxyKey] = vpxyResource;
        }

        public void AdjustPresetCount()
        {
            while (CASPartResource.Presets.Count < Presets.Count)
            {
                CASPartResource.Presets.Add(new CASPartResource.CASPartResource.Preset(0, null));
            }
            while (CASPartResource.Presets.Count > Presets.Count)
            {
                CASPartResource.Presets.RemoveAt(0);
            }
        }

        public void ClearCurrentRig()
        {
            CurrentRig = null;
        }

        public void LoadLODs(Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            foreach (var entry in ((s3pi.GenericRCOLResource.VPXY)vpxyResource.ChunkEntries[0].RCOLBlock).Entries)
            {
                var entry00 = entry as s3pi.GenericRCOLResource.VPXY.Entry00;
                if (entry00 != null)
                {
                    LODs[entry00.EntryID] = new List<GEOM>();
                    foreach (var tgiIndex in entry00.TGIIndexes)
                    {
                        var geometryResourceIndexEntry = ParentPackage.GetResourceIndexEntry(entry00.ParentTGIBlocks[tgiIndex]);
                        var geometryResourceKey = geometryResourceIndexEntry.ReverseEvaluateResourceKey();
                        GEOM geometryResource;
                        if (!geometryResources.TryGetValue(geometryResourceKey, out geometryResource))
                        {
                            geometryResources.Add(geometryResourceKey, new GEOM(new BinaryReader(((APackage)ParentPackage).GetResource(geometryResourceIndexEntry))));
                            geometryResource = geometryResources[geometryResourceKey];
                        }
                        LODs[entry00.EntryID].Add(geometryResource);
                    }
                }
            }
        }

        public void SaveDefaultPreset()
        {   
            if (DefaultPreset == null || DefaultPresetKey == null)
            {
                return;
            }
            var defaultPresetResourceIndexEntry = ParentPackage.EvaluateResourceKey(DefaultPresetKey).ResourceIndexEntry;
            var tempResourceIndexEntry = ParentPackage.AddResource(defaultPresetResourceIndexEntry, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(AllPresets[0].XmlFile.ReadToEnd())), false);
            ParentPackage.ReplaceResource(defaultPresetResourceIndexEntry, WrapperDealer.GetResource(0, ParentPackage, tempResourceIndexEntry));
            ParentPackage.DeleteResource(tempResourceIndexEntry);
        }

        public void SavePreset(int index)
        {
            CASPartResource.Presets[index].Unknown1 = (uint)index;
            CASPartResource.Presets[index].XmlFile = Presets[index].XmlFile;
        }

        public void SavePresets()
        {
            SaveDefaultPreset();
            AdjustPresetCount();
            for (var i = 0; i < CASPartResource.Presets.Count; i++)
            {
                SavePreset(i);
            }
        }
    }
}
