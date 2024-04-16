using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace simpleSTL
{
    public class Reader
    {
        //STL format https://en.wikipedia.org/wiki/STL_(file_format)
        /*UINT8[80]    – Header                 -     80 bytes
        UINT32       – Number of triangles    -      4 bytes
        foreach triangle                      - 50 bytes:
            REAL32[3] – Normal vector             - 12 bytes
            REAL32[3] – Vertex 1                  - 12 bytes
            REAL32[3] – Vertex 2                  - 12 bytes
            REAL32[3] – Vertex 3                  - 12 bytes
            UINT16    – Attribute byte count      -  2 bytes
        end*/

        private TriangleMesh LoadBinarySTL(FileStream fs)
        {
            TriangleMesh mesh = new TriangleMesh();
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
        /*    solid name
            facet normal ni nj nk
                outer loop
                    vertex v1x v1y v1z
                    vertex v2x v2y v2z
                    vertex v3x v3y v3z
                endloop
        endfacet*/

        private TriangleMesh LoadAsciiSTL(StreamReader sr, string header)
        {
            TriangleMesh mesh = new TriangleMesh();
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

        public TriangleMesh LoadSTL(string filePath)
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
}
