using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public class Export
    {
        public void ExportMeshToSTL(TriangleMesh mesh, string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine("solid mesh");
                foreach (var face in mesh.Faces)
                {
                    Vector3 normal = face.Normal;
                    writer.WriteLine($"  facet normal {normal.X} {normal.Y} {normal.Z}");
                    writer.WriteLine("    outer loop");

                    var vertices = GetVerticesOfFace(face);
                    foreach (var vertex in vertices)
                    {
                        writer.WriteLine($"      vertex {vertex.Position.X} {vertex.Position.Y} {vertex.Position.Z}");
                    }

                    writer.WriteLine("    endloop");
                    writer.WriteLine("  endfacet");
                }
                writer.WriteLine("endsolid mesh");
            }
        }

        public void VerifyEdgeConsistency(TriangleMesh mesh)
        {
            foreach (var face in mesh.Faces)
            {
                HalfEdge startEdge = face.Edge;
                HalfEdge currentEdge = startEdge;
                HashSet<HalfEdge> visitedEdges = new HashSet<HalfEdge>();

                do
                {
                    if (!visitedEdges.Add(currentEdge))
                    {
                        throw new Exception("Loop detected in the face traversal, indicating an inconsistent edge linkage.");
                    }

                    currentEdge = currentEdge.Next;
                } while (currentEdge != startEdge);

                if (visitedEdges.Count < 3)
                {
                    throw new Exception("A face with fewer than 3 edges detected, indicating an invalid face.");
                }
            }
        }


        private List<Vertex> GetVerticesOfFace(Face face)
        {
            List<Vertex> vertices = new List<Vertex>();
            HalfEdge startEdge = face.Edge;
            HalfEdge currentEdge = startEdge;
            int safetyCounter = 0; // Safety measure to prevent infinite loops

            do
            {
                vertices.Add(currentEdge.Start);
                currentEdge = currentEdge.Next;

                // Increment and check the safety counter
                safetyCounter++;
                if (safetyCounter > 10000) // Arbitrary large number, adjust based on expected mesh sizes
                {
                    throw new InvalidOperationException("Infinite loop detected in GetVerticesOfFace. Possible mesh corruption.");
                }

            } while (currentEdge != startEdge && currentEdge != null);

            // Additional verification: Ensure we have at least 3 vertices for a valid face
            if (vertices.Count < 3)
            {
                throw new InvalidOperationException("Invalid face encountered with fewer than 3 vertices.");
            }

            return vertices;
        }
    }
}
