using System;

namespace Destrospean.CmarNYCBorrowed
{
    public class Euler
    {
        float mX, mY, mZ;

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

        public float[] xyzRotation
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

        public Euler(float x, float y, float z)
        {
            mX = x;
            mY = y;
            mZ = z;
        }
    }

    public class Quaternion
    {
        float mW, mX, mY, mZ;

        public float[] Coordinates
        {
            get
            {
                return new float[]
                {
                    mX,
                    mY,
                    mZ,
                    mW
                };
            }
        }

        public float W
        {
            get
            {
                return mW;
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

        public Quaternion(float x, float y, float z, float w)
        {
            mX = x;
            mY = y;
            mZ = z;
            mW = w;
        }

        public Quaternion(float[] quat)
        {
            mX = quat[0];
            mY = quat[1];
            mZ = quat[2];
            mW = quat[3];
        }

        public static Quaternion Identity
        {
            get
            {
                return new Quaternion(0, 0, 0, 1);
            }
        }

        public static Quaternion RotateYUpToZUp
        {
            get
            {
                return new Quaternion(0.7071068f, 0, 0, 0.7071068f);
            }
        }

        public bool IsEmpty
        {
            get
            {
                return mX == 0d && mY == 0d && mZ == 0d && (mW == 1d || mW == 0d);
            }
        }

        public bool IsIdentity
        {
            get
            {
                return mX == 0d && mY == 0d && mZ == 0d && mW == 1d;
            }
        }

        public bool IsNormalized
        {
            get
            {
                double magnitude = mX * mX + mY * mY + mZ * mZ + mW * mW;
                return (Math.Abs(magnitude - 1d) <= .000001d);
            }
        }

        public void Normalize()
        {
            var magnitude = Math.Sqrt(mX * mX + mY * mY + mZ * mZ + mW * mW);
            mX = (float)(mX / magnitude);
            mY = (float)(mY / magnitude);
            mZ = (float)(mZ / magnitude);
            mW = (float)(mW / magnitude);
        }

        public void Balance()
        {
            double m = mX * mX - mY * mY - mZ * mZ;
            if (m <= 1d)
            {
                mW = (float)Math.Sqrt(1d - m);
            }
            else
            {
                Normalize();
            }
        }

        public static Quaternion operator *(Quaternion q, Quaternion r)
        {
            return new Quaternion(r.mW * q.mX + r.mX * q.mW - r.mY * q.mZ + r.mZ * q.mY,
                r.mW * q.mY + r.mX * q.mZ + r.mY * q.mW - r.mZ * q.mX,
                r.mW * q.mZ - r.mX * q.mY + r.mY * q.mX + r.mZ * q.mW,
                r.mW * q.mW - r.mX * q.mX - r.mY * q.mY - r.mZ * q.mZ);
        }

        public static Quaternion operator *(Quaternion q, float f)
        {
            var temp = new Quaternion(q.mX * f, q.mY * f, q.mZ * f, q.mW);
            temp.Normalize();
            return temp;
        }

        public static Quaternion operator *(Quaternion q, Vector3 v)
        {
            var temp = new Quaternion(v.X, v.Y, v.Z, 0);
            return q * temp;
        }

        public Quaternion Conjugate()
        {
            return new Quaternion(-mX, -mY, -mZ, mW);
        }

        public Quaternion Inverse()
        {
            float norm = mX * mX + mY * mY + mZ * mZ + mW * mW;
            if (norm > 0f)
            {
                Quaternion q = new Quaternion(-mX / norm, -mY / norm, -mZ / norm, mW / norm);
                q.Normalize();
                return q;
            }
            else
            {
                return Quaternion.Identity;
            }
        }

        public Euler ToEuler()
        {
            Quaternion q = this;
            float[] res = new float[3];
            res[0] = (float)Math.Atan2(2 * (q.mY * q.mZ + q.mW * q.mX), q.mW * q.mW - q.mX * q.mX - q.mY * q.mY + q.mZ * q.mZ);
            double r21 = -2 * (q.mX * q.mZ - q.mW * q.mY);
            if (r21 < -1d)
            {
                r21 = -1;
            }
            if (r21 > 1d)
            {
                r21 = 1;
            }
            res[1] = (float)Math.Asin(r21);
            res[2] = (float)Math.Atan2(2 * (q.mX * q.mY + q.mW * q.mZ), q.mW * q.mW + q.mX * q.mX - q.mY * q.mY - q.mZ * q.mZ);
            Euler temp = new Euler(res[0], res[1], res[2]);
            return temp;
        }

        public Matrix3D ToMatrix3D()
        {
            var matrix = new float[3, 3];
            matrix[0, 0] = 1f - (2f * mY * mY) - (2f * mZ * mZ);
            matrix[0, 1] = (2f * mX * mY) - (2f * mZ * mW);
            matrix[0, 2] = (2f * mX * mZ) + (2f * mY * mW);
            matrix[1, 0] = (2f * mX * mY) + (2f * mZ * mW);
            matrix[1, 1] = 1f - (2f * mX * mX) - (2f * mZ * mZ);
            matrix[1, 2] = (2f * mY * mZ) - (2f * mX * mW);
            matrix[2, 0] = (2f * mX * mZ) - (2f * mY * mW);
            matrix[2, 1] = (2f * mY * mZ) + (2f * mX * mW);
            matrix[2, 2] = 1f - (2f * mX * mX) - (2f * mY * mY);
            return new Matrix3D(matrix);
        }

        public Matrix4D ToMatrix4D()
        {
            return ToMatrix4D(new Vector3(0, 0, 0));
        }

        public Matrix4D ToMatrix4D(Vector3 offset)
        {
            double[,] matrix = new double[4, 4];
            matrix[0, 0] = 1d - (2d * mY * mY) - (2d * mZ * mZ);
            matrix[0, 1] = (2d * mX * mY) - (2d * mZ * mW);
            matrix[0, 2] = (2d * mX * mZ) + (2d * mY * mW);
            matrix[0, 3] = offset.X;
            matrix[1, 0] = (2d * mX * mY) + (2d * mZ * mW);
            matrix[1, 1] = 1d - (2d * mX * mX) - (2d * mZ * mZ);
            matrix[1, 2] = (2d * mY * mZ) - (2d * mX * mW);
            matrix[1, 3] = offset.Y;
            matrix[2, 0] = (2d * mX * mZ) - (2d * mY * mW);
            matrix[2, 1] = (2d * mY * mZ) + (2d * mX * mW);
            matrix[2, 2] = 1d - (2d * mX * mX) - (2d * mY * mY);
            matrix[2, 3] = offset.Z;
            matrix[3, 0] = 0d;
            matrix[3, 1] = 0d;
            matrix[3, 2] = 0d;
            matrix[3, 3] = 1d;
            return new Matrix4D(matrix);
        }

        public Matrix4D ToMatrix4D(Vector3 offset, Vector3 scale)
        {
            double[,] matrix = new double[4, 4];
            matrix[0, 0] = scale.X - (2d * mY * mY) - (2d * mZ * mZ);
            matrix[0, 1] = (2d * mX * mY) - (2d * mZ * mW);
            matrix[0, 2] = (2d * mX * mZ) + (2d * mY * mW);
            matrix[0, 3] = offset.X;
            matrix[1, 0] = (2d * mX * mY) + (2d * mZ * mW);
            matrix[1, 1] = scale.Y - (2d * mX * mX) - (2d * mZ * mZ);
            matrix[1, 2] = (2d * mY * mZ) - (2d * mX * mW);
            matrix[1, 3] = offset.Y;
            matrix[2, 0] = (2d * mX * mZ) - (2d * mY * mW);
            matrix[2, 1] = (2d * mY * mZ) + (2d * mX * mW);
            matrix[2, 2] = scale.Z - (2d * mX * mX) - (2d * mY * mY);
            matrix[2, 3] = offset.Z;
            matrix[3, 0] = 0d;
            matrix[3, 1] = 0d;
            matrix[3, 2] = 0d;
            matrix[3, 3] = 1d;
            return new Matrix4D(matrix);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)mX, (float)mY, (float)mZ);
        }

        public override string ToString()
        {
            return mX.ToString() + ", " + mY.ToString() + ", " + mZ.ToString() + ", " + mW.ToString();
        }

        public string ToString(string format)
        {
            return mX.ToString(format) + ", " + mY.ToString(format) + ", " + mZ.ToString(format) + ", " + mW.ToString(format);
        }
    }
}
