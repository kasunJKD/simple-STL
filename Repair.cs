using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public class Repair
    {
        public TriangleMesh mesh { get; set; } // Ensure Repair has access to the mesh it's modifying.

        public Repair(TriangleMesh _mesh)
        {
            mesh = _mesh;
        }
        public void FillHoles()
        {
            var boundaryLoops = mesh.FindBoundaryLoops(); // Method to implement
            foreach (var loop in boundaryLoops)
            {
                FillSingleHole(loop);
            }
        }

        private void FillSingleHole(List<HalfEdge> boundaryLoop)
        {
            Vector3 centroid = CalculateCentroid(boundaryLoop);
            Vertex centerVertex = mesh.AddVertex(centroid);

            List<HalfEdge> newEdges = new List<HalfEdge>();
            for (int i = 0; i < boundaryLoop.Count; i++)
            {
                HalfEdge boundaryEdge = boundaryLoop[i];
                Vertex startVertex = boundaryEdge.Start;
                Vertex endVertex = boundaryLoop[(i + 1) % boundaryLoop.Count].Start;

                // Create new edges from the center to boundary vertices, forming new triangles.
                HalfEdge newEdge1 = mesh.FindOrCreateHalfEdge(startVertex, centerVertex);
                HalfEdge newEdge2 = mesh.FindOrCreateHalfEdge(centerVertex, endVertex);
                HalfEdge newEdge3 = mesh.FindOrCreateHalfEdge(endVertex, startVertex);

                newEdges.Add(newEdge1);
                newEdges.Add(newEdge2);
                newEdges.Add(newEdge3);

                // Link the new half-edges correctly.
                newEdge1.Next = newEdge2;
                newEdge2.Next = newEdge3;
                newEdge3.Next = newEdge1;

                Face face = new Face();
                face.Edge = newEdge1;
                mesh.Faces.Add(face);

                newEdge1.Face = face;
                newEdge2.Face = face;
                newEdge3.Face = face;

                PostRepair_SetTwinEdge(newEdge1, centerVertex, startVertex);
                PostRepair_SetTwinEdge(newEdge2, endVertex, centerVertex);
                PostRepair_SetTwinEdge(newEdge3, startVertex, endVertex);
            }


        }

        /*public void FillSmallHoles()
        {
            var boundaryLoops = FindBoundaryLoops();
            foreach (var loop in boundaryLoops)
            {
                Vector3 centroid = CalculateCentroid(loop);
                Vertex centerVertex = new Vertex(centroid);
                Vertices.Add(centerVertex);

                for (int i = 0; i < loop.Count; i++)
                {
                    Vertex startVertex = loop[i].Start;
                    Vertex nextVertex = loop[(i + 1) % loop.Count].Start;

                    HalfEdge he1 = new HalfEdge(centerVertex);
                    HalfEdge he2 = new HalfEdge(startVertex);
                    HalfEdge he3 = new HalfEdge(nextVertex);

                    he1.Next = he2; he2.Next = he3; he3.Next = he1;

                    Face newFace = new Face();
                    newFace.Edge = he1;
                    he1.Face = newFace; he2.Face = newFace; he3.Face = newFace;

                    HalfEdges.Add(he1); HalfEdges.Add(he2); HalfEdges.Add(he3);
                    Faces.Add(newFace);

                    // Setup twins between the new half-edges and existing boundary edges
                    SetupTwins(he2, startVertex, nextVertex);
                    SetupTwins(he3, nextVertex, centerVertex);
                }
            }
        }*/

        public void PostRepair_SetTwinEdge(HalfEdge newEdge, Vertex start, Vertex end)
        {
            // This searches for an existing boundary edge that should act as the twin to the new edge.
            var potentialTwin = mesh.HalfEdges.FirstOrDefault(e => e.Start == start && e.Next.Start == end && e.Twin == null);
            if (potentialTwin != null)
            {
                newEdge.Twin = potentialTwin;
                potentialTwin.Twin = newEdge;
                // Ensure newEdge is correctly recognized as not being a boundary edge anymore.
                newEdge.IsBoundary = false;
                potentialTwin.IsBoundary = false; // Assuming the filled hole correctly integrates the edge.
            }
            else
            {
                // Log or handle the case where a twin isn't found as expected.
                Console.WriteLine("Expected twin not found, potential mesh inconsistency.");
            }
        }

        private Vector3 CalculateCentroid(List<HalfEdge> loop)
        {
            Vector3 sum = new Vector3(0, 0, 0);
            foreach (var edge in loop)
            {
                sum += edge.Start.Position;
            }
            return sum / loop.Count;
        }
    }
}
