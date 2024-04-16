using simpleSTL;
using System.Globalization;
using System.Text.RegularExpressions;

/*public class Vector3
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

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    public static Vector3 Normalize(Vector3 a)
    {
        float length = (float)Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
        if (length > 1e-8)
            return new Vector3(a.X / length, a.Y / length, a.Z / length);
        return a; 
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3 operator /(Vector3 a, int scalar)
    {
        return new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
    }
}

public class Vertex
{
    public Vector3 Position;
    public HalfEdge Edge;

    public Vertex(Vector3 position)
    {
        Position = position;
    }
}

public class HalfEdge
{
    public Vertex Start;
    public HalfEdge? Twin;
    public HalfEdge? Next; 
    // Omit Prev for simplicity in this adjusted approach
    public Face? Face; 
    public bool IsBoundary { get; set; } = false;

    public HalfEdge(Vertex start)
    {
        Start = start;
        Twin = null; 
    }
}

public class Face
{
    public HalfEdge Edge;
    public Vector3 Normal;

    public void SetEdge(HalfEdge edge)
    {
        Edge = edge;
    }
}


public class Mesh
{
    public List<Vertex> Vertices = new List<Vertex>();
    public List<HalfEdge> HalfEdges = new List<HalfEdge>();
    public List<Face> Faces = new List<Face>();
    //pairs of vertex indices to their connecting half-edge
    private Dictionary<(int, int), HalfEdge> edgeMap = new Dictionary<(int, int), HalfEdge>();
    private Dictionary<Vector3, Vertex> vertexMap = new Dictionary<Vector3, Vertex>(new Vector3Comparer());

    private Vertex AddVertex(Vector3 position)
    {
        if (vertexMap.TryGetValue(position, out var existingVertex))
        {
            return existingVertex;
        }
        var newVertex = new Vertex(position);
        Vertices.Add(newVertex);
        vertexMap[position] = newVertex;
        return newVertex;
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vertex vertex1 = AddVertex(v1);
        Vertex vertex2 = AddVertex(v2);
        Vertex vertex3 = AddVertex(v3);

        HalfEdge edge1 = FindOrCreateHalfEdge(vertex1, vertex2);
        HalfEdge edge2 = FindOrCreateHalfEdge(vertex2, vertex3);
        HalfEdge edge3 = FindOrCreateHalfEdge(vertex3, vertex1);

        edge1.Next = edge2;
        edge2.Next = edge3;
        edge3.Next = edge1;

        Face face = new Face();
        face.SetEdge(edge1);
        Faces.Add(face);

        edge1.Face = face;
        edge2.Face = face;
        edge3.Face = face;

        // Setup or verify twins - ensure this step is included
        SetTwin(edge1, vertex2, vertex1);
        SetTwin(edge2, vertex3, vertex2);
        SetTwin(edge3, vertex1, vertex3);
    }

    private HalfEdge FindOrCreateHalfEdge(Vertex start, Vertex end)
    {
        var key = (Vertices.IndexOf(start), Vertices.IndexOf(end));
        if (edgeMap.TryGetValue(key, out var edge))
        {
            return edge;
        }

        var newEdge = new HalfEdge(start);
        edgeMap[key] = newEdge;
        HalfEdges.Add(newEdge);
        return newEdge;
    }

    private void SetTwin(HalfEdge edge, Vertex start, Vertex end)
    {
        var reverseKey = (Vertices.IndexOf(start), Vertices.IndexOf(end));
        if (edgeMap.TryGetValue(reverseKey, out var twinEdge))
        {
            edge.Twin = twinEdge;
            twinEdge.Twin = edge;
        }
        
    }


    public void UpdateBoundaryEdges()
    {
        // A boundary edge is one without a twin.
        foreach (var edge in HalfEdges)
        {
            if (edge.Twin == null)
            {
                edge.IsBoundary = true;
            }
        }
    }

    class Vector3Comparer : IEqualityComparer<Vector3>
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

    //calculate normals and adjust normal direction 
    public void RecalculateNormals()
    {
        foreach (var face in Faces)
        {
            HalfEdge edge1 = face.Edge;
            HalfEdge edge2 = edge1.Next;
            HalfEdge edge3 = edge2.Next;

            // Compute two edge vectors
            Vector3 vector1 = edge2.Start.Position - edge1.Start.Position;
            Vector3 vector2 = edge3.Start.Position - edge2.Start.Position;

            // Calculate normal using the right-hand rule
            Vector3 normal = Vector3.Cross(vector1, vector2);

            normal = Vector3.Normalize(normal);

            face.Normal = normal;
        }

        // TODO:: to adjust and flipping the normal or reordering face vertices not implemented
    }

    public bool IsWatertight()
    {
        foreach (var halfedge in HalfEdges)
        {
            if (halfedge.Twin == null)
            {
                return false;
            }
        }
        return true;
    }

    // Method to find boundary loops
    public List<List<HalfEdge>> FindBoundaryLoops()
    {
        List<List<HalfEdge>> boundaryLoops = new List<List<HalfEdge>>();
        HashSet<HalfEdge> visited = new HashSet<HalfEdge>();

        foreach (var edge in HalfEdges)
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

    public void FillHoles()
    {
        var boundaryLoops = FindBoundaryLoops(); // Method to implement
        foreach (var loop in boundaryLoops)
        {
            FillSingleHole(loop);
        }
    }

    private void FillSingleHole(List<HalfEdge> boundaryLoop)
    {
        Vector3 centroid = CalculateCentroid(boundaryLoop);
        Vertex centerVertex = AddVertex(centroid);

        List<HalfEdge> newEdges = new List<HalfEdge>();
        for (int i = 0; i < boundaryLoop.Count; i++)
        {
            HalfEdge boundaryEdge = boundaryLoop[i];
            Vertex startVertex = boundaryEdge.Start;
            Vertex endVertex = boundaryLoop[(i + 1) % boundaryLoop.Count].Start;

            // Create new edges from the center to boundary vertices, forming new triangles.
            HalfEdge newEdge1 = FindOrCreateHalfEdge(startVertex, centerVertex);
            HalfEdge newEdge2 = FindOrCreateHalfEdge(centerVertex, endVertex);
            HalfEdge newEdge3 = FindOrCreateHalfEdge(endVertex, startVertex);

            newEdges.Add(newEdge1);
            newEdges.Add(newEdge2);
            newEdges.Add(newEdge3);

            // Link the new half-edges correctly.
            newEdge1.Next = newEdge2;
            newEdge2.Next = newEdge3;
            newEdge3.Next = newEdge1;

            Face face = new Face();
            face.SetEdge(newEdge1);
            Faces.Add(face);

            newEdge1.Face = face;
            newEdge2.Face = face;
            newEdge3.Face = face;

            SetupTwins(newEdge1, centerVertex, startVertex);
            SetupTwins(newEdge2, endVertex, centerVertex);
            SetupTwins(newEdge3, startVertex, endVertex);
        }

        
    }

    *//*public void FillSmallHoles()
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
    }*//*

    private void SetupTwins(HalfEdge newEdge, Vertex start, Vertex end)
    {
        // This searches for an existing boundary edge that should act as the twin to the new edge.
        var potentialTwin = HalfEdges.FirstOrDefault(e => e.Start == start && e.Next.Start == end && e.Twin == null);
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



class STLReader
{
    //STL format https://en.wikipedia.org/wiki/STL_(file_format)
    *//*UINT8[80]    – Header                 -     80 bytes
    UINT32       – Number of triangles    -      4 bytes
    foreach triangle                      - 50 bytes:
        REAL32[3] – Normal vector             - 12 bytes
        REAL32[3] – Vertex 1                  - 12 bytes
        REAL32[3] – Vertex 2                  - 12 bytes
        REAL32[3] – Vertex 3                  - 12 bytes
        UINT16    – Attribute byte count      -  2 bytes
    end*//*

    private Mesh LoadBinarySTL(FileStream fs)
    {
        Mesh mesh = new Mesh();
        using (BinaryReader br = new BinaryReader(fs))
        {
            br.ReadBytes(80);
            uint numTriangles = br.ReadUInt32();
            for (int i = 0; i < numTriangles; i++)
            {
                Vector3 normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                Vector3 v1 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                Vector3 v2 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                Vector3 v3 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                mesh.AddTriangle(v1, v2, v3);
                br.ReadUInt16();
            }
        }
        return mesh;
    }

    //ASCII format
    *//*    solid name
        facet normal ni nj nk
            outer loop
                vertex v1x v1y v1z
                vertex v2x v2y v2z
                vertex v3x v3y v3z
            endloop
    endfacet*//*

    private Mesh LoadAsciiSTL(StreamReader sr, string header)
    {
        Mesh mesh = new Mesh();
        string line;
        Regex vertexPattern = new Regex(@"vertex\s+([\d\.-]+)\s+([\d\.-]+)\s+([\d\.-]+)");

        Vector3 normal = new Vector3(0, 0, 0);
        Vector3[] vertices = new Vector3[3];
        int vertexCount = 0;

        while ((line = sr.ReadLine()) != null)
        {
            if (line.Trim().StartsWith("facet normal"))
            {
                string[] normalParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                normal = new Vector3(float.Parse(normalParts[2], CultureInfo.InvariantCulture),
                                     float.Parse(normalParts[3], CultureInfo.InvariantCulture),
                                     float.Parse(normalParts[4], CultureInfo.InvariantCulture));
            }
            else if (vertexPattern.IsMatch(line))
            {
                Match match = vertexPattern.Match(line);
                vertices[vertexCount] = new Vector3(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                                                    float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                                                    float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
                vertexCount++;
                if (vertexCount == 3)
                {
                    mesh.AddTriangle(vertices[0], vertices[1], vertices[2]);
                    vertexCount = 0;
                }
            }
        }
        return mesh;
    }

    public Mesh LoadSTL(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[5];
            fs.Read(buffer, 0, 5);
            fs.Seek(0, SeekOrigin.Begin); // Reset stream position

            string start = System.Text.Encoding.ASCII.GetString(buffer);
            if (start.Trim().ToLower().Equals("solid"))
            {
                //ASCII file starting with "solid" but is actually binary
                fs.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(fs))
                {
                    string header = sr.ReadLine();
                    string secondLine = sr.ReadLine().Trim().ToLower();
                    if (secondLine.StartsWith("facet normal"))
                    {
                        return LoadAsciiSTL(sr, header);
                    }
                }
                // go to start of the stream
                fs.Seek(0, SeekOrigin.Begin);
            }
            return LoadBinarySTL(fs);
        }
    }
}

public class MeshToSTLExporter
{
    public void ExportMeshToSTL(Mesh mesh, string filename)
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

    public void VerifyEdgeConsistency(Mesh mesh)
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
}*/


class Program
{
    static void Main()
    {
        Reader reader = new Reader();
        /*Mesh mesh = new Mesh();
        mesh.AddTriangle(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        mesh.AddTriangle(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, 0, 0));*/
        TriangleMesh mesh = reader.LoadSTL("D:\\work\\2023 - Kasun\\mesh library internship\\Example support models\\Support tester simple\\Support tester simple.stl");
        Console.WriteLine("is mesh watertight? " + mesh.IsWatertight());
        mesh.RecalculateNormals();

        Export exporter = new Export();
        exporter.ExportMeshToSTL(mesh, "outputstl.stl");

        /*Mesh mesh = new Mesh();

        var v0 = new Vector3(-0.5f, -0.5f, -0.5f);
        var v1 = new Vector3(0.5f, -0.5f, -0.5f);
        var v2 = new Vector3(0.5f, 0.5f, -0.5f);
        var v3 = new Vector3(-0.5f, 0.5f, -0.5f);
        var v4 = new Vector3(-0.5f, -0.5f, 0.5f);
        var v5 = new Vector3(0.5f, -0.5f, 0.5f);
        var v6 = new Vector3(0.5f, 0.5f, 0.5f);
        var v7 = new Vector3(-0.5f, 0.5f, 0.5f);

        // Bottom
        mesh.AddTriangle(v0, v3, v2);
        mesh.AddTriangle(v0, v2, v1);
        // Front
        mesh.AddTriangle(v0, v1, v5);
        mesh.AddTriangle(v0, v5, v4);
        // Back
        mesh.AddTriangle(v2, v3, v7);
        mesh.AddTriangle(v2, v7, v6);
        // Left
        mesh.AddTriangle(v0, v4, v7);
        mesh.AddTriangle(v0, v7, v3);
        // Right
        mesh.AddTriangle(v1, v2, v6);
        mesh.AddTriangle(v1, v6, v5);

        mesh.UpdateBoundaryEdges();
        var boundaryLoopsAfterFilling = mesh.FindBoundaryLoops();

        mesh.FillHoles();

        mesh.UpdateBoundaryEdges();
        var boundaryLoopsAfterFilling2 = mesh.FindBoundaryLoops();

        mesh.RecalculateNormals();
       
        MeshToSTLExporter exporter = new MeshToSTLExporter();
        //exporter.VerifyEdgeConsistency(mesh);
        exporter.ExportMeshToSTL(mesh, "out123.stl");*/

        Console.ReadLine();
    }
}


