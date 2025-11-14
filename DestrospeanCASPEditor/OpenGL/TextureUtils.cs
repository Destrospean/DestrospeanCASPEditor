using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Destrospean.DestrospeanCASPEditor;

namespace Destrospean.CmarNYCBorrowed
{
    public static class TextureUtils
    {
        public class HSVColor
        {
            float mHue, mSaturation, mValue;

            public float Hue
            {
                get
                {
                    return mHue;
                }
                set
                {
                    mHue = value;
                    while (mHue > 360)
                    {
                        mHue -= 360;
                    }
                    while (mHue < 0)
                    {
                        mHue += 360;
                    }
                }
            }

            public float Saturation
            {
                get
                {
                    return mSaturation;
                }
                set
                {
                    mSaturation = value;
                    if (mSaturation > 1)
                    {
                        mSaturation = 1;
                    }
                    if (mSaturation < 0)
                    {
                        mSaturation = 0;
                    }
                }
            }

            public float Value
            {
                get
                {
                    return mValue;
                }
                set
                {
                    mValue = value;
                    if (mValue > 1)
                    {
                        mValue = 1;
                    }
                    if (mValue < 0)
                    {
                        mValue = 0;
                    }
                }
            }

            public HSVColor(float hue, float saturation, float value)
            {
                mHue = hue;
                mSaturation = saturation;
                mValue = value;
            }

            public HSVColor(float hue, float saturation, float value, bool corrected)
            {
                Hue = hue;
                Saturation = saturation;
                Value = value;
            }

            /// <summary>
            /// Create HSV color from red, green, blue values
            /// </summary>
            /// <param name="r">0-255</param>
            /// <param name="g">0-255</param>
            /// <param name="b">0-255</param>
            /// <returns></returns>
            public HSVColor(byte r, byte g, byte b)
            {
                double blue = (double)b / byte.MaxValue,
                green = (double)g / byte.MaxValue,
                hue = 0,
                red = (double)r / byte.MaxValue,
                subtrahend = Math.Min(Math.Min(red, green), blue),
                value = Math.Max(Math.Max(red, green), blue);
                if (subtrahend != value)
                {
                    if (red == value && green >= blue)
                    {
                        hue = 60 * (green - blue) / (value - subtrahend) + 0;
                    }
                    else if (red == value && green < blue)
                    {
                        hue = 60 * (green - blue) / (value - subtrahend) + 360;
                    }
                    else if (green == value)
                    {
                        hue = 60 * (blue - red) / (value - subtrahend) + 120;
                    }
                    else if (blue == value)
                    {
                        hue = 60 * (red - green) / (value - subtrahend) + 240;
                    }
                }
                Hue = (float)hue;
                Value = (float)value;
                if (value == 0)
                {
                    Saturation = 0;
                }
                else
                {
                    Saturation = (float)((value - subtrahend) / value);
                }
            }

            /// <summary>
            /// Returns byte array of red (0-255), green (0-255), blue (0-255)
            /// </summary>
            public byte[] ToRGBColor()
            {
                double blue,
                green,
                hue = mHue,
                red;
                while (hue < 0)
                {
                    hue += 360;
                }
                while (hue >= 360)
                {
                    hue -= 360;
                }
                if (mValue <= 0)
                {
                    double channel;
                    blue = channel = 0;
                    green = channel;
                    red = channel;
                }
                else if (mSaturation <= 0)
                {
                    double channel;
                    blue = channel = mValue;
                    green = channel;
                    red = channel;
                }
                else
                {
                    double hueDividedBy60 = hue / 60,
                    roundedDown = Math.Floor(hueDividedBy60),
                    modifier = hueDividedBy60 - roundedDown,
                    channel0 = mValue * (1.0 - mSaturation),
                    channel1 = mValue * (1.0 - mSaturation * modifier),
                    channel2 = mValue * (1.0 - mSaturation * (1.0 - modifier));
                    switch ((int)roundedDown)
                    {
                        case -1:
                            red = mValue;
                            green = channel0;
                            blue = channel1;
                            break;
                        case 0:
                            red = mValue;
                            green = channel2;
                            blue = channel0;
                            break;
                        case 1:
                            red = channel1;
                            green = mValue;
                            blue = channel0;
                            break;
                        case 2:
                            red = channel0;
                            green = mValue;
                            blue = channel2;
                            break;
                        case 3:
                            red = channel0;
                            green = channel1;
                            blue = mValue;
                            break;
                        case 4:
                            red = channel2;
                            green = channel0;
                            blue = mValue;
                            break;
                        case 5:
                            red = mValue;
                            green = channel0;
                            blue = channel1;
                            break;
                        case 6:
                            red = mValue;
                            green = channel2;
                            blue = channel0;
                            break;
                        default:
                            double channel;
                            blue = channel = mValue;
                            green = channel;
                            red = channel;
                            break;
                    }
                }
                return new byte[]
                {
                    Clamp((int)(red * byte.MaxValue)), Clamp((int)(green * byte.MaxValue)), Clamp((int)(blue * byte.MaxValue))
                };
            }

            static byte Clamp(int number)
            {
                return (byte)(number < 0 ? 0 : number > byte.MaxValue ? byte.MaxValue : number);
            }

            public static HSVColor operator +(HSVColor hsv1, HSVColor hsv2)
            {
                return new HSVColor(hsv1.mHue + hsv2.mHue, hsv1.mSaturation + hsv2.mSaturation, hsv1.mValue + hsv2.mValue, true);
            }

            public static HSVColor operator -(HSVColor hsv1, HSVColor hsv2)
            {
                return new HSVColor(hsv1.mHue - hsv2.mHue, hsv1.mSaturation - hsv2.mSaturation, hsv1.mValue - hsv2.mValue, true);
            }
        }

        public class PatternInfo
        {
            float[][] mRGBColors;

            public string Background, PatternName, RGBMask;

            public string[] Channels;

            public bool[] ChannelEnabled;

            public float[][] HSV, HSVBase, HSVShift;

            public float[] HSVBaseBG, HSVBG, HSVShiftBG, SolidColor;

            public enum PatternType
            {
                Solid,
                Colored,
                HSV,
                None
            }

            public float[][] RGBColors
            {
                get
                {
                    var colors = new List<float[]>();
                    for (var i = 0; i < mRGBColors.GetLength(0); i++)
                    {
                        if (mRGBColors[i] != null)
                        {
                            colors.Add(mRGBColors[i]);
                        }
                    }
                    return colors.ToArray();
                }
            }

            public PatternType Type
            {
                get
                {
                    if (PatternName.Contains("solidColor") || PatternName.Contains("Flat Color"))
                    {
                        return PatternType.Solid;
                    }
                    else if (PatternName.Contains("None"))
                    {
                        return PatternType.None;
                    }
                    else if (HSVBaseBG != null || HSVBase != null || HSVBG != null || HSV != null || HSVShiftBG != null || HSVShift != null)
                    {
                        return PatternType.HSV;
                    }
                    else
                    {
                        return PatternType.Colored;
                    }
                }
            }

            public string PrintFloatArray(float[] values)
            {
                var text = "";
                for (var i = 0; i < SolidColor.Length; i++)
                {
                    text += SolidColor[i].ToString();
                    if (i < SolidColor.Length - 1)
                    {
                        text += ", ";
                    }
                }
                return text;
            }
        }

        public static Image DisplayableTexture(Bitmap multiplier, uint[] maskTexture, List<object> patterns, bool overlay)
        {
            var rectangle = new Rectangle(0, 0, multiplier.Width, multiplier.Height);
            var bitmapData = multiplier.LockBits(rectangle, ImageLockMode.ReadWrite, multiplier.PixelFormat);
            var ptr = bitmapData.Scan0 + (bitmapData.Stride > 0 ? 0 : bitmapData.Stride * (multiplier.Height - 1));
            var byteCount = Math.Abs(bitmapData.Stride) * multiplier.Height;
            var multiplierArray = new byte[byteCount];
            Marshal.Copy(ptr, multiplierArray, 0, byteCount);
            for (var i = 0; i < byteCount; i += 4)
            {
                var gray = (multiplierArray[i] + multiplierArray[i + 1] + multiplierArray[i + 2]) / 3f / (byte.MaxValue >> (overlay ? 0 : 1));
                byte[] mask = BitConverter.GetBytes(maskTexture[i >> 2]),
                maskControl = new byte[]
                    {
                        mask[2],
                        mask[1],
                        mask[0],
                        mask[3]
                    };
                for (var j = 0; j < patterns.Count; j++)
                {
                    var blend = (float)maskControl[j] / byte.MaxValue;
                    if (patterns[j] != null && maskControl[j] > 0)
                    {
                        var rgba = patterns[j] as float[];
                        if (rgba != null)
                        {
                            for (var k = 0; k < 3; k++)
                            {
                                var temp = gray * rgba[2 - k];
                                if (temp < 0)
                                {
                                    temp = 0;
                                }
                                if (temp > 1)
                                {
                                    temp = 1;
                                }
                                if (k == 0 || !overlay)
                                {
                                    multiplierArray[i + k] = (byte)(temp * byte.MaxValue);
                                }
                                else
                                {
                                    multiplierArray[i + k] = (byte)((blend * temp + ((1 - blend) * multiplierArray[i + k] / byte.MaxValue)) * byte.MaxValue);
                                }
                            }
                            continue;
                        }
                        var image = patterns[j] as Bitmap;
                        if (image != null)
                        {
                            int currentX,
                            currentY = Math.DivRem(i / 4, multiplier.Width, out currentX),
                            height,
                            width;
                            Math.DivRem(currentX, image.Width, out width);
                            Math.DivRem(currentY, image.Height, out height);
                            var color = image.GetPixel(width, height);
                            rgba = new float[]
                                {
                                    (float)color.R / byte.MaxValue,
                                    (float)color.G / byte.MaxValue,
                                    (float)color.B / byte.MaxValue,
                                    (float)color.A / byte.MaxValue
                                };
                            for (var k = 0; k < 3; k++)
                            {
                                var temp = gray * rgba[2 - k];
                                if (temp < 0)
                                {
                                    temp = 0;
                                }
                                if (temp > 1)
                                {
                                    temp = 1;
                                }
                                if (k == 0 || !overlay)
                                {
                                    multiplierArray[i + k] = (byte)(temp * byte.MaxValue);
                                }
                                else
                                {
                                    multiplierArray[i + k] = (byte)((blend * temp + ((1 - blend) * multiplierArray[i + k] / byte.MaxValue)) * byte.MaxValue);
                                }
                            }
                        }
                    }
                }
            }
            Marshal.Copy(multiplierArray, 0, ptr, byteCount);
            multiplier.UnlockBits(bitmapData);
            return multiplier;
        }

        public static Bitmap DisplayableRGBPattern(PatternInfo pattern)
        {
            int height = 256,
            width = 256;
            var colors = pattern.RGBColors;
            var texture = new Bitmap(width, height);
            var rectangle = new Rectangle(0, 0, width, height);
            var bitmapData = texture.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var ptr = bitmapData.Scan0 + (bitmapData.Stride > 0 ? 0 : bitmapData.Stride * (texture.Height - 1));
            var byteCount = Math.Abs(bitmapData.Stride) * texture.Height;
            byte[] maskArray = GetImageARGBArray(pattern.RGBMask, width, height), 
            textureArray = new byte[byteCount];
            for (var i = 0; i < maskArray.Length; i++)
            {
                byte[] mask = BitConverter.GetBytes(maskArray[i]),
                maskControl = new byte[]
                    {
                        mask[2],
                        mask[1],
                        mask[0],
                        mask[3]
                    };
                for (var j = 0; j < colors.GetLength(0); j++)
                {
                    if (colors[j] != null && maskControl[j] > 0)
                    {
                        var blend = (float)maskControl[j] / byte.MaxValue;
                        for (var k = 0; k < 3; k++)
                        {
                            var temp = j == 0 ? colors[j][2 - k] : blend * colors[j][2 - k] + (1 - blend) * textureArray[i * 4 + k] / byte.MaxValue;
                            if (temp < 0)
                            {
                                temp = 0;
                            }
                            if (temp > 1)
                            {
                                temp = 1;
                            }
                            textureArray[i * 4 + k] = (byte)(temp * byte.MaxValue);
                        }
                    }
                }
                textureArray[i * 4 + 3] = byte.MaxValue;
            }
            Marshal.Copy(textureArray, 0, ptr, byteCount);
            texture.UnlockBits(bitmapData);
            return texture;
        }

        public static Bitmap DisplayableHSVPattern(PatternInfo pattern)
        {
            int height = 256,
            width = 256;
            Bitmap background = GetImage(pattern.Background, width, height),
            patternImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rgb = GetImageARGBArray(pattern.RGBMask, width, height);
            var patternBack = new Bitmap[3];
            if (pattern.Channels != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (pattern.Channels[i] != null)
                    {
                        patternBack[i] = GetImage(pattern.Channels[i], width, height);
                    }
                }
            }
            BitmapData bitmapData0 = patternImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternImage.PixelFormat),
            bitmapData1 = background.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, background.PixelFormat),
            bitmapData2 = null,
            bitmapData3 = null,
            bitmapData4 = null;
            IntPtr ptr0 = bitmapData0.Scan0 + (bitmapData0.Stride > 0 ? 0 : bitmapData0.Stride * (patternImage.Height - 1)),
            ptr1 = bitmapData1.Scan0 + (bitmapData1.Stride > 0 ? 0 : bitmapData1.Stride * (background.Height - 1));
            byte[] alphaArray = null,
            backArray = new byte[Math.Abs(bitmapData1.Stride) * background.Height],
            blueArray = null,
            finalArray = new byte[Math.Abs(bitmapData0.Stride) * patternImage.Height],
            greenArray = null;
            Marshal.Copy(ptr0, finalArray, 0, finalArray.Length);
            Marshal.Copy(ptr1, backArray, 0, backArray.Length);
            if (pattern.ChannelEnabled[0])
            {
                bitmapData2 = patternBack[0].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[0].PixelFormat);
                var ptr = bitmapData2.Scan0 + (bitmapData2.Stride > 0 ? 0 : bitmapData2.Stride * (patternBack[0].Height - 1));
                greenArray = new byte[Math.Abs(bitmapData2.Stride) * patternBack[0].Height];
                Marshal.Copy(ptr, greenArray, 0, greenArray.Length);
            }
            if (pattern.ChannelEnabled[1])
            {
                bitmapData3 = patternBack[1].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[1].PixelFormat);
                var ptr = bitmapData3.Scan0 + (bitmapData3.Stride > 0 ? 0 : bitmapData3.Stride * (patternBack[1].Height - 1));
                blueArray = new byte[Math.Abs(bitmapData3.Stride) * patternBack[1].Height];
                Marshal.Copy(ptr, blueArray, 0, blueArray.Length);
            }
            if (pattern.ChannelEnabled[2])
            {
                bitmapData4 = patternBack[2].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[2].PixelFormat);
                var ptr = bitmapData4.Scan0 + (bitmapData4.Stride > 0 ? 0 : bitmapData4.Stride * (patternBack[2].Height - 1));
                alphaArray = new byte[Math.Abs(bitmapData4.Stride) * patternBack[2].Height];
                Marshal.Copy(ptr, alphaArray, 0, alphaArray.Length);
            }
            HSVColor alphaChannel = pattern.HSV == null || pattern.HSV[2] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[2][0] * 360, pattern.HSV[2][1], pattern.HSV[2][2]),
            backChannel = pattern.HSVBG == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSVBG[0] * 360, pattern.HSVBG[1], pattern.HSVBG[2]),
            blueChannel = pattern.HSV == null || pattern.HSV[1] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[1][0] * 360, pattern.HSV[1][1], pattern.HSV[1][2]),
            greenChannel = pattern.HSV == null || pattern.HSV[0] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[0][0] * 360, pattern.HSV[0][1], pattern.HSV[0][2]);
            for (var i = 0; i < finalArray.Length; i += 4)
            {
                var hsv = new HSVColor(backArray[i + 2], backArray[i + 1], backArray[i]);
                byte[] color = (hsv + backChannel).ToRGBColor(),
                maskArray = BitConverter.GetBytes(rgb[i >> 2]);
                if (pattern.ChannelEnabled[0] && maskArray[1] > 0) // green channel
                {
                    var tempHSV = new HSVColor(greenArray[i + 2], greenArray[i + 1], greenArray[i]);
                    var tempColor = (tempHSV + greenChannel).ToRGBColor();
                    var weight = (float)maskArray[1] / byte.MaxValue;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                if (pattern.ChannelEnabled[1] && maskArray[0] > 0) // blue channel
                {
                    var tempHSV = new HSVColor(blueArray[i + 2], blueArray[i + 1], blueArray[i]);
                    var tempColor = (tempHSV + blueChannel).ToRGBColor();
                    var weight = (float)maskArray[0] / byte.MaxValue;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                if (pattern.ChannelEnabled[2] && maskArray[3] > 0) // alpha channel
                {
                    var tempHSV = new HSVColor(alphaArray[i + 2], alphaArray[i + 1], alphaArray[i]);
                    var tempColor = (tempHSV + alphaChannel).ToRGBColor();
                    var weight = (float)maskArray[3] / byte.MaxValue;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                finalArray[i + 2] = color[0];
                finalArray[i + 1] = color[1];
                finalArray[i] = color[2];
            }
            Marshal.Copy(finalArray, 0, ptr0, finalArray.Length);
            patternImage.UnlockBits(bitmapData0);
            background.UnlockBits(bitmapData1);
            if (pattern.ChannelEnabled[0])
            {
                patternBack[0].UnlockBits(bitmapData2);
            }
            if (pattern.ChannelEnabled[1])
            {
                patternBack[1].UnlockBits(bitmapData3);
            }
            if (pattern.ChannelEnabled[2])
            {
                patternBack[2].UnlockBits(bitmapData4);
            }
            return patternImage;
        }

        public static Bitmap GetImage(string key, int[] dimensions)
        {
            Bitmap image;
            if (!ImageUtils.PreloadedGameImages.TryGetValue(key, out image) && !ImageUtils.PreloadedImages.TryGetValue(key, out image))
            {
                var evaluated = ResourceUtils.EvaluateImageResourceKey(MainWindow.Singleton.CurrentPackage, key);
                image = GDImageLibrary._DDS.LoadImage(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Item1, evaluated.Item2).AsBytes);
                if (evaluated.Item1 == MainWindow.Singleton.CurrentPackage)
                {
                    ImageUtils.PreloadedImages.Add(key, image);
                }
                else
                {
                    ImageUtils.PreloadedGameImages.Add(key, image);
                }
            }
            if (dimensions != null && dimensions[0] != image.Size.Width && dimensions[1] != image.Size.Height)
            {
                image = new Bitmap(image, new Size(dimensions[0], dimensions[1]));
            }
            return image;
        }

        public static Bitmap GetImage(string key, int width, int height)
        {
            return GetImage(key, new int[]
                {
                    width,
                    height
                });
        }

        public static byte[] GetImageARGBArray(string key, int[] dimensions)
        {
            var image = GetImage(key, dimensions);
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var ptr = bitmapData.Scan0;
            var byteCount = Math.Abs(bitmapData.Stride) * image.Height;
            byte[] bgraValues = new byte[byteCount],
            argbValues = new byte[byteCount];
            Marshal.Copy(ptr, bgraValues, 0, byteCount);
            image.UnlockBits(bitmapData);
            for (var i = 0; i < byteCount; i += 4)
            {
                argbValues[i] = bgraValues[i + 3];
                argbValues[i + 1] = bgraValues[i + 2];
                argbValues[i + 2] = bgraValues[i + 1];
                argbValues[i + 3] = bgraValues[i];
            }
            return argbValues;
        }

        public static byte[] GetImageARGBArray(string key, int width, int height)
        {
            return GetImageARGBArray(key, new int[]
                {
                    width,
                    height
                });
        }
    }
}
