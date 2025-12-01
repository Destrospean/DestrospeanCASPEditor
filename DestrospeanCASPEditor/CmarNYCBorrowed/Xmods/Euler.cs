namespace Destrospean.CmarNYCBorrowed
{
    public class Euler
    {
        float mX, mY, mZ;

        public float[] Rotation
        {
            get
            {
                return new float[]
                {
                    mX,
                    mY,
                    mZ
                };
            }
        }

        public float X
        {
            get
            {
                return mX;
            }
        }

        public float Y
        {
            get
            {
                return mY;
            }
        }

        public float Z
        {
            get
            {
                return mZ;
            }
        }

        public Euler(float x, float y, float z)
        {
            mX = x;
            mY = y;
            mZ = z;
        }
    }
}
