using System;

namespace Destrospean.CmarNYCBorrowed
{
    public class Matrix3D
    {
        float[,] matrix;

        public float[,] Matrix
        {
            get
            {
                return new float[,] { { this.matrix[0,0], this.matrix[0,1], this.matrix[0,2] },
                    { this.matrix[1,0], this.matrix[1,1], this.matrix[1,2] },
                    { this.matrix[2,0], this.matrix[2,1], this.matrix[2,2] } };
            }
        }

        public Matrix3D()
        {
            this.matrix = new float[,] { { 0, 0, 0 },
                { 0, 0, 0 },
                { 0, 0, 0 } };
        }

        public Matrix3D(float[,] matrix)
        {
            this.matrix = new float[,] { { matrix[0,0], matrix[0,1], matrix[0,2] },
                { matrix[1,0], matrix[1,1], matrix[1,2] },
                { matrix[2,0], matrix[2,1], matrix[2,2] } };
        }

        public static Vector3 operator *(Matrix3D m, Vector3 v)
        {
            float x1 = 0, y1 = 0, z1 = 0;
            for (int i = 0; i < 3; i++)
            {
                x1 += m.matrix[0, i] * v.Coordinates[i];
                y1 += m.matrix[1, i] * v.Coordinates[i];
                z1 += m.matrix[2, i] * v.Coordinates[i];
            }
            return new Vector3(x1, y1, z1);
        }

        public static Matrix3D operator *(Matrix3D m, float f)
        {
            float[,] res = new float[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    res[r, c] = m.matrix[r, c] * f;
                }
            }
            return new Matrix3D(res);
        }

        public static float[] operator *(Matrix3D m, float[] v)
        {
            float[] tmp = new float[3];
            for (int i = 0; i < 3; i++)
            {
                tmp[0] += m.matrix[0, i] * v[i];
                tmp[1] += m.matrix[1, i] * v[i];
                tmp[2] += m.matrix[2, i] * v[i];
            }
            return tmp;
        }

        public static Matrix3D operator *(Matrix3D m1, Matrix3D m2)
        {
            float[][] v = new float[3][];

            for (int i = 0; i < 3; i++)
            {
                v[i] = m1 * new float[] { m2.matrix[0, i], m2.matrix[1, i], m2.matrix[2, i] };
            }
            return new Matrix3D(new float[,] { { v[0][0], v[1][0], v[2][0] },
                { v[0][1], v[1][1], v[2][1] },
                { v[0][2], v[1][2], v[2][2] } });
        }

        public static Matrix3D Identity
        {
            get { return new Matrix3D(new float[,] { { 1f, 0f, 0f }, { 0f, 1f, 0f }, { 0f, 0f, 1f } }); }
        }

        public static Matrix3D FromScale(Vector3 scale)
        {
            return new Matrix3D(new float[,] { { scale.X, 0f, 0f }, { 0f, scale.Y, 0f }, { 0f, 0f, scale.Z } });
        }

        public Vector3 Scale
        {
            get
            {
                Vector3 sx = new Vector3((float)this.matrix[0, 0], (float)this.matrix[0, 1], (float)this.matrix[0, 2]);
                Vector3 sy = new Vector3((float)this.matrix[1, 0], (float)this.matrix[1, 1], (float)this.matrix[1, 2]);
                Vector3 sz = new Vector3((float)this.matrix[2, 0], (float)this.matrix[2, 1], (float)this.matrix[2, 2]);
                return new Vector3(sx.Magnitude, sy.Magnitude, sz.Magnitude);
            }
        }

        public static Matrix3D RotateZupToYup
        {
            get { return new Matrix3D(new float[,] { { 1f, 0f, 0f }, { 0f, 0f, 1f }, { 0f, -1f, 0f } }); }
        }

        public static Matrix3D RotateYupToZup
        {
            get { return new Matrix3D(new float[,] { { 1f, 0f, 0f }, { 0f, 0f, -1f }, { 0f, 1f, 0f } }); }
        }

        public Matrix3D Inverse()
        {
            // computes the inverse of a matrix
            float det = this.matrix[0, 0] * (this.matrix[1, 1] * this.matrix[2, 2] - this.matrix[2, 1] * this.matrix[1, 2]) -
                this.matrix[0, 1] * (this.matrix[1, 0] * this.matrix[2, 2] - this.matrix[1, 2] * this.matrix[2, 0]) +
                this.matrix[0, 2] * (this.matrix[1, 0] * this.matrix[2, 1] - this.matrix[1, 1] * this.matrix[2, 0]);

            float invdet = 1f / det;

            float[,] minv = new float[3, 3];
            minv[0, 0] = (this.matrix[1, 1] * this.matrix[2, 2] - this.matrix[2, 1] * this.matrix[1, 2]) * invdet;
            minv[0, 1] = (this.matrix[0, 2] * this.matrix[2, 1] - this.matrix[0, 1] * this.matrix[2, 2]) * invdet;
            minv[0, 2] = (this.matrix[0, 1] * this.matrix[1, 2] - this.matrix[0, 2] * this.matrix[1, 1]) * invdet;
            minv[1, 0] = (this.matrix[1, 2] * this.matrix[2, 0] - this.matrix[1, 0] * this.matrix[2, 2]) * invdet;
            minv[1, 1] = (this.matrix[0, 0] * this.matrix[2, 2] - this.matrix[0, 2] * this.matrix[2, 0]) * invdet;
            minv[1, 2] = (this.matrix[1, 0] * this.matrix[0, 2] - this.matrix[0, 0] * this.matrix[1, 2]) * invdet;
            minv[2, 0] = (this.matrix[1, 0] * this.matrix[2, 1] - this.matrix[2, 0] * this.matrix[1, 1]) * invdet;
            minv[2, 1] = (this.matrix[2, 0] * this.matrix[0, 1] - this.matrix[0, 0] * this.matrix[2, 1]) * invdet;
            minv[2, 2] = (this.matrix[0, 0] * this.matrix[1, 1] - this.matrix[1, 0] * this.matrix[0, 1]) * invdet;

            return new Matrix3D(minv);
        }

        public Matrix3D Transpose()
        {
            float[,] mt = new float[,] { { this.matrix[0,0], this.matrix[1,0], this.matrix[2,0] },
                { this.matrix[0,1], this.matrix[1,1], this.matrix[2,1] },
                { this.matrix[0,2], this.matrix[1,2], this.matrix[2,2] } };
            return new Matrix3D(mt);
        }

        public override string ToString()
        {
            string str = "";
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    str += this.matrix[r, c].ToString();
                    if (c != 2 || r != 2) str += ", ";
                }
                str += Environment.NewLine;
            }
            return str;
        }
    }
}
