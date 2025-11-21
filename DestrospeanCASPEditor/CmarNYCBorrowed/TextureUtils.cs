using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Destrospean.DestrospeanCASPEditor;
using s3pi.Interfaces;

namespace Destrospean.CmarNYCBorrowed
{
    public static class TextureUtils
    {
        const float kInverseByteMax = 1f / byte.MaxValue,
        kOneThirdInverseByteMax = kInverseByteMax / 3;

        public static Bitmap GetHSVPatternImage(this IPackage package, PatternInfo pattern)
        {
            int height = 256,
            width = 256;
            Bitmap background = package.GetTexture(pattern.Background, width, height),
            patternImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rgb = package.GetTextureARGBArray(pattern.RGBMask, width, height);
            var patternBack = new Bitmap[3];
            for (var i = 0; i < 3 && pattern.Channels != null; i++)
            {
                if (pattern.Channels[i] != null)
                {
                    patternBack[i] = package.GetTexture(pattern.Channels[i], width, height);
                }
            }
            BitmapData bitmapData0 = patternImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternImage.PixelFormat),
            bitmapData1 = background.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, background.PixelFormat),
            bitmapData2 = null,
            bitmapData3 = null,
            bitmapData4 = null;
            byte[] alphaArray = null,
            backArray = new byte[Math.Abs(bitmapData1.Stride) * background.Height],
            blueArray = null,
            finalArray = new byte[Math.Abs(bitmapData0.Stride) * patternImage.Height],
            greenArray = null;
            var ptr = bitmapData0.Scan0 + (bitmapData0.Stride > 0 ? 0 : bitmapData0.Stride * (patternImage.Height - 1));
            Marshal.Copy(ptr, finalArray, 0, finalArray.Length);
            Marshal.Copy(bitmapData1.Scan0 + (bitmapData1.Stride > 0 ? 0 : bitmapData1.Stride * (background.Height - 1)), backArray, 0, backArray.Length);
            if (pattern.ChannelEnabled[0])
            {
                bitmapData2 = patternBack[0].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[0].PixelFormat);
                greenArray = new byte[Math.Abs(bitmapData2.Stride) * patternBack[0].Height];
                Marshal.Copy(bitmapData2.Scan0 + (bitmapData2.Stride > 0 ? 0 : bitmapData2.Stride * (patternBack[0].Height - 1)), greenArray, 0, greenArray.Length);
            }
            if (pattern.ChannelEnabled[1])
            {
                bitmapData3 = patternBack[1].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[1].PixelFormat);
                blueArray = new byte[Math.Abs(bitmapData3.Stride) * patternBack[1].Height];
                Marshal.Copy(bitmapData3.Scan0 + (bitmapData3.Stride > 0 ? 0 : bitmapData3.Stride * (patternBack[1].Height - 1)), blueArray, 0, blueArray.Length);
            }
            if (pattern.ChannelEnabled[2])
            {
                bitmapData4 = patternBack[2].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, patternBack[2].PixelFormat);
                alphaArray = new byte[Math.Abs(bitmapData4.Stride) * patternBack[2].Height];
                Marshal.Copy(bitmapData4.Scan0 + (bitmapData4.Stride > 0 ? 0 : bitmapData4.Stride * (patternBack[2].Height - 1)), alphaArray, 0, alphaArray.Length);
            }
            HSVColor alphaChannel = pattern.HSV == null || pattern.HSV[2] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[2][0] * 360, pattern.HSV[2][1], pattern.HSV[2][2]),
            backChannel = pattern.HSVBG == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSVBG[0] * 360, pattern.HSVBG[1], pattern.HSVBG[2]),
            blueChannel = pattern.HSV == null || pattern.HSV[1] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[1][0] * 360, pattern.HSV[1][1], pattern.HSV[1][2]),
            greenChannel = pattern.HSV == null || pattern.HSV[0] == null ? new HSVColor(0, 0, 0) : new HSVColor(pattern.HSV[0][0] * 360, pattern.HSV[0][1], pattern.HSV[0][2]);
            for (var i = 0; i < finalArray.Length; i += 4)
            {
                var hsv = new HSVColor(backArray[i + 2], backArray[i + 1], backArray[i]);
                byte[] color = (hsv + backChannel).AsRGB,
                maskArray = BitConverter.GetBytes(rgb[i >> 2]);
                if (pattern.ChannelEnabled[0] && maskArray[1] > 0)
                {
                    var tempHSV = new HSVColor(greenArray[i + 2], greenArray[i + 1], greenArray[i]);
                    var tempColor = (tempHSV + greenChannel).AsRGB;
                    var weight = maskArray[1] * kInverseByteMax;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                if (pattern.ChannelEnabled[1] && maskArray[0] > 0)
                {
                    var tempHSV = new HSVColor(blueArray[i + 2], blueArray[i + 1], blueArray[i]);
                    var tempColor = (tempHSV + blueChannel).AsRGB;
                    var weight = maskArray[0] * kInverseByteMax;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                if (pattern.ChannelEnabled[2] && maskArray[3] > 0)
                {
                    var tempHSV = new HSVColor(alphaArray[i + 2], alphaArray[i + 1], alphaArray[i]);
                    var tempColor = (tempHSV + alphaChannel).AsRGB;
                    var weight = maskArray[3] * kInverseByteMax;
                    for (var j = 0; j < 3; j++)
                    {
                        color[j] = (byte)(tempColor[j] * weight + color[j] * (1 - weight));
                    }
                }
                finalArray[i + 2] = color[0];
                finalArray[i + 1] = color[1];
                finalArray[i] = color[2];
            }
            Marshal.Copy(finalArray, 0, ptr, finalArray.Length);
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

        public static Bitmap GetRGBPatternImage(this IPackage package, PatternInfo pattern)
        {
            int height = 256,
            width = 256;
            var colors = pattern.RGBColors;
            var texture = new Bitmap(width, height);
            var rectangle = new Rectangle(0, 0, width, height);
            var bitmapData = texture.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var byteCount = Math.Abs(bitmapData.Stride) * texture.Height;
            var maskArray = package.GetTextureARGBArray(pattern.RGBMask, width, height);
            var textureArray = new byte[byteCount];
            for (var i = 0; i < maskArray.Length; i += 4)
            {
                byte[] mask = BitConverter.GetBytes(maskArray[i >> 2]),
                maskControl =
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
                        var blend = maskControl[j] * kInverseByteMax;
                        for (var k = 0; k < 3; k++)
                        {
                            var temp = j == 0 ? colors[j][2 - k] : blend * colors[j][2 - k] + (1 - blend) * textureArray[i + k] * kInverseByteMax;
                            temp = temp < 0 ? 0 : temp > 1 ? 1 : temp;
                            textureArray[i + k] = (byte)(temp * byte.MaxValue);
                        }
                    }
                }
                textureArray[i + 3] = byte.MaxValue;
            }
            Marshal.Copy(textureArray, 0, bitmapData.Scan0 + (bitmapData.Stride > 0 ? 0 : bitmapData.Stride * (texture.Height - 1)), byteCount);
            texture.UnlockBits(bitmapData);
            return texture;
        }

        public static Bitmap GetTexture(this IPackage package, string key, int[] dimensions = null)
        {
            Bitmap image;
            if (!ImageUtils.PreloadedGameImages.TryGetValue(key, out image) && !ImageUtils.PreloadedImages.TryGetValue(key, out image))
            {
                var evaluated = package.EvaluateImageResourceKey(key);
                image = GDImageLibrary._DDS.LoadImage(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).AsBytes);
                if (evaluated.Package == package)
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
            return (Bitmap)image.Clone();
        }

        public static Bitmap GetTexture(this IPackage package, string key, int width, int height)
        {
            return package.GetTexture(key, new int[]
                {
                    width,
                    height
                });
        }

        public static uint[] GetTextureARGBArray(this IPackage package, string key, int[] dimensions = null)
        {
            var image = package.GetTexture(key, dimensions);
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var byteCount = Math.Abs(bitmapData.Stride) * image.Height;
            var bgraValues = new byte[byteCount];
            var argbValues = new uint[byteCount];
            Marshal.Copy(bitmapData.Scan0, bgraValues, 0, byteCount);
            image.UnlockBits(bitmapData);
            for (int i = 0, j = 0; i < byteCount; i += 4, j++)
            {
                argbValues[j] = ((uint)bgraValues[i + 3] << 24) + ((uint)bgraValues[i + 2] << 16) + ((uint)bgraValues[i + 1] << 8) + bgraValues[i];
            }
            return argbValues;
        }

        public static uint[] GetTextureARGBArray(this IPackage package, string key, int width, int height)
        {
            return package.GetTextureARGBArray(key, new int[]
                {
                    width,
                    height
                });
        }

        public static Bitmap GetWithPatternsApplied(this Bitmap multiplier, uint[] maskArray, System.Collections.Generic.List<object> patternImages, bool overlay)
        {
            var multiplierCopy = (Bitmap)multiplier.Clone();
            var rectangle = new Rectangle(0, 0, multiplierCopy.Width, multiplierCopy.Height);
            var bitmapData = multiplierCopy.LockBits(rectangle, ImageLockMode.ReadWrite, multiplierCopy.PixelFormat);
            try
            {
                var byteCount = Math.Abs(bitmapData.Stride) * multiplierCopy.Height;
                var multiplierArray = new byte[byteCount];
                var ptr = bitmapData.Scan0 + (bitmapData.Stride > 0 ? 0 : bitmapData.Stride * (multiplierCopy.Height - 1));
                Marshal.Copy(ptr, multiplierArray, 0, byteCount);
                for (var i = 0; i < byteCount; i += 4)
                {
                    var gray = (multiplierArray[i] + multiplierArray[i + 1] + multiplierArray[i + 2]) * kOneThirdInverseByteMax * (overlay ? 1 : 2);
                    byte[] mask = BitConverter.GetBytes(maskArray[i >> 2]),
                    maskControl =
                        {
                            mask[2],
                            mask[1],
                            mask[0],
                            mask[3]
                        };
                    for (var j = 0; j < patternImages.Count - 1; j++)
                    {
                        var blend = maskControl[j] * kInverseByteMax;
                        if (patternImages[j] != null && maskControl[j] > 0)
                        {
                            var rgba = patternImages[j] as float[];
                            if (rgba != null)
                            {
                                for (var k = 0; k < 3; k++)
                                {
                                    var temp = gray * rgba[2 - k];
                                    temp = temp < 0 ? 0 : temp > 1 ? 1 : temp;
                                    if (k == 0 || !overlay)
                                    {
                                        multiplierArray[i + k] = (byte)(temp * byte.MaxValue);
                                    }
                                    else
                                    {
                                        multiplierArray[i + k] = (byte)((blend * temp + (1 - blend) * (multiplierArray[i + k] >> 8)) * byte.MaxValue);
                                    }
                                }
                                continue;
                            }
                            var image = patternImages[j] as Bitmap;
                            if (image != null)
                            {
                                int currentX,
                                currentY = Math.DivRem(i >> 2, multiplierCopy.Width, out currentX),
                                height,
                                width;
                                Math.DivRem(currentX, image.Width, out width);
                                Math.DivRem(currentY, image.Height, out height);
                                var color = image.GetPixel(width, height);
                                rgba = new float[]
                                    {
                                        color.R * kInverseByteMax,
                                        color.G * kInverseByteMax,
                                        color.B * kInverseByteMax,
                                        color.A * kInverseByteMax
                                    };
                                for (var k = 0; k < 3; k++)
                                {
                                    var temp = gray * rgba[2 - k];
                                    temp = temp < 0 ? 0 : temp > 1 ? 1 : temp;
                                    if (k == 0 || !overlay)
                                    {
                                        multiplierArray[i + k] = (byte)(temp * byte.MaxValue);
                                    }
                                    else
                                    {
                                        multiplierArray[i + k] = (byte)((blend * temp + (1 - blend) * (multiplierArray[i + k] >> 8)) * byte.MaxValue);
                                    }
                                }
                            }
                        }
                    }
                }
                Marshal.Copy(multiplierArray, 0, ptr, byteCount);
                multiplierCopy.UnlockBits(bitmapData);
            }
            catch (IndexOutOfRangeException ex)
            {
                Console.WriteLine(ex);
                multiplierCopy.UnlockBits(bitmapData);
                return multiplier;
            }
            return multiplierCopy;
        }
    }
}
