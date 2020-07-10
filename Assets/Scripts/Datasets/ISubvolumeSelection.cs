using System;
using UnityEngine;

namespace Sereno.Datasets
{
    /// <summary>
    /// Interface used for objects that can be selected, volumetric speaking
    /// </summary>
    public interface ISubvolumeSelection
    {
        /// <summary>
        /// Function to call when we need to perform a selection on this object
        /// </summary>
        /// <param name="meshData">The Mesh Data containing the selection data</param>
        /// <param name="MeshToLocalMatrix">The matrix permitting to convert points from the mesh to this object local coordinate system</param>
        void OnSelection(NewSelectionMeshData meshData, Matrix4x4 MeshToLocalMatrix);
    }

    public class RayIntersection
    {
        /// <summary>
        /// Ray -- Triangle intersection function. See Möller–Trumbore intersection algorithm https://en.wikipedia.org/wiki/Möller–Trumbore_intersection_algorithm
        /// </summary>
        /// <param name="rayOrigin">The ray origin</param>
        /// <param name="rayDir">The ray direction</param>
        /// <param name="triangle">The triangle. Each cell of the array defines a point in the triangle (point0, point1, and point2)</param>
        /// <param name="t">The output t to apply to get the intersection point. intersectionPoint = rayOrigin + t*rayDir</param>
        /// <returns>true if there is an intersection, false otherwise</returns>
        static public bool RayTriangleIntersection(Vector3 rayOrigin, Vector3 rayDir, 
                                                   Vector3[] triangle, out float t)
        {
            t = 0;


            const float EPSILON = 0.0000001f;

            Vector3 edge1 = triangle[1] - triangle[0];
            Vector3 edge2 = triangle[2] - triangle[0];
            Vector3 h = Vector3.Cross(rayDir, edge2);
            float a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON)
                return false;    // This ray is parallel to this triangle.

            float   f = 1.0f / a;
            Vector3 s = rayOrigin - triangle[0];
            float   u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float   v = f * Vector3.Dot(rayDir, q);
            if (v < 0.0f || u + v > 1.0f)
                return false;

            // At this stage we can compute t to find out where the intersection point is on the line.
            t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
                return true;
            else // This means that there is a line intersection but not a ray intersection.
                return false;
        }
    }
}
