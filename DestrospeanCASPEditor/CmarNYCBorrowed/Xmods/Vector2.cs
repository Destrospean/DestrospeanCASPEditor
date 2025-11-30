using System;

namespace Destrospean.CmarNYCBorrowed
{
    public struct Vector2
    {
        private float x, y;

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

        public float[] Coordinates
        {
            get { return new float[] { x, y }; }
            set
            {
                x = value[0];
                y = value[1];
            }
        }

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2(float[] coordinates)
        {
            this.x = coordinates[0];
            this.y = coordinates[1];
        }

        public Vector2(Vector2 vector)
        {
            this.x = vector.X;
            this.y = vector.Y;
        }

        public double Magnitude
        {
            get { return Math.Sqrt((this.x * this.x) + (this.y * this.y)); }
        }

        public float Distance(Vector2 other)
        {
            double deltaX = this.x - other.x;
            double deltaY = this.y - other.y;
            return (float)Math.Sqrt(Math.Pow(deltaX, 2d) + Math.Pow(deltaY, 2d));
        }

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            const float EPSILON = 0.00005f;
            return
                (
                    (Math.Abs(v1.X - v2.X) < EPSILON) &&
                    (Math.Abs(v1.Y - v2.Y) < EPSILON)
                );
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2)
            {
                Vector2 other = (Vector2)obj;
                return (other == this);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(Vector2 obj)
        {
            return obj == this;
        }

        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            return !(v1 == v2);
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator *(Vector2 v1, float m2)
        {
            return new Vector2(v1.x * m2, v1.y * m2);
        }

        public static Vector2 operator *(float m1, Vector2 v2)
        {
            return v2 * m1;
        }

        public static Vector2 Normalize(Vector2 v1)
        {
            // Check for divide by zero errors
            if (v1.Magnitude == 0)
            {
                throw new DivideByZeroException("Cannot normalize a vector with magnitude of zero!");
            }
            else
            {
                // find the inverse of the vector's magnitude
                float inverse = (float)(1f / v1.Magnitude);
                return new Vector2(v1.X * inverse, v1.Y * inverse);
            }
        }

        public override int GetHashCode()
        {
            return
                (
                    (int)((X + Y) % Int32.MaxValue)
                );
        }

        public override string ToString()
        {
            return this.X.ToString() + ", " + this.Y.ToString();
        }

        public static float Dot(Vector2 v1, Vector2 v2)
        {
            return (v1.x * v2.x) + (v1.y * v2.y);
        }

        public float Dot(Vector2 other)
        {
            return Dot(this, other);
        }

        public bool DistanceFromLineRestricted(Vector2 P1, Vector2 P2, out float distance, out int endpointIndex)
        //returns whether point project to line segment falls within endpoints, 
        //distance = distance point to projected point or to nearest endpoint,
        //endPontIndex = 0 if nearest endpoint is P1 or 1 if P2
        {
            Vector2 tmp = this - P1;
            Vector2 line = P2 - P1;

            float lineLenSq = (line.x * line.x) + (line.y * line.y);
            float distanceOnSegment = Vector2.Dot(tmp, line) / lineLenSq;

            endpointIndex = 0;
            if (distanceOnSegment >= 0f && distanceOnSegment <= 1f)         //within segment
            {
                Vector2 projectedPoint = P1 + (line * distanceOnSegment);
                distance = this.Distance(projectedPoint);
                return true;
            }
            else
            {
                if (distanceOnSegment > 1f) endpointIndex = 1;
                distance = this.Distance(endpointIndex == 0 ? P1 : P2);
                return false;
            }
        }

        public float DistanceFromLine(Vector2 P1, Vector2 P2)
        {
            return Math.Abs(((P2.y - P1.y) * this.x) - ((P2.x - P1.x) * this.y) + (P2.x * P1.y) - (P2.y * P1.x)) / P1.Distance(P2);
        }

        public Vector2 ProjectToLine(Vector2 A, Vector2 B, out bool withinLine)
        {
            Vector2 AP = this - A;       //Vector from A to P   
            Vector2 AB = B - A;       //Vector from A to B  

            float magnitudeAB = (AB.x * AB.x) + (AB.y * AB.y);     //Magnitude of AB vector (its length squared)
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            withinLine = (distance >= 0f && distance <= 1f);
            return A + AB * distance;
        }

        public bool ProjectsWithinLine(Vector2 v1, Vector2 v2)
        {
            bool tmp;
            this.ProjectToLine(v1, v2, out tmp);
            return tmp;
        }
    }
}

