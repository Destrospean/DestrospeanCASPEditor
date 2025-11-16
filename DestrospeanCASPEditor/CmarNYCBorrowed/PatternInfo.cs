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

        public bool[] ChannelEnabled;

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
                if (Name.Contains("solidColor") || Name.Contains("Flat Color"))
                {
                    return PatternType.Solid;
                }
                else if (Name.Contains("None"))
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
    }
}
