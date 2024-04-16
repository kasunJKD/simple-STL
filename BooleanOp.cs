using System.IO.Pipes;

namespace simpleSTL
{
    //references
    //https://www.mdpi.com/2227-7390/11/12/2713
    //https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/pubs/tritri.pdf
    public class BooleanOp
    {
        public static List<(Face, Face)> FindIntersectingFaces(OctreeNode octreeA, OctreeNode octreeB)
        {
            List<(Face, Face)> intersectingPairs = new List<(Face, Face)>();
            FindIntersectingFacesRecursive(octreeA, octreeB, intersectingPairs);
            return intersectingPairs;
        }

        private static void FindIntersectingFacesRecursive(OctreeNode nodeA, OctreeNode nodeB, List<(Face, Face)> intersectingPairs)
        {
            // Base case: If the AABBs of the current nodes do not intersect, return.
            if (!nodeA.Bounds.IntersectsAABB(nodeB.Bounds))
                return;

            // If both nodes are leaf nodes, check all face pairs for intersection.
            if (nodeA.IsLeaf && nodeB.IsLeaf)
            {
                foreach (var faceA in nodeA.Faces)
                {
                    foreach (var faceB in nodeB.Faces)
                    {
                        // Use Möller’s algorithm to check for intersection between faceA and faceB
                        //https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/pubs/tritri.pdf
                        if (FacesIntersect(faceA, faceB))
                        {
                            intersectingPairs.Add((faceA, faceB));
                        }
                    }
                }
            }
            else
            {
                // Recurse into children of the larger node or both if they are equal.
                // This is a simplification. In practice, you might need to consider more cases.
                if (!nodeA.IsLeaf)
                {
                    foreach (var childA in nodeA.Children.Where(c => c != null))
                    {
                        FindIntersectingFacesRecursive(childA, nodeB, intersectingPairs);
                    }
                }

                if (!nodeB.IsLeaf)
                {
                    foreach (var childB in nodeB.Children.Where(c => c != null))
                    {
                        FindIntersectingFacesRecursive(nodeA, childB, intersectingPairs);
                    }
                }
            }
        }

        //private static List<(Face, Face)> FindIntersectingFacesIterative(OctreeNode octreeA, OctreeNode octreeB)
        //{
        //    List<(Face, Face)> intersectingPairs = new List<(Face, Face)>();
        //    Stack<(OctreeNode, OctreeNode)> stack = new Stack<(OctreeNode, OctreeNode)>();
        //    stack.Push((octreeA, octreeB));

        //    while (stack.Count > 0)
        //    {
        //        (OctreeNode currentNodeA, OctreeNode currentNodeB) = stack.Pop();

        //        if (!currentNodeA.Bounds.IntersectsAABB(currentNodeB.Bounds))
        //        {
        //            continue; // Skip non-intersecting node pairs.
        //        }

        //        // If both nodes are leaf nodes, directly check for face intersections.
        //        if (currentNodeA.IsLeaf && currentNodeB.IsLeaf)
        //        {
        //            foreach (var faceA in currentNodeA.Faces)
        //            {
        //                foreach (var faceB in currentNodeB.Faces)
        //                {
        //                    if (FacesIntersect(faceA, faceB))
        //                    {
        //                        intersectingPairs.Add((faceA, faceB));
        //                    }
        //                }
        //            }
        //            continue; // Skip further checks since we've processed the leaf nodes.
        //        }

        //        // For non-leaf nodes, push child node pairs onto the stack for further checking.
        //        if (!currentNodeA.IsLeaf)
        //        {
        //            foreach (var childA in currentNodeA.Children.Where(c => c != null))
        //            {
        //                stack.Push((childA, currentNodeB)); // Compare childA with the entire nodeB
        //            }
        //        }

        //        if (!currentNodeB.IsLeaf && currentNodeA.IsLeaf) // Ensures we don't push duplicate pairs if nodeA was already a leaf.
        //        {
        //            foreach (var childB in currentNodeB.Children.Where(c => c != null))
        //            {
        //                stack.Push((currentNodeA, childB)); // Compare the entire nodeA with childB
        //            }
        //        }
        //    }

        //    return intersectingPairs;
        //}


        private static Vector3[] GetVertices(Face face)
        {
            List<Vector3> vertices = new List<Vector3>();

            // Start with the first edge of the face
            HalfEdge startEdge = face.Edge;
            HalfEdge currentEdge = startEdge;

            do
            {
                // Add the starting vertex of the current edge to the list
                vertices.Add(currentEdge.Start.Position);

                // Move to the next edge
                currentEdge = currentEdge.Next;
            }
            // Continue until we've looped back to the start edge
            while (currentEdge != startEdge && currentEdge != null);

            return vertices.ToArray();
        }

        private static bool FacesIntersect(Face faceA, Face faceB)
        {
            // Extract vertices
            Vector3[] v1 = GetVertices(faceA);
            Vector3[] v2 = GetVertices(faceB);

            // Calculate normals
            Vector3 n1 = (v1[1] - v1[0]).Cross(v1[2] - v1[0]).Normalize();
            Vector3 n2 = (v2[1] - v2[0]).Cross(v2[2] - v2[0]).Normalize();

            // Calculate plane constants
            float d1 = -n1.Dot(v1[0]);
            float d2 = -n2.Dot(v2[0]);

            // Check if triangles are on the same side of each other's planes
            if (SameSide(v1, n2, d2) && SameSide(v2, n1, d1))
                return false;

            // Calculate intersection line (D = direction, O = point on the line)
            Vector3 D = n1.Cross(n2);

            Vector3 O = CalculateLinePoint(n1, d1, n2, d2);

            // Project vertices onto the intersection line and calculate intervals
            var intervalA = CalculateInterval(faceA, D, O, n2, d2);
            var intervalB = CalculateInterval(faceB, D, O, n1, d1);

            // Check for interval overlap
            return IntervalOverlap(intervalA, intervalB);
        }

        private static Vector3 CalculateLinePoint(Vector3 n1, float d1, Vector3 n2, float d2)
        {
            Vector3 direction = n1.Cross(n2);

            // Check if the planes are parallel (cross product is close to zero)
            if (direction.LengthSquared() < 1e-6)
            {
                throw new InvalidOperationException("Planes are parallel and do not intersect.");
            }

            // Choose a point that lies on the first plane
            // For simplicity, find a point on the plane n1.dot(point) = -d1 by assuming two coordinates and solving for the third
            Vector3 pointOnPlane1;
            if (Math.Abs(n1.Z) > 1e-6)
            {
                // Assuming x = 0 and y = 0 for simplicity, solve for z
                float z = -d1 / n1.Z;
                pointOnPlane1 = new Vector3(0, 0, z);
            }
            else if (Math.Abs(n1.Y) > 1e-6)
            {
                // Assuming x = 0 and z = 0 for simplicity, solve for y
                float y = -d1 / n1.Y;
                pointOnPlane1 = new Vector3(0, y, 0);
            }
            else if (Math.Abs(n1.X) > 1e-6)
            {
                // Assuming y = 0 and z = 0 for simplicity, solve for x
                float x = -d1 / n1.X;
                pointOnPlane1 = new Vector3(x, 0, 0);
            }
            else
            {
                throw new InvalidOperationException("Invalid plane equation.");
            }

            // Find a second point by moving along the line of intersection from the point found above
            Vector3 pointOnLine = pointOnPlane1 + direction;
            return pointOnLine;
        }

        public static bool IntervalOverlap((float? start, float? end) intervalA, (float? start, float? end) intervalB)
        {
            // Check if intervalA starts after intervalB ends or intervalB starts after intervalA ends
            // If either condition is true, intervals do not overlap
            if (intervalA.start > intervalB.end || intervalB.start > intervalA.end)
            {
                return false; // No overlap
            }
            else
            {
                return true; // Overlap exists
            }

            //return intervalA.end >= intervalB.start && intervalB.end >= intervalA.start;
        }

        public static (float? t1, float? t2) CalculateInterval(Face triangle, Vector3 D, Vector3 O, Vector3 N2, float d2)
        {
            // Extract vertices from the Face (triangle)
            Vector3 V0 = triangle.Edge.Start.Position;
            Vector3 V1 = triangle.Edge.Next.Start.Position;
            Vector3 V2 = triangle.Edge.Next.Next.Start.Position;

            // Compute signed distances of triangle vertices to the plane
            float dV0 = N2.Dot(V0) + d2;
            float dV1 = N2.Dot(V1) + d2;
            float dV2 = N2.Dot(V2) + d2;

            // Compute projections of vertices onto L
            float pV0 = D.Dot(V0 - O);
            float pV1 = D.Dot(V1 - O);
            float pV2 = D.Dot(V2 - O);

            // Initialize interval values to null (no intersection)
            float? t1 = null;
            float? t2 = null;

            // Compute t values for edges intersecting with the line L
            // For each edge, check if it crosses the plane by examining the signs of dV* for its vertices
            if (dV0 * dV1 < 0) // Edge V0V1 crosses the plane
            {
                t1 = pV0 + (pV1 - pV0) * Math.Abs(dV0) / Math.Abs(dV0 - dV1);
            }

            if (dV1 * dV2 < 0) // Edge V1V2 crosses the plane
            {
                // Ensure we assign to t2 only if t1 is already assigned, otherwise assign to t1
                float temp = pV1 + (pV2 - pV1) * Math.Abs(dV1) / Math.Abs(dV1 - dV2);
                if (t1.HasValue)
                {
                    t2 = temp;
                }
                else
                {
                    t1 = temp;
                }
            }

            if (dV2 * dV0 < 0) // Edge V2V0 crosses the plane
            {
                // Handle the case where neither t1 nor t2 has been assigned yet
                float temp = pV2 + (pV0 - pV2) * Math.Abs(dV2) / Math.Abs(dV2 - dV0);
                if (!t1.HasValue)
                {
                    t1 = temp;
                }
                else if (!t2.HasValue || temp < t1.Value)
                {
                    t2 = t1;
                    t1 = temp;
                }
                else
                {
                    t2 = temp;
                }
            }

            // Sort t1 and t2 to ensure t1 <= t2
            if (t1.HasValue && t2.HasValue && t1 > t2)
            {
                var temp = t1;
                t1 = t2;
                t2 = temp;
            }

            return (t1, t2);
        }

        private static bool SameSide(Vector3[] vertices, Vector3 normal, float d)
        {
            /*float dv10 = n1.Dot(v1[0]) + d2;
            float dv11 = n1.Dot(v1[1]) + d2;
            float dv12 = n1.Dot(v1[2]) + d2;

            float dv20 = n2.Dot(v2[0]) + d2;
            float dv21 = n2.Dot(v2[1]) + d2;
            float dv22 = n2.Dot(v2[2]) + d2;*/

            //same sign and not equal to zero
            float tolerance = 1e-6f;

            bool positive = false;
            bool negative = false;
            foreach (var vertex in vertices)
            {
                float distance = normal.Dot(vertex) + d;
                if (distance > tolerance) positive = true;
                if (distance < -tolerance) negative = true;
            }
            // If both positive and negative are true, vertices are on different sides
            // If either is false, all vertices are on the same side
            return !(positive && negative);
        }

    }

   

  
}