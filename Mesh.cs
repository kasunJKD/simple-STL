using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{

    public class Vertex
    {
        public Vector3 Position { get; set; }
        public HalfEdge Edge { get; set; }

        public Vertex(Vector3 position)
        {
            Position = position;
        }
    }

    public class HalfEdge
    {
        public Vertex Start { get; set; }
        public HalfEdge? Twin { get; set; } = null;
        public HalfEdge? Next { get; set; } = null;
        public Face? Face { get; set; } = null;
        public bool IsBoundary { get; set; } = false;

        public HalfEdge(Vertex start)
        {
            Start = start;
        }
    }

    public class Face
    {
        public HalfEdge Edge { get; set; }
        public Vector3 Normal { get; set; }
    }

    public class TriangleMesh
    {
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<HalfEdge> HalfEdges { get; set; } = new List<HalfEdge>();
        public List<Face> Faces { get; set; } = new List<Face>();
        //pairs of vertex indices to their connecting half-edge
        public Dictionary<(int, int), HalfEdge> edgeMap = new Dictionary<(int, int), HalfEdge>();
        public Dictionary<Vector3, Vertex> vertexMap = new Dictionary<Vector3, Vertex>(new Vector3Comparer());

    }

}
