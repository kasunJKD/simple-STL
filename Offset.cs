using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public static class Offset
    {
        public static void OffsetMesh(this TriangleMesh mesh, float offsetDistance)
        {
            // Step 1: Calculate the average normal for each vertex
            Dictionary<Vertex, Vector3> vertexNormals = new Dictionary<Vertex, Vector3>();

            foreach (var vertex in mesh.Vertices)
            {
                vertexNormals[vertex] = new Vector3(0, 0, 0);
            }

            foreach (var face in mesh.Faces)
            {
                var normal = face.Normal;
                var edge = face.Edge;
                do
                {
                    vertexNormals[edge.Start] += normal;
                    edge = edge.Next;
                } while (edge != face.Edge);
            }

            foreach (var vertex in mesh.Vertices)
            {
                vertexNormals[vertex] = vertexNormals[vertex].Normalize();
            }

            // Step 2: Move each vertex along its average normal by the offset distance
            foreach (var vertex in mesh.Vertices)
            {
                vertex.Position += vertexNormals[vertex].Multiply(offsetDistance);
            }

            // Step 3: Optionally, recalculate normals for the mesh
            mesh.RecalculateNormals();
        }
    }
    
}
