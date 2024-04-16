using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public static class TriangleMeshExtensions
    {

        public static void UpdateBoundaryEdges(this TriangleMesh mesh)
        {
            // A boundary edge is one without a twin.
            foreach (var edge in mesh.HalfEdges)
            {
                if (edge.Twin == null)
                {
                    edge.IsBoundary = true;
                }
            }
        }


        //calculate normals and adjust normal direction 
        public static void RecalculateNormals(this TriangleMesh mesh)
        {
            foreach (var face in mesh.Faces)
            {
                HalfEdge edge1 = face.Edge;
                HalfEdge edge2 = edge1.Next;
                HalfEdge edge3 = edge2.Next;

                // Compute two edge vectors
                Vector3 vector1 = edge2.Start.Position - edge1.Start.Position;
                Vector3 vector2 = edge3.Start.Position - edge2.Start.Position;

                // Calculate normal using the right-hand rule
                Vector3 normal = vector1.Cross(vector2);

                normal = normal.Normalize();

                face.Normal = normal;
            }

            // TODO:: to adjust and flipping the normal or reordering face vertices not implemented
        }

        public static bool IsWatertight(this TriangleMesh mesh)
        {
            foreach (var halfedge in mesh.HalfEdges)
            {
                if (halfedge.Twin == null)
                {
                    return false;
                }
            }
            return true;
        }

        public static List<List<HalfEdge>> FindBoundaryLoops(this TriangleMesh mesh)
        {
            List<List<HalfEdge>> boundaryLoops = new List<List<HalfEdge>>();
            HashSet<HalfEdge> visited = new HashSet<HalfEdge>();

            foreach (var edge in mesh.HalfEdges)
            {
                if (edge.Twin == null && !visited.Contains(edge))
                {
                    List<HalfEdge> loop = new List<HalfEdge>();
                    HalfEdge current = edge;
                    do
                    {
                        loop.Add(current);
                        visited.Add(current);
                        // Move to the next edge in the boundary loop
                        current = current.Next;
                        while (current.Twin != null && !visited.Contains(current))
                        {
                            current = current.Twin.Next;
                        }
                    } while (current != edge && !visited.Contains(current));

                    boundaryLoops.Add(loop);
                }
            }

            return boundaryLoops;
        }

        public static HalfEdge FindOrCreateHalfEdge(this TriangleMesh mesh, Vertex start, Vertex end)
        {
            var key = (mesh.Vertices.IndexOf(start), mesh.Vertices.IndexOf(end));
            if (mesh.edgeMap.TryGetValue(key, out var edge))
            {
                return edge;
            }

            var newEdge = new HalfEdge(start);
            mesh.edgeMap[key] = newEdge;
            mesh.HalfEdges.Add(newEdge);
            return newEdge;
        }

        public static Vertex AddVertex(this TriangleMesh mesh, Vector3 position)
        {
            if (mesh.vertexMap.TryGetValue(position, out var existingVertex))
            {
                return existingVertex;
            }
            var newVertex = new Vertex(position);
            mesh.Vertices.Add(newVertex);
            mesh.vertexMap[position] = newVertex;
            return newVertex;
        }

        public static void AddTriangle(this TriangleMesh mesh, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vertex vertex1 = mesh.AddVertex(v1);
            Vertex vertex2 = mesh.AddVertex(v2);
            Vertex vertex3 = mesh.AddVertex(v3);

            HalfEdge edge1 = mesh.FindOrCreateHalfEdge(vertex1, vertex2);
            HalfEdge edge2 = mesh.FindOrCreateHalfEdge(vertex2, vertex3);
            HalfEdge edge3 = mesh.FindOrCreateHalfEdge(vertex3, vertex1);

            edge1.Next = edge2;
            edge2.Next = edge3;
            edge3.Next = edge1;

            Face face = new Face();
            face.Edge = edge1;
            mesh.Faces.Add(face);

            edge1.Face = face;
            edge2.Face = face;
            edge3.Face = face;

            // Setup or verify twins - ensure this step is included
            mesh.SetTwin(edge1, vertex2, vertex1);
            mesh.SetTwin(edge2, vertex3, vertex2);
            mesh.SetTwin(edge3, vertex1, vertex3);
        }

        private static void SetTwin(this TriangleMesh mesh, HalfEdge edge, Vertex start, Vertex end)
        {
            var reverseKey = (mesh.Vertices.IndexOf(start), mesh.Vertices.IndexOf(end));
            if (mesh.edgeMap.TryGetValue(reverseKey, out var twinEdge))
            {
                edge.Twin = twinEdge;
                twinEdge.Twin = edge;
            }

        }


    }
}
