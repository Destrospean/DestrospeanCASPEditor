using System;

namespace Destrospean.CmarNYCBorrowed
{
    public struct Matrix4D
    {
        double[,] matrix;

        public double[,] Matrix
        {
            get
            {
                return new double[,] { { this.matrix[0,0], this.matrix[0,1], this.matrix[0,2], this.matrix[0,3] },
                    { this.matrix[1,0], this.matrix[1,1], this.matrix[1,2], this.matrix[1,3] },
                    { this.matrix[2,0], this.matrix[2,1], this.matrix[2,2], this.matrix[2,3] },
                    { this.matrix[3,0], this.matrix[3,1], this.matrix[3,2], this.matrix[3,3] } };
            }
        }

        public double[] Values
        {
            get
            {
                return new double[] { this.matrix[0,0], this.matrix[0,1], this.matrix[0,2], this.matrix[0,3],
                    this.matrix[1,0], this.matrix[1,1], this.matrix[1,2], this.matrix[1,3],
                    this.matrix[2,0], this.matrix[2,1], this.matrix[2,2], this.matrix[2,3],
                    this.matrix[3,0], this.matrix[3,1], this.matrix[3,2], this.matrix[3,3] };
            }
        }

        public Matrix4D(double[,] array4x4)
        {
            this.matrix = new double[,] { { array4x4[0,0], array4x4[0,1], array4x4[0,2], array4x4[0,3] },
                { array4x4[1,0], array4x4[1,1], array4x4[1,2], array4x4[1,3] },
                { array4x4[2,0], array4x4[2,1], array4x4[2,2], array4x4[2,3] },
                { array4x4[3,0], array4x4[3,1], array4x4[3,2], array4x4[3,3] } };
        }

        public Matrix4D(double[] array)
        {
            this.matrix = new double[,] { { array[0], array[1], array[2], array[3] },
                { array[4], array[5], array[6], array[7] },
                { array[8], array[9], array[10], array[11] },
                { array[12], array[13], array[14], array[15] } };
        }

        public static Matrix4D Identity
        {
            get { return new Matrix4D(new double[,] { { 1d, 0d, 0d, 0d }, { 0d, 1d, 0d, 0d }, { 0d, 0d, 1d, 0d }, { 0d, 0d, 0d, 1d } }); }
        }

        public static Matrix4D FromOffset(Vector3 offset)
        {
            return new Matrix4D(new double[,] { { 1d, 0d, 0d, offset.X }, { 0d, 1d, 0d, offset.Y }, { 0d, 0d, 1d, offset.Z }, { 0d, 0d, 0d, 1d } });
        }

        public static Matrix4D FromOffset(double[] offset)
        {
            return new Matrix4D(new double[,] { { 1d, 0d, 0d, offset[0] }, { 0d, 1d, 0d, offset[1] }, { 0d, 0d, 1d, offset[2] }, { 0d, 0d, 0d, 1d } });
        }

        public static Matrix4D FromScale(Vector3 scale)
        {
            return new Matrix4D(new double[,] { { scale.X, 0d, 0d, 0d }, { 0d, scale.Y, 0d, 0d }, { 0d, 0d, scale.Z, 0d }, { 0d, 0d, 0d, 1d } });
        }

        public static Matrix4D FromScale(double[] scale)
        {
            return new Matrix4D(new double[,] { { scale[0], 0d, 0d, 0d }, { 0d, scale[1], 0d, 0d }, { 0d, 0d, scale[2], 0d }, { 0d, 0d, 0d, 1d } });
        }

        public static Matrix4D FromAxisAngle(AxisAngle aa)
        {
            aa.Normalize();
            Matrix4D m = new Matrix4D();
            m.matrix = new double[,] { { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 } };
            double c = Math.Cos(aa.Angle);
            double s = Math.Sin(aa.Angle);
            double t = 1.0 - c;

            m.matrix[0, 0] = c + aa.X * aa.X * t;
            m.matrix[1, 1] = c + aa.Y * aa.Y * t;
            m.matrix[2, 2] = c + aa.Z * aa.Z * t;

            double tmp1 = aa.X * aa.Y * t;
            double tmp2 = aa.Z * s;
            m.matrix[1, 0] = tmp1 + tmp2;
            m.matrix[0, 1] = tmp1 - tmp2;
            tmp1 = aa.X * aa.Z * t;
            tmp2 = aa.Y * s;
            m.matrix[2, 0] = tmp1 - tmp2;
            m.matrix[0, 2] = tmp1 + tmp2; tmp1 = aa.Y * aa.Z * t;
            tmp2 = aa.X * s;
            m.matrix[2, 1] = tmp1 + tmp2;
            m.matrix[1, 2] = tmp1 - tmp2;

            m.matrix[3, 3] = 1;

            return m;
        }

        public AxisAngle toAxisAngle()
        {
            double angle, x, y, z; // variables for result
            double epsilon = 0.01; // margin to allow for rounding errors
            double epsilon2 = 0.1; // margin to distinguish between 0 and 180 degrees
            // optional check that input is pure rotation, 'isRotationMatrix' is defined at:
            // https://www.euclideanspace.com/maths/algebra/matrix/orthogonal/rotation/

            if ((Math.Abs(this.matrix[0, 1] - this.matrix[1, 0]) < epsilon)
                && (Math.Abs(this.matrix[0, 2] - this.matrix[2, 0]) < epsilon)
                && (Math.Abs(this.matrix[1, 2] - this.matrix[2, 1]) < epsilon))
            {
                // singularity found
                // first check for identity matrix which must have +1 for all terms
                //  in leading diagonaland zero in other terms
                if ((Math.Abs(this.matrix[0, 1] + this.matrix[1, 0]) < epsilon2)
                    && (Math.Abs(this.matrix[0, 2] + this.matrix[2, 0]) < epsilon2)
                    && (Math.Abs(this.matrix[1, 2] + this.matrix[2, 1]) < epsilon2)
                    && (Math.Abs(this.matrix[0, 0] + this.matrix[1, 1] + this.matrix[2, 2] - 3) < epsilon2))
                {
                    // this singularity is identity matrix so angle = 0
                    return new AxisAngle(0, 1, 0, 0); // zero angle, arbitrary axis
                }
                // otherwise this singularity is angle = 180
                angle = Math.PI;
                double xx = (this.matrix[0, 0] + 1) / 2;
                double yy = (this.matrix[1, 1] + 1) / 2;
                double zz = (this.matrix[2, 2] + 1) / 2;
                double xy = (this.matrix[0, 1] + this.matrix[1, 0]) / 4;
                double xz = (this.matrix[0, 2] + this.matrix[2, 0]) / 4;
                double yz = (this.matrix[1, 2] + this.matrix[2, 1]) / 4;
                if ((xx > yy) && (xx > zz))
                { // m[0][0] is the largest diagonal term
                    if (xx < epsilon)
                    {
                        x = 0;
                        y = 0.7071;
                        z = 0.7071;
                    }
                    else
                    {
                        x = Math.Sqrt(xx);
                        y = xy / x;
                        z = xz / x;
                    }
                }
                else if (yy > zz)
                { // m[1][1] is the largest diagonal term
                    if (yy < epsilon)
                    {
                        x = 0.7071;
                        y = 0;
                        z = 0.7071;
                    }
                    else
                    {
                        y = Math.Sqrt(yy);
                        x = xy / y;
                        z = yz / y;
                    }
                }
                else
                { // m[2][2] is the largest diagonal term so base result on this
                    if (zz < epsilon)
                    {
                        x = 0.7071;
                        y = 0.7071;
                        z = 0;
                    }
                    else
                    {
                        z = Math.Sqrt(zz);
                        x = xz / z;
                        y = yz / z;
                    }
                }
                return new AxisAngle(angle, x, y, z); // return 180 deg rotation
            }
            // as we have reached here there are no singularities so we can handle normally
            double s = Math.Sqrt((this.matrix[2, 1] - this.matrix[1, 2]) * (this.matrix[2, 1] - this.matrix[1, 2])
                + (this.matrix[0, 2] - this.matrix[2, 0]) * (this.matrix[0, 2] - this.matrix[2, 0])
                + (this.matrix[1, 0] - this.matrix[0, 1]) * (this.matrix[1, 0] - this.matrix[0, 1])); // used to normalise
            if (Math.Abs(s) < 0.001) s = 1;
            // prevent divide by zero, should not happen if matrix is orthogonal and should be
            // caught by singularity test above, but I've left it in just in case
            angle = Math.Acos((this.matrix[0, 0] + this.matrix[1, 1] + this.matrix[2, 2] - 1) / 2);
            x = (this.matrix[2, 1] - this.matrix[1, 2]) / s;
            y = (this.matrix[0, 2] - this.matrix[2, 0]) / s;
            z = (this.matrix[1, 0] - this.matrix[0, 1]) / s;
            return new AxisAngle(angle, x, y, z);
        }

        public static Matrix4D RotateZupToYup
        {
            get { return new Matrix4D(new double[,] { { 1f, 0f, 0f, 0f }, { 0f, 0f, 1f, 0f }, { 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f } }); }
        }

        public static Matrix4D RotateYupToZup
        {
            get { return new Matrix4D(new double[,] { { 1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f }, { 0f, 1f, 0f, 0f }, { 0f, 0f, 0f, 1f } }); }
        }

        public Matrix3D ToMatrix3D()
        {
            return new Matrix3D(new float[,] { { (float)this.matrix[0,0], (float)this.matrix[0,1], (float)this.matrix[0,2] },
                { (float)this.matrix[1,0], (float)this.matrix[1,1], (float)this.matrix[1,2] },
                { (float)this.matrix[2,0], (float)this.matrix[2,1], (float)this.matrix[2,2] } });
        }

        /// <summary>
        /// Rounds values close to zero
        /// </summary>
        public void Clean()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (Math.Abs(this.matrix[i, j]) < .0000002d) this.matrix[i, j] = 0d;
                }
            }
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

        public Vector3 Offset
        {
            get
            {
                return new Vector3((float)this.matrix[0, 3], (float)this.matrix[1, 3], (float)this.matrix[2, 3]);
            }
        }

        public Matrix4D RemoveOffset()
        {
            double[,] d = new double[4, 4];
            Array.Copy(this.matrix, d, 16);
            d[0, 3] = 0d;
            d[1, 3] = 0d;
            d[2, 3] = 0d;
            return new Matrix4D(d);
        }

        public Matrix4D Inverse()
        {
            double[,] m = this.matrix;
            double det = m[0, 3] * m[1, 2] * m[2, 1] * m[3, 0] - m[0, 2] * m[1, 3] * m[2, 1] * m[3, 0] - m[0, 3] * m[1, 1] * m[2, 2] * m[3, 0] + m[0, 1] * m[1, 3] * m[2, 2] * m[3, 0] +
                m[0, 2] * m[1, 1] * m[2, 3] * m[3, 0] - m[0, 1] * m[1, 2] * m[2, 3] * m[3, 0] - m[0, 3] * m[1, 2] * m[2, 0] * m[3, 1] + m[0, 2] * m[1, 3] * m[2, 0] * m[3, 1] +
                m[0, 3] * m[1, 0] * m[2, 2] * m[3, 1] - m[0, 0] * m[1, 3] * m[2, 2] * m[3, 1] - m[0, 2] * m[1, 0] * m[2, 3] * m[3, 1] + m[0, 0] * m[1, 2] * m[2, 3] * m[3, 1] +
                m[0, 3] * m[1, 1] * m[2, 0] * m[3, 2] - m[0, 1] * m[1, 3] * m[2, 0] * m[3, 2] - m[0, 3] * m[1, 0] * m[2, 1] * m[3, 2] + m[0, 0] * m[1, 3] * m[2, 1] * m[3, 2] +
                m[0, 1] * m[1, 0] * m[2, 3] * m[3, 2] - m[0, 0] * m[1, 1] * m[2, 3] * m[3, 2] - m[0, 2] * m[1, 1] * m[2, 0] * m[3, 3] + m[0, 1] * m[1, 2] * m[2, 0] * m[3, 3] +
                m[0, 2] * m[1, 0] * m[2, 1] * m[3, 3] - m[0, 0] * m[1, 2] * m[2, 1] * m[3, 3] - m[0, 1] * m[1, 0] * m[2, 2] * m[3, 3] + m[0, 0] * m[1, 1] * m[2, 2] * m[3, 3];
            double invdet = 1d / det;

            double[,] minv = new double[4, 4];
            minv[0, 0] = (m[1, 2] * m[2, 3] * m[3, 1] - m[1, 3] * m[2, 2] * m[3, 1] + m[1, 3] * m[2, 1] * m[3, 2] - m[1, 1] * m[2, 3] * m[3, 2] - m[1, 2] * m[2, 1] * m[3, 3] + m[1, 1] * m[2, 2] * m[3, 3]) * invdet;
            minv[0, 1] = (m[0, 3] * m[2, 2] * m[3, 1] - m[0, 2] * m[2, 3] * m[3, 1] - m[0, 3] * m[2, 1] * m[3, 2] + m[0, 1] * m[2, 3] * m[3, 2] + m[0, 2] * m[2, 1] * m[3, 3] - m[0, 1] * m[2, 2] * m[3, 3]) * invdet;
            minv[0, 2] = (m[0, 2] * m[1, 3] * m[3, 1] - m[0, 3] * m[1, 2] * m[3, 1] + m[0, 3] * m[1, 1] * m[3, 2] - m[0, 1] * m[1, 3] * m[3, 2] - m[0, 2] * m[1, 1] * m[3, 3] + m[0, 1] * m[1, 2] * m[3, 3]) * invdet;
            minv[0, 3] = (m[0, 3] * m[1, 2] * m[2, 1] - m[0, 2] * m[1, 3] * m[2, 1] - m[0, 3] * m[1, 1] * m[2, 2] + m[0, 1] * m[1, 3] * m[2, 2] + m[0, 2] * m[1, 1] * m[2, 3] - m[0, 1] * m[1, 2] * m[2, 3]) * invdet;
            minv[1, 0] = (m[1, 3] * m[2, 2] * m[3, 0] - m[1, 2] * m[2, 3] * m[3, 0] - m[1, 3] * m[2, 0] * m[3, 2] + m[1, 0] * m[2, 3] * m[3, 2] + m[1, 2] * m[2, 0] * m[3, 3] - m[1, 0] * m[2, 2] * m[3, 3]) * invdet;
            minv[1, 1] = (m[0, 2] * m[2, 3] * m[3, 0] - m[0, 3] * m[2, 2] * m[3, 0] + m[0, 3] * m[2, 0] * m[3, 2] - m[0, 0] * m[2, 3] * m[3, 2] - m[0, 2] * m[2, 0] * m[3, 3] + m[0, 0] * m[2, 2] * m[3, 3]) * invdet;
            minv[1, 2] = (m[0, 3] * m[1, 2] * m[3, 0] - m[0, 2] * m[1, 3] * m[3, 0] - m[0, 3] * m[1, 0] * m[3, 2] + m[0, 0] * m[1, 3] * m[3, 2] + m[0, 2] * m[1, 0] * m[3, 3] - m[0, 0] * m[1, 2] * m[3, 3]) * invdet;
            minv[1, 3] = (m[0, 2] * m[1, 3] * m[2, 0] - m[0, 3] * m[1, 2] * m[2, 0] + m[0, 3] * m[1, 0] * m[2, 2] - m[0, 0] * m[1, 3] * m[2, 2] - m[0, 2] * m[1, 0] * m[2, 3] + m[0, 0] * m[1, 2] * m[2, 3]) * invdet;
            minv[2, 0] = (m[1, 1] * m[2, 3] * m[3, 0] - m[1, 3] * m[2, 1] * m[3, 0] + m[1, 3] * m[2, 0] * m[3, 1] - m[1, 0] * m[2, 3] * m[3, 1] - m[1, 1] * m[2, 0] * m[3, 3] + m[1, 0] * m[2, 1] * m[3, 3]) * invdet;
            minv[2, 1] = (m[0, 3] * m[2, 1] * m[3, 0] - m[0, 1] * m[2, 3] * m[3, 0] - m[0, 3] * m[2, 0] * m[3, 1] + m[0, 0] * m[2, 3] * m[3, 1] + m[0, 1] * m[2, 0] * m[3, 3] - m[0, 0] * m[2, 1] * m[3, 3]) * invdet;
            minv[2, 2] = (m[0, 1] * m[1, 3] * m[3, 0] - m[0, 3] * m[1, 1] * m[3, 0] + m[0, 3] * m[1, 0] * m[3, 1] - m[0, 0] * m[1, 3] * m[3, 1] - m[0, 1] * m[1, 0] * m[3, 3] + m[0, 0] * m[1, 1] * m[3, 3]) * invdet;
            minv[2, 3] = (m[0, 3] * m[1, 1] * m[2, 0] - m[0, 1] * m[1, 3] * m[2, 0] - m[0, 3] * m[1, 0] * m[2, 1] + m[0, 0] * m[1, 3] * m[2, 1] + m[0, 1] * m[1, 0] * m[2, 3] - m[0, 0] * m[1, 1] * m[2, 3]) * invdet;
            minv[3, 0] = (m[1, 2] * m[2, 1] * m[3, 0] - m[1, 1] * m[2, 2] * m[3, 0] - m[1, 2] * m[2, 0] * m[3, 1] + m[1, 0] * m[2, 2] * m[3, 1] + m[1, 1] * m[2, 0] * m[3, 2] - m[1, 0] * m[2, 1] * m[3, 2]) * invdet;
            minv[3, 1] = (m[0, 1] * m[2, 2] * m[3, 0] - m[0, 2] * m[2, 1] * m[3, 0] + m[0, 2] * m[2, 0] * m[3, 1] - m[0, 0] * m[2, 2] * m[3, 1] - m[0, 1] * m[2, 0] * m[3, 2] + m[0, 0] * m[2, 1] * m[3, 2]) * invdet;
            minv[3, 2] = (m[0, 2] * m[1, 1] * m[3, 0] - m[0, 1] * m[1, 2] * m[3, 0] - m[0, 2] * m[1, 0] * m[3, 1] + m[0, 0] * m[1, 2] * m[3, 1] + m[0, 1] * m[1, 0] * m[3, 2] - m[0, 0] * m[1, 1] * m[3, 2]) * invdet;
            minv[3, 3] = (m[0, 1] * m[1, 2] * m[2, 0] - m[0, 2] * m[1, 1] * m[2, 0] + m[0, 2] * m[1, 0] * m[2, 1] - m[0, 0] * m[1, 2] * m[2, 1] - m[0, 1] * m[1, 0] * m[2, 2] + m[0, 0] * m[1, 1] * m[2, 2]) * invdet;

            return new Matrix4D(minv);
        }

        public Matrix4D Transpose()
        {
            double[,] mt = new double[,] { { this.matrix[0,0], this.matrix[1,0], this.matrix[2,0], this.matrix[3,0] },
                { this.matrix[0,1], this.matrix[1,1], this.matrix[2,1], this.matrix[3,1] },
                { this.matrix[0,2], this.matrix[1,2], this.matrix[2,2], this.matrix[3,2] },
                { this.matrix[0,3], this.matrix[1,3], this.matrix[2,3], this.matrix[3,3] } };
            return new Matrix4D(mt);
        }

        public static Matrix4D operator *(Matrix4D m, float f)
        {
            double[,] res = new double[4, 4];
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    res[r, c] = m.matrix[r, c] * f;
                }
            }
            return new Matrix4D(res);
        }

        public static Vector3 operator *(Matrix4D m, Vector3 v)
        {
            double x1 = 0, y1 = 0, z1 = 0, ex = 0; ;
            double[] tmp = new double[] { v.X, v.Y, v.Z, 1f };
            for (int i = 0; i < 4; i++)
            {
                x1 += m.matrix[0, i] * tmp[i];
                y1 += m.matrix[1, i] * tmp[i];
                z1 += m.matrix[2, i] * tmp[i];
                ex += m.matrix[3, i] * tmp[i];
            }
            return new Vector3((float)x1, (float)y1, (float)z1);
        }

        public static Matrix4D operator *(Matrix4D m1, Matrix4D m2)
        {
            double[][] v = new double[4][];

            for (int i = 0; i < 4; i++)
            {
                v[i] = m1 * new double[] { m2.matrix[0, i], m2.matrix[1, i], m2.matrix[2, i], m2.matrix[3, i] };
            }
            return new Matrix4D(new double[,] { { v[0][0], v[1][0], v[2][0], v[3][0] },
                { v[0][1], v[1][1], v[2][1], v[3][1] },
                { v[0][2], v[1][2], v[2][2], v[3][2] },
                { v[0][3], v[1][3], v[2][3], v[3][3] } });
        }

        public static double[] operator *(Matrix4D m, double[] v)
        {
            double[] tmp = new double[4];
            for (int i = 0; i < 4; i++)
            {
                tmp[0] += m.matrix[0, i] * v[i];
                tmp[1] += m.matrix[1, i] * v[i];
                tmp[2] += m.matrix[2, i] * v[i];
                tmp[3] += m.matrix[3, i] * v[i];
            }
            return tmp;
        }

        public override string ToString()
        {
            string str = "";
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    str += this.matrix[r, c].ToString("G7");
                    if (c != 3 || r != 3) str += ", ";
                }
                if (r < 4) str += Environment.NewLine;
            }
            return str;
        }

        public string ToUnpunctuatedString()
        {
            string str = "";
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    str += this.matrix[r, c].ToString("G7", System.Globalization.CultureInfo.InvariantCulture) + " ";
                }
            }
            return str;
        }
    }
}
