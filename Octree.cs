using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleSTL
{
    public class OctreeNode
    {
        public AABB Bounds;
        public List<Face> Faces = new List<Face>();
        public OctreeNode[] Children = new OctreeNode[8];
        public bool IsLeaf => Children.All(child => child == null);

        public OctreeNode(AABB bounds)
        {
            Bounds = bounds;
        }

        // Recursively builds the octree node and its children
        private void Subdivide(int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth || Faces.Count <= 1) // Termination condition
            {
                return;
            }

            AABB[] childBounds = Bounds.Subdivide();
            List<Face>[] childTriangles = new List<Face>[8];
            for (int i = 0; i < 8; i++)
            {
                childTriangles[i] = new List<Face>();
            }

            // Distribute triangles into child nodes
            foreach (var triangle in Faces)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (childBounds[i].Intersects(triangle))
                    {
                        childTriangles[i].Add(triangle);
                    }
                }
            }

            // Create child nodes
            for (int i = 0; i < 8; i++)
            {
                if (childTriangles[i].Count > 0)
                {
                    Children[i] = new OctreeNode(childBounds[i]);
                    Children[i].Faces = childTriangles[i];
                    Children[i].Subdivide(currentDepth + 1, maxDepth); // Recursive subdivision
                }
            }
        }

        public static OctreeNode BuildOctreeForMesh(TriangleMesh mesh, int maxDepth)
        {
            // Compute the overall AABB for the mesh
            var (min, max) = AABB.ComputeAABBForMesh(mesh);
            AABB initialBounds = new AABB(min, max);

            // Convert mesh faces to a format suitable for the octree if needed
            List<Face> faces = mesh.Faces; // Assuming Faces are directly usable

            // Construct the octree
            OctreeNode root = new OctreeNode(initialBounds);
            root.Faces = mesh.Faces;
            root.Subdivide(0, maxDepth);

            return root;
        }
    }
}
