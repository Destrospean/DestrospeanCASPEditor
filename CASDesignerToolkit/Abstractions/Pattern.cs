using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Destrospean.CmarNYCBorrowed;
using Destrospean.S3PIExtensions;

namespace Destrospean.CASDesignerToolkit.Abstractions
{
    public class Pattern : Complate
    {
        protected object mPatternImage;

        public override CASTableObject CASTableObject
        {
            get
            {
                return Preset.CASTableObject;
            }
        }

        public override s3pi.Interfaces.IPackage ParentPackage
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
            mXmlDocument.LoadXml(new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream).ReadToEnd());
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
                string key = propertyXmlNodeKvp.Key.ToLowerInvariant(),
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
}
