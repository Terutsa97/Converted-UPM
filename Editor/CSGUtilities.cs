using System.Collections;
using System.Collections.Generic;

using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

using UnityEngine;

namespace Terutsa97.ProBuilderUtilities.Editor
{
    /// <summary>
    /// Useful Static Methods taken from CSG's source code.
    /// </summary>
    public static class CSGUtilities
    {
        public static CSGPlane CalcPolygonPlane(ControlMesh controlMesh, short polygonIndex)
        {
            if (controlMesh == null ||
                controlMesh.Polygons == null ||
                polygonIndex >= controlMesh.Polygons.Length)
                return new CSGPlane(MathConstants.upVector3, 0);
            var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
            if (edgeIndices.Length == 3)
            {
                var v0 = controlMesh.GetVertex(edgeIndices[0]);
                var v1 = controlMesh.GetVertex(edgeIndices[1]);
                var v2 = controlMesh.GetVertex(edgeIndices[2]);

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            var normal = MathConstants.zeroVector3;
            var prevIndex = edgeIndices.Length - 1;
            if (prevIndex < 0)
            {
                return new CSGPlane(MathConstants.upVector3, MathConstants.zeroVector3);
            }

            var prevVertex = controlMesh.GetVertex(edgeIndices[prevIndex]);
            for (var e = 0; e < edgeIndices.Length; e++)
            {
                var currVertex = controlMesh.GetVertex(edgeIndices[e]);
                normal.x += (prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z);
                normal.y += (prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x);
                normal.z += (prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y);

                prevVertex = currVertex;
            }
            normal = -normal.normalized;

            var d = 0.0f;
            var count = 0;
            for (var e = 0; e < edgeIndices.Length; e++)
            {
                var currVertex = controlMesh.GetVertex(edgeIndices[e]);
                d += Vector3.Dot(normal, currVertex);
                count++;
            }
            d /= count;

            return new CSGPlane(normal, d);
        }

        public static void CalculateTangents(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            var absX = Mathf.Abs(normal.x);
            var absY = Mathf.Abs(normal.y);
            var absZ = Mathf.Abs(normal.z);

            tangent = absY > absX && absY > absZ ? new Vector3(0, 0, 1) : new Vector3(0, -1, 0);
            tangent = Vector3.Cross(normal, tangent).normalized;
            binormal = Vector3.Cross(normal, tangent).normalized;
        }

        public static Vector4 ToVector4(this CSGPlane plane)
            => new();
    }
}
