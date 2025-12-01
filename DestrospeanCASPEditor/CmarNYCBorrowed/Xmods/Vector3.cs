using System;
using System.Collections.Generic;
using System.Globalization;

namespace Destrospean.CmarNYCBorrowed
{
    public struct Vector3 : IEquatable<Vector3>
    {
        public float[] Coordinates
        {
            get
            {
                return new float[]
                {
                    X,
                    Y,
                    Z
                };
            }
            set
            {
                X = value[0];
                Y = value[1];
                Z = value[2];
            }
        }

        public float Magnitude
        {
            get
            {
                return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
            }
        }

        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(float[] coordinates)
        {
            X = coordinates[0];
            Y = coordinates[1];
            Z = coordinates[2];
        }

        public Vector3(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public static bool operator ==(Vector3 v1, Vector3 v2)
        {
            return AlmostEquals(v1.X, v2.X) && AlmostEquals(v1.Y, v2.Y) && AlmostEquals(v1.Z, v2.Z);
        }

        public static bool operator !=(Vector3 v1, Vector3 v2)
        {
            return !(v1 == v2);
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3 operator *(Vector3 v1, float s2)
        {
            return new Vector3(v1.X * s2, v1.Y * s2, v1.Z * s2);
        }

        public static Vector3 operator *(float s1, Vector3 v2)
        {
            return v2 * s1;
        }

        public static Vector3 operator /(Vector3 v1, float s2)
        {
            return new Vector3(v1.X / s2, v1.Y / s2, v1.Z / s2);
        }

        public static Vector3 AbsoluteValue(Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
        }

        public static bool AlmostEquals(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < .00005f;
        }

        public float Angle(Vector3 other)
        {
            return Angle(this, other);
        }

        public static float Angle(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Acos(Normalize(v1).Dot(Normalize(v2)));
        }

        public int ArrayMinimumIndex(float[] array)
        {
            int tempIndex = -1;
            var tempValue = float.MaxValue;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] < tempValue)
                {
                    tempIndex = i;
                    tempValue = array[i];
                }
            }
            return tempIndex;
        }

        public bool Between(Vector3 p1, Vector3 p2)
        {
            float max = Math.Max(p1.X, p2.X),
            min = Math.Min(p1.X, p2.X);
            if (min > X || X > max)
            {
                return false;
            }
            min = Math.Min(p1.Y, p2.Y);
            max = Math.Max(p1.Y, p2.Y);
            if (min > Y || Y > max)
            {
                return false;
            }
            min = Math.Min(p1.Z, p2.Z);
            max = Math.Max(p1.Z, p2.Z);
            if (min > Z || Z > max)
            {
                return false;
            }
            return true;
        }

        public static Vector3 Centroid(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return new Vector3((p1.X + p2.X + p3.X) / 3f, (p1.Y + p2.Y + p3.Y) / 3f, (p1.Z + p2.Z + p3.Z) / 3f);
        }

        public Vector3 Cross(Vector3 other)
        {
            return Cross(this, other);
        }

        public static Vector3 Cross(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);
        }

        public float Distance(Vector3 other)
        {
            return Distance(this, other);
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z));
        }

        public float Dot(Vector3 other)
        {
            return Dot(this, other);
        }

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3 ? (Vector3)obj == this : false;
        }

        public bool Equals(Vector3 obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return (int)((X + Y + Z) % Int32.MaxValue);
        }

        public float[] GetInterpolationWeights(Vector3[] points, float weightingFactor)
        {
            var weights = new float[points.Length];
            if (points.Length == 1)
            {
                weights[0] = 1;
                return weights;
            }
            for (var i = 0; i < points.Length; i++)
            {
                if (Vector3.Distance(points[i], this) == 0)
                {
                    weights[i] = 1;
                    return weights;
                }
            }
            var d = new float[points.Length];
            var dt = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                d[i] = 1 / (float)Math.Pow(Vector3.Distance(points[i], this), weightingFactor);
                dt += d[i];
            }
            for (var i = 0; i < points.Length; i++)
            {
                weights[i] = d[i] / dt;
            }
            return weights;
        }

        public Vector3 NearestPoint(Vector3 thisFacesCentroid, Vector3[] refPointsArray, Vector3[] refPointsFacesCentroids)
        {
            return refPointsArray[NearestPointIndex(thisFacesCentroid, refPointsArray, refPointsFacesCentroids)];
        }

        public int NearestPointIndex(Vector3 thisFacesCentroid, Vector3[] refPointsArray, Vector3[] refPointsFacesCentroids)
        {
            float minDistance = float.MaxValue,
            minFaceDistance = float.MaxValue;
            var workingIndices = new List<int>();
            for (var i = 0; i < refPointsArray.Length; i++)
            {
                if (Distance(refPointsArray[i]) < minDistance)
                {
                    workingIndices.Clear();
                    workingIndices.Add(i);
                    minDistance = Distance(refPointsArray[i]);
                }
                else if (Distance(refPointsArray[i]) == minDistance)
                {
                    workingIndices.Add(i);
                }
            }
            var index = 0;
            for (var i = 0; i < workingIndices.Count; i++)
            {
                if (thisFacesCentroid.Distance(refPointsFacesCentroids[workingIndices[i]]) < minFaceDistance)
                {
                    index = workingIndices[i];
                    minFaceDistance = thisFacesCentroid.Distance(refPointsFacesCentroids[workingIndices[i]]);
                }
            }
            return index;
        }

        public int NearestPointIndexSimple(Vector3[] RefPointsArray)
        {
            var index = 0;
            var minDistance = float.MaxValue;
            for (var i = 0; i < RefPointsArray.Length; i++)
            {
                if (Distance(RefPointsArray[i]) < minDistance)
                {
                    minDistance = Distance(RefPointsArray[i]);
                    index = i;
                }
            }
            return index;
        }

        public Vector3 NearestPointSimple(Vector3[] refPointsArray)
        {
            return refPointsArray[NearestPointIndexSimple(refPointsArray)];
        }

        public void Normalize()
        {
            var n = Normalize(this);
            X = n.X;
            Y = n.Y;
            Z = n.Z;
        }

        public static Vector3 Normalize(Vector3 vector)
        {
            if (vector.Magnitude == 0)
            {
                return vector;
            }
            else
            {
                var inverse = 1 / vector.Magnitude;
                return new Vector3(vector.X * inverse, vector.Y * inverse, vector.Z * inverse);
            }
        }

        public static Vector3 Parse(string coordinateString)
        {
            var coordinatesAsStrings = coordinateString.Split(',');
            if (coordinatesAsStrings.Length != 3)
            {
                throw new FormatException("Input not in correct format");
            }
            var coordinates = new float[3];
            for (var i = 0; i < 3; i++)
            {
                if (!float.TryParse(coordinatesAsStrings[i], NumberStyles.Float, CultureInfo.InvariantCulture, out coordinates[i]))
                {
                    throw new FormatException("Input not in correct format");
                }
            }
            return new Vector3(coordinates);
        }

        public bool PositionClose(float[] other)
        {
            return PositionClose(new Vector3(other));
        }

        public bool PositionClose(Vector3 other)
        {
            var epsilon = .005f;
            return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon && Math.Abs(Z - other.Z) < epsilon;
        }

        public bool PositionMatches(float[] other)
        {
            return PositionMatches(new Vector3(other));
        }

        public bool PositionMatches(Vector3 other)
        {
            var epsilon = .0005f;
            return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon && Math.Abs(Z - other.Z) < epsilon;
        }

        public bool PositionMatches(float x, float y, float z)
        {
            return PositionMatches(new Vector3(x, y, z));
        }

        public Vector3 ProjectToLine(Vector3 p1, Vector3 p2)
        {
            Vector3 temp0 = p2 - p1,
            temp1 = this - p1;
            temp0.Normalize();
            return new Vector3(p1 + Vector3.Dot(temp0, temp1) * temp0);
        }

        public Vector3 Scale(Vector3 other)
        {
            return Scale(this, other);
        }

        public static Vector3 Scale(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public override string ToString()
        {
            return X.ToString() + ", " + Y.ToString() + ", " + Z.ToString();
        }
    }
}
