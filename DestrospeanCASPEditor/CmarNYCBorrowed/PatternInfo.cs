namespace Destrospean.CmarNYCBorrowed
{
    public enum PatternType
    {
        None,
        Colored,
        HSV,
        Solid
    }

    public class PatternInfo
    {
        float[][] mRGBColors;

        public string Background, Name, RGBMask;

        public string[] Channels;

        public bool[] ChannelsEnabled;

        public float[][] HSV, HSVBase, HSVShift;

        public float[] HSVBaseBG, HSVBG, HSVShiftBG, SolidColor;

        public float[][] RGBColors
        {
            get
            {
                var colors = new System.Collections.Generic.List<float[]>();
                for (var i = 0; i < mRGBColors.GetLength(0); i++)
                {
                    if (mRGBColors[i] != null)
                    {
                        colors.Add(mRGBColors[i]);
                    }
                }
                return colors.ToArray();
            }
            set
            {
                mRGBColors = value;
            }
        }

        public PatternType Type
        {
            get
            {
                if (SolidColor != null)
                {
                    return PatternType.Solid;
                }
                if (RGBColors.Length > 1)
                {
                    return PatternType.Colored;
                }
                if (HSVBaseBG != null || HSVBase != null || HSVBG != null || HSV != null || HSVShiftBG != null || HSVShift != null)
                {
                    return PatternType.HSV;
                }
                return PatternType.None;
            }
        }
    }
}
