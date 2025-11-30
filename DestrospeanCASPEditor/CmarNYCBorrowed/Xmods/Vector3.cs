using System;
using System.Globalization;
using System.Collections.Generic;

namespace Destrospean.CmarNYCBorrowed
{
    public struct Vector3 : IEquatable<Vector3>
    {
        private float x, y, z;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public float[] Coordinates
        {
            get { return new float[] { x, y, z }; }
            set
            {
                x = value[0];
                y = value[1];
                z = value[2];
            }
        }

        public float Magnitude
        {
            get
            {
                double tmp = (x * x) + (y * y) + (z * z);
                return (float)Math.Sqrt(tmp);
            }
        }

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3(float[] coordinates)
        {
            this.x = coordinates[0];
            this.y = coordinates[1];
            this.z = coordinates[2];
        }

        public Vector3(Vector3 vector)
        {
            this.x = vector.X;
            this.y = vector.Y;
            this.z = vector.Z;
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return
                (
                    new Vector3
                    (
                        v1.X + v2.X,
                        v1.Y + v2.Y,
                        v1.Z + v2.Z
                    )
                );
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return
                (
                    new Vector3
                    (
                        v1.X - v2.X,
                        v1.Y - v2.Y,
                        v1.Z - v2.Z
                    )
                );
        }

        public static bool operator ==(Vector3 v1, Vector3 v2)
        {
            return
                (
                    (AlmostEquals(v1.X, v2.X)) &&
                    (AlmostEquals(v1.Y, v2.Y)) &&
                    (AlmostEquals(v1.Z, v2.Z))
                );
        }

        public override bool Equals(object obj)
        {
            // Check object other is a Vector3 object
            if (obj is Vector3)
            {
                // Convert object to Vector3
                Vector3 otherVector = (Vector3)obj;

                // Check for equality
                return otherVector == this;
            }
            else
            {
                return false;
            }
        }

        public bool Equals(Vector3 obj)
        {
            return obj == this;
        }

        public static bool operator !=(Vector3 v1, Vector3 v2)
        {
            return !(v1 == v2);
        }

        public static Vector3 operator *(Vector3 v1, float s2)
        {
            return
                (
                    new Vector3
                    (
                        v1.X * s2,
                        v1.Y * s2,
                        v1.Z * s2
                    )
                );
        }

        public static Vector3 operator *(float s1, Vector3 v2)
        {
            return v2 * s1;
        }

        public static Vector3 operator /(Vector3 v1, float s2)
        {
            return
                (
                    new Vector3
                    (
                        v1.X / s2,
                        v1.Y / s2,
                        v1.Z / s2
                    )
                );
        }

        public static Vector3 Scale(Vector3 v1, Vector3 v2)
        {
            return
                (
                    new Vector3
                    (
                        v1.X * v2.X,
                        v1.Y * v2.Y,
                        v1.Z * v2.Z
                    )
                );
        }

        public static Vector3 AbsoluteValue(Vector3 v)
        {
            return new Vector3(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z));
        }

        public Vector3 Scale(Vector3 scalingVector)
        {
            return Scale(this, scalingVector);
        }

        public static Vector3 Cross(Vector3 v1, Vector3 v2)
        {
            return
                (
                    new Vector3
                    (
                        v1.Y * v2.Z - v1.Z * v2.Y,
                        v1.Z * v2.X - v1.X * v2.Z,
                        v1.X * v2.Y - v1.Y * v2.X
                    )
                );
        }

        public Vector3 Cross(Vector3 other)
        {
            return Cross(this, other);
        }

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return
                (
                    v1.X * v2.X +
                    v1.Y * v2.Y +
                    v1.Z * v2.Z
                );
        }

        public float Dot(Vector3 other)
        {
            return Dot(this, other);
        }

        public static Vector3 Normalize(Vector3 v1)
        {
            // Check for divide by zero errors
            if (v1.Magnitude == 0)
            {
                return v1;
            }
            else
            {
                // find the inverse of the vector's magnitude
                float inverse = 1 / v1.Magnitude;
                return
                    (
                        new Vector3
                        (
                            // multiply each component by the inverse of the magnitude
                            v1.X * inverse,
                            v1.Y * inverse,
                            v1.Z * inverse
                        )
                    );
            }
        }

        public void Normalize()
        {
            Vector3 n = Normalize(this);
            this.x = n.x;
            this.y = n.y;
            this.z = n.z;
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return
                (
                    (float)Math.Sqrt
                    (
                        (v1.X - v2.X) * (v1.X - v2.X) +
                        (v1.Y - v2.Y) * (v1.Y - v2.Y) +
                        (v1.Z - v2.Z) * (v1.Z - v2.Z)
                    )
                );
        }

        public float Distance(Vector3 other)
        {
            return Distance(this, other);
        }

        public static float Angle(Vector3 v1, Vector3 v2)
        {
            return
                (
                    (float)Math.Acos
                    (
                        Normalize(v1).Dot(Normalize(v2))
                    )
                );
        }

        public float Angle(Vector3 other)
        {
            return Angle(this, other);
        }

        public static Vector3 Centroid(Vector3 P1, Vector3 P2, Vector3 P3)
        {
            return new Vector3((P1.x + P2.x + P3.x) / 3f, (P1.y + P2.y + P3.y) / 3f, (P1.z + P2.z + P3.z) / 3f);
        }

        public Vector3 ProjectToLine(Vector3 Point1, Vector3 Point2)
        {
            Vector3 tmp = Point2 - Point1;
            tmp.Normalize();
            Vector3 tmp2 = this - Point1;
            Vector3 tmp3 = Vector3.Dot(tmp, tmp2) * tmp;
            return new Vector3(Point1 + tmp3);
        }

        public bool Between(Vector3 Point1, Vector3 Point2)
        {
            float min = Math.Min(Point1.X, Point2.X);
            float max = Math.Max(Point1.X, Point2.X);
            if (min > this.X | this.X > max)
            {
                return false;
            }
            min = Math.Min(Point1.Y, Point2.Y);
            max = Math.Max(Point1.Y, Point2.Y);
            if (min > this.Y | this.Y > max)
            {
                return false;
            }
            min = Math.Min(Point1.Z, Point2.Z);
            max = Math.Max(Point1.Z, Point2.Z);
            if (min > this.Z | this.Z > max)
            {
                return false;
            }
            return true;
        }

        public float[] GetInterpolationWeights(Vector3[] points, float weightingFactor)
        {
            float[] weights = new float[points.Length];

            if (points.Length == 1)
            {
                weights[0] = 1f;
                return weights;
            }
            for (int i = 0; i < points.Length; i++)
            {
                if (Vector3.Distance(points[i], this) == 0f)
                {
                    weights[i] = 1f;
                    return weights;
                }
            }

            float[] d = new float[points.Length];
            float dt = 0;
            for (int i = 0; i < points.Length; i++)
            {
                d[i] = 1f / (float)Math.Pow(Vector3.Distance(points[i], this), weightingFactor);
                dt += d[i];
            }

            for (int i = 0; i < points.Length; i++)
            {
                weights[i] = d[i] / dt;
            }
            //string a = "Distance: ";
            //string b = "Weights: ";
            //string x = "Positions: ";
            //for (int i = 0; i < weights.Length; i++)
            //{
            //    a += d[i].ToString() + ", ";
            //    b += weights[i].ToString() + ", ";
            //    x += points[i].x.ToString() + "," + points[i].y.ToString() + "," + points[i].z.ToString() + ", ";
            //}
            //MessageBox.Show(a + System.Environment.NewLine + b + System.Environment.NewLine + x);
            return weights;
        }

        public int NearestPointIndexSimple(Vector3[] RefPointsArray)
        {
            float minDistance = float.MaxValue;
            int ind = 0;
            for (int i = 0; i < RefPointsArray.Length; i++)
            {
                if (this.Distance(RefPointsArray[i]) < minDistance)
                {
                    minDistance = this.Distance(RefPointsArray[i]);
                    ind = i;
                }
            }
            return ind;
        }

        public Vector3 NearestPointSimple(Vector3[] RefPointsArray)
        {
            int ind = this.NearestPointIndexSimple(RefPointsArray);
            return RefPointsArray[ind];
        }

        public int NearestPointIndex(Vector3 thisFacesCentroid, Vector3[] RefPointsArray, Vector3[] refPointsFacesCentroids)
        {
            float minDistance = float.MaxValue;
            List<int> workingIndexes = new List<int>();
            for (int i = 0; i < RefPointsArray.Length; i++)
            {
                if (this.Distance(RefPointsArray[i]) < minDistance)
                {
                    workingIndexes.Clear();
                    workingIndexes.Add(i);
                    minDistance = this.Distance(RefPointsArray[i]);
                }
                else if (this.Distance(RefPointsArray[i]) == minDistance)
                {
                    workingIndexes.Add(i);
                }
            }
            float minFaceDistance = float.MaxValue;
            int ind = 0;
            for (int i = 0; i < workingIndexes.Count; i++)
            {
                if (thisFacesCentroid.Distance(refPointsFacesCentroids[workingIndexes[i]]) < minFaceDistance)
                {
                    ind = workingIndexes[i];
                    minFaceDistance = thisFacesCentroid.Distance(refPointsFacesCentroids[workingIndexes[i]]);
                }
            }
            return ind;
        }

        public Vector3 NearestPoint(Vector3 thisFacesCentroid, Vector3[] RefPointsArray, Vector3[] refPointsFacesCentroids)
        {
            int ind = this.NearestPointIndex(thisFacesCentroid, RefPointsArray, refPointsFacesCentroids);
            return RefPointsArray[ind];
        }

        internal int ArrayMinimumIndex(float[] array)
        {
            int tmp = -1;
            float tmpVal = float.MaxValue;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] < tmpVal) tmp = i;
            }
            return tmp;
        }

        public override int GetHashCode()
        {
            return
                (
                    (int)((X + Y + Z) % Int32.MaxValue)
                );
        }

        public override string ToString()
        {
            return this.X.ToString() + ", " + this.Y.ToString() + ", " + this.Z.ToString();
        }

        public static Vector3 Parse(string coordinateString)
        {
            string[] coordsStr = coordinateString.Split(new char[] { ',' });
            if (coordsStr.Length != 3) throw new FormatException("Input not in correct format");
            float[] coords = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (!float.TryParse(coordsStr[i], NumberStyles.Float, CultureInfo.InvariantCulture, out coords[i])) throw new FormatException("Input not in correct format");
            }
            return new Vector3(coords);
        }

        internal static bool AlmostEquals(float f1, float f2)
        {
            const float EPSILON = 0.00005f;
            return (Math.Abs(f1 - f2) < EPSILON);
        }

        internal bool positionMatches(float[] other)
        {
            return this.positionMatches(new Vector3(other));
        }
        internal bool positionMatches(float x, float y, float z)
        {
            return this.positionMatches(new Vector3(x, y, z));
        }
        internal bool positionMatches(Vector3 other)
        {
            const float EPSILON = 0.0005f;
            if (Math.Abs(this.x - other.x) < EPSILON && Math.Abs(this.y - other.y) < EPSILON && Math.Abs(this.z - other.z) < EPSILON) return true;
            return false;
        }

        internal bool positionClose(float[] other)
        {
            return this.positionClose(new Vector3(other));
        }
        internal bool positionClose(Vector3 other)
        {
            const float EPSILON = 0.005f;
            if (Math.Abs(this.x - other.x) < EPSILON && Math.Abs(this.y - other.y) < EPSILON && Math.Abs(this.z - other.z) < EPSILON) return true;
            return false;
        }
    }
}
