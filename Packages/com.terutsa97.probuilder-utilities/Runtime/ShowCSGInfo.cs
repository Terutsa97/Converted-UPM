using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RealtimeCSG.Components;
using RealtimeCSG.Legacy;

using UnityEditor;

using UnityEngine;

namespace Terutsa97.ProBuilderUtilities.Editor
{
    public class ShowCSGInfo : MonoBehaviour
    {
        [field: SerializeField] public CSGBrush Brush { get; set; }

        [SerializeField] int SelectedFace;

        void OnDrawGizmos()
        {
            if (Brush == null) { return; }

            ControlMesh controlMesh = Brush.ControlMesh;

            Vector3[] vertices = controlMesh.Vertices;
            Vector3 position = Brush.transform.position;

            Vector3[] facePositions = controlMesh.Polygons
                .Select(p => p.EdgeIndices)
                .Select((k) => GetCenterPoint(k.Select(j => controlMesh.GetVertex(j))))
                .ToArray();

            Vector3[] edgePositions = controlMesh.Edges
                .Select(e => (vertices[e.VertexIndex] + vertices[controlMesh.GetVertexIndex(e.TwinIndex)]) / 2)
                .ToArray();

            var guiStyleBlack = new GUIStyle()
            {
                fontSize = 18,
                normal = new GUIStyleState() { textColor = Color.black },
                contentOffset = new Vector2(1, 1)
            };

            var guiStyleWhite = new GUIStyle()
            {
                fontSize = 18,
                normal = new GUIStyleState() { textColor = Color.white }
            };

            var guiStyleGrey = new GUIStyle()
            {
                fontSize = 18,
                normal = new GUIStyleState() { textColor = Color.white * 1.5f }
            };

            var guiStyleDarkGrey = new GUIStyle()
            {
                fontSize = 18,
                normal = new GUIStyleState() { textColor = Color.white / 2 }
            };

            Gizmos.color = Color.blue;
            for (int i = 0; i < edgePositions.Length; i++)
            {
                if (controlMesh.Edges[i].PolygonIndex != SelectedFace) { continue; }

                var vertexIndex = controlMesh.Edges[i].VertexIndex;
                var vertexTwinIndex = controlMesh.GetVertexIndex(controlMesh.Edges[i].TwinIndex);
                var text = new StringBuilder($"e={i}|{vertexIndex}-{vertexTwinIndex} ({edgePositions[i].x}, {edgePositions[i].y}, {edgePositions[i].z})");

                Gizmos.DrawSphere(position + edgePositions[i], 0.0125f);
                Handles.Label(position + edgePositions[i], text.ToString(), guiStyleBlack);
                Handles.Label(position + edgePositions[i], text.ToString(), guiStyleDarkGrey);
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < vertices.Length; i++)
            {
                Gizmos.DrawSphere(position + vertices[i], 0.0125f);
                string text = $"v={i} ({vertices[i].x}, {vertices[i].y}, {vertices[i].z})";

                Handles.Label(position + vertices[i], text, guiStyleBlack);
                Handles.Label(position + vertices[i], text, guiStyleGrey);
            }

            Gizmos.color = Color.green;
            for (int i = 0; i < facePositions.Length; i++)
            {
                if (i != SelectedFace) { continue; }

                var currentPolygonEdgeIndices = controlMesh.Polygons[i].EdgeIndices;

                Gizmos.DrawSphere(position + facePositions[i], 0.0125f);
                string text = $"f={i} ({facePositions[i].x}, {facePositions[i].y}, {facePositions[i].z})"
                    + $" ({string.Join(',', currentPolygonEdgeIndices.Select(e => controlMesh.GetVertexIndex(e)))})";

                Handles.Label(position + facePositions[i], text, guiStyleBlack);
                Handles.Label(position + facePositions[i], text, guiStyleWhite);
            }
        }

        Vector3 GetCenterPoint(IEnumerable<Vector3> points)
            => points.Count() > 0
                ? new Vector3(points.Sum(p => p.x), points.Sum(p => p.y), points.Sum(p => p.z)) / points.Count()
                : Vector3.zero;
    }
}
