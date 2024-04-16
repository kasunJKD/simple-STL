using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace simpleSTL
{
    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 Normalized()
        {
            float length = (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
            // Avoid division by zero
            if (length > 0)
            {
                return new Vector3(X / length, Y / length, Z / length);
            }
            return this; // Return the vector unchanged if it's zero-length
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator /(Vector3 a, int scalar)
        {
            return new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

        public static Vector3 Min(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        public static Vector3 Max(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }
    }

    public static class Vector3Extensions
    {
        // Extension method to calculate the LengthSquared of a Vector3.
        public static float LengthSquared(this Vector3 vector)
        {
            return vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
        }

        // Extension method to calculate the Length of a Vector3.
        public static float Length(this Vector3 vector)
        {
            return (float)Math.Sqrt(LengthSquared(vector));
        }

        public static Vector3 Multiply(this Vector3 vector, float scalar)
        {
            return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }

        public static Vector3 Multiply(this float scalar, Vector3 vector)
        {
            return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }

        public static Vector3 Cross(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public static Vector3 Normalize(this Vector3 a)
        {
            float length = (float)Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            if (length > 1e-8)
                return new Vector3(a.X / length, a.Y / length, a.Z / length);
            return a;
        }

        public static float Distance(this Vector3 vectorA, Vector3 vectorB)
        {
            // Calculate the difference in each dimension
            float dx = vectorA.X - vectorB.X;
            float dy = vectorA.Y - vectorB.Y;
            float dz = vectorA.Z - vectorB.Z;

            // Calculate the square root of the sum of the squares of the differences
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static float Dot(this Vector3 vectorA, Vector3 vectorB)
        {
            return vectorA.X * vectorB.X + vectorA.Y * vectorB.Y + vectorA.Z * vectorB.Z;
        }
    }

    public class Vector3Comparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 x, Vector3 y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Vector3 obj)
        {
            return obj.GetHashCode();
        }
    }
}
