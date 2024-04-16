using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public class AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        // Checks if two AABBs intersect
        public bool IntersectsAABB(AABB other)
        {
            return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
                   (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
                   (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
        }

        public bool Intersects(Face face)
        {
            Vector3 faceMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 faceMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Start with the first edge and iterate over the edges to cover all vertices
            HalfEdge edge = face.Edge;
            do
            {
                Vector3 position = edge.Start.Position;

                // Update faceMin and faceMax based on the current vertex position
                faceMin = new Vector3(Math.Min(faceMin.X, position.X), Math.Min(faceMin.Y, position.Y), Math.Min(faceMin.Z, position.Z));
                faceMax = new Vector3(Math.Max(faceMax.X, position.X), Math.Max(faceMax.Y, position.Y), Math.Max(faceMax.Z, position.Z));

                edge = edge.Next;
            } while (edge != null && edge != face.Edge);

            // Perform the intersection test between this AABB and the face's AABB
            // Overlap occurs if the face's min is less than or equal to this max AND face's max is greater than or equal to this min, along all axes
            return !(faceMin.X > this.Max.X || faceMax.X < this.Min.X ||
                     faceMin.Y > this.Max.Y || faceMax.Y < this.Min.Y ||
                     faceMin.Z > this.Max.Z || faceMax.Z < this.Min.Z);
        }

        // Computes the intersection of two AABBs
        public static AABB Intersection(AABB a, AABB b)
        {
            var min = Vector3.Max(a.Min, b.Min);
            var max = Vector3.Min(a.Max, b.Max);
            return new AABB(min, max);
        }

        // Computes the Axis-Aligned Bounding Box for a mesh
        public static (Vector3 min, Vector3 max) ComputeAABBForMesh(TriangleMesh mesh)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var vertex in mesh.Vertices)
            {
                min.X = Math.Min(min.X, vertex.Position.X);
                min.Y = Math.Min(min.Y, vertex.Position.Y);
                min.Z = Math.Min(min.Z, vertex.Position.Z);

                max.X = Math.Max(max.X, vertex.Position.X);
                max.Y = Math.Max(max.Y, vertex.Position.Y);
                max.Z = Math.Max(max.Z, vertex.Position.Z);
            }

            return (min, max);
        }

        // Filters triangles based on whether they intersect with a given AABB
        public static List<Face> FilterTrianglesByAABB(TriangleMesh mesh, Vector3 min, Vector3 max)
        {
            var filteredFaces = new List<Face>();

            foreach (var face in mesh.Faces)
            {
                var vertices = new List<Vertex>()
            {
                face.Edge.Start,
                face.Edge.Next.Start,
                face.Edge.Next.Next.Start
            };

                if (TriangleIntersectsAABB(vertices, min, max))
                {
                    filteredFaces.Add(face);
                }
            }

            return filteredFaces;
        }

        private static bool TriangleIntersectsAABB(List<Vertex> vertices, Vector3 min, Vector3 max)
        {
            // Calculate the AABB for the triangle
            Vector3 triMin = new Vector3(
                vertices.Min(vertex => vertex.Position.X),
                vertices.Min(vertex => vertex.Position.Y),
                vertices.Min(vertex => vertex.Position.Z)
            );

            Vector3 triMax = new Vector3(
                vertices.Max(vertex => vertex.Position.X),
                vertices.Max(vertex => vertex.Position.Y),
                vertices.Max(vertex => vertex.Position.Z)
            );

            // Check for overlap in the X, Y, and Z axes
            bool overlapX = triMin.X <= max.X && triMax.X >= min.X;
            bool overlapY = triMin.Y <= max.Y && triMax.Y >= min.Y;
            bool overlapZ = triMin.Z <= max.Z && triMax.Z >= min.Z;

            // If there's overlap in all three axes, the triangle intersects the AABB
            return overlapX && overlapY && overlapZ;
        }

        // Splits the current AABB into eight smaller AABBs
        public AABB[] Subdivide()
        {
            Vector3 size = (Max - Min).Multiply(0.5f); // Half the dimensions of the current AABB
            AABB[] result = new AABB[8];
            int index = 0;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 newMin = Min + new Vector3(x * size.X, y * size.Y, z * size.Z);
                        result[index++] = new AABB(newMin, newMin + size);
                    }
                }
            }
            return result;
        }

    }
}
