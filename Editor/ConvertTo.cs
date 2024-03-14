using System.Collections.Generic;

using RealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;

using UnityEditor;

using UnityEngine;
using UnityEngine.ProBuilder;

using System.Linq;

using Shape = RealtimeCSG.Legacy.Shape;
using UnityEngine.WSA;

namespace Terutsa97.ProBuilderUtilities.Editor
{
    public static class ConvertTo
    {
        const string PARENT_FOLDER = "GameObject/ProBuilder Utilities/";
        const int PRIORITY = 1;

        #region Convert ProBuilder to CSG

        const string CONVERT_PROBUILDER_TO_CSG = "Convert to CSG Brush";

        [MenuItem(PARENT_FOLDER + CONVERT_PROBUILDER_TO_CSG, false, PRIORITY)]
        public static void ConvertProBuilderToCSG(MenuCommand command)
        {
            var proBuilderObject = ((GameObject)command.context);
            var proBuilderComponent = proBuilderObject.GetComponent<ProBuilderMesh>();
            CreateBrushFromMesh(proBuilderComponent, out ControlMesh controlMesh, out Shape shape);

            Undo.IncrementCurrentGroup();

            var gameObject = new GameObject(proBuilderComponent.transform.name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Created new CSG Object");

            var brush = Undo.AddComponent<CSGBrush>(gameObject);

            brush.ControlMesh = controlMesh;
            brush.Shape = shape;

            gameObject.transform.SetLocalPositionAndRotation(
                proBuilderObject.transform.position,
                proBuilderObject.transform.rotation);

            Selection.activeTransform = gameObject.transform;
            Undo.SetCurrentGroupName("Created new CSG Object Was Created!");
        }

        [MenuItem(PARENT_FOLDER + CONVERT_PROBUILDER_TO_CSG, true)]
        static bool Validate_ConvertProBuilderToCSG()
            => Selection.activeTransform != null
            && Selection.activeTransform.GetComponent<ProBuilderMesh>() != null;

        private static void CreateBrushFromMesh(ProBuilderMesh proBuilderComponent, out ControlMesh controlMesh, out Shape shape)
        {
            var faces = proBuilderComponent.faces;
            var sharedVertices = proBuilderComponent.sharedVertices.ToList();

            var renderer = proBuilderComponent.GetComponent<MeshRenderer>();

            var mesh = new ControlMesh();
            mesh.Reset();

            mesh.Vertices = sharedVertices
                .Select(s => proBuilderComponent.positions[s.First()])
                .ToArray();

            mesh.Edges = GetHalfEdges(faces, sharedVertices).ToArray();

            // Sets the polygons based on halfedge indices.
            // This logic also reverses the windings to Counter Clockwise
            // so we get correct normals.
            mesh.Polygons = faces
                .Select((f, i) =>
                {
                    // TODO: Only ProBuilder Sphere works, need to fix cones and stuff.
                    if (!f.IsQuad())
                        return new Polygon(f.indexes.ToArray(), i);

                    var quads = f.ToQuad();
                    var newVertexOrder = new int[]
                    {
                        quads[0],
                        quads[3],
                        quads[2],
                        quads[1]
                    };
                    return new Polygon(newVertexOrder, i);
                })
                .ToArray();

            var polygonRange = Enumerable.Range(0, mesh.Polygons.Length);
            shape = new Shape
            {
                Surfaces = polygonRange.Select(i => new Surface()
                {
                    TexGenIndex = i,
                    Plane = CSGUtilities.CalcPolygonPlane(mesh, (short)i)
                }).ToArray(),
                TexGens = polygonRange.Select(i => new TexGen()
                {
                    RenderMaterial = renderer.sharedMaterials[faces[i].submeshIndex],
                    Scale = -faces[i].uv.scale,
                    Translation = faces[i].uv.offset,
                }).ToArray(),
                TexGenFlags = polygonRange.Select(_ => CSGSettings.DefaultTexGenFlags).ToArray()
            };

            for (int i = 0; i < shape.Surfaces.Length; i++)
            {
                Surface surface = shape.Surfaces[i];

                CSGUtilities.CalculateTangents(-surface.Plane.normal, out var tangent, out var biNormal);
                surface.Tangent = tangent;
                surface.BiNormal = biNormal;
            }

            controlMesh = mesh;
        }

        /// <summary>
        /// Gets half edges from a list of faces. Think of half edges like a set
        /// of pointers to the next vertex. This should always be counter-clockwise
        /// otherwise you get wrong normals.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="sharedVertices"></param>
        /// <returns></returns>
        static IList<HalfEdge> GetHalfEdges(IList<Face> faces, IList<SharedVertex> sharedVertices)
        {
            int twinIndexOffset = faces.Count * sharedVertices.Count;

            var vertexToSharedVertex = new Dictionary<int, int>();
            for (int i = 0; i < sharedVertices.Count; i++)
            {
                for (int j = 0; j < sharedVertices[i].Count; j++)
                {
                    vertexToSharedVertex[sharedVertices[i][j]] = i;
                }
            }

            // Collects the half edges, setting vertex index to vertex B and sets the
            // twin index to vertex A + offset so we can properly resolve it later.
            var halfEdgeCollection = new List<HalfEdge>();
            for (short i = 0; i < faces.Count; i++)
            {
                halfEdgeCollection.AddRange(faces[i].edges.Select(e => new HalfEdge
                (
                    polygonIndex: i,
                    twinIndex: twinIndexOffset + vertexToSharedVertex[e.a],
                    vertexIndex: (short)vertexToSharedVertex[e.b]
                )));
            }

            // Goes through each edge and resolves the halfedge index based on the
            // vertex index and the twin index.
            for (int i = 0; i < halfEdgeCollection.Count; i++)
            {
                if (halfEdgeCollection[i].TwinIndex < twinIndexOffset) { continue; }
                var currentEdge = halfEdgeCollection[i];

                var twinedEdge = halfEdgeCollection
                    .Where(e => (e.TwinIndex == currentEdge.VertexIndex + twinIndexOffset) && (currentEdge.TwinIndex == e.VertexIndex + twinIndexOffset))
                    .SingleOrDefault();

                if (twinedEdge.Equals(default)) { continue; }

                var twinedIndex = halfEdgeCollection.IndexOf(twinedEdge);

                halfEdgeCollection[i] = new HalfEdge(currentEdge.PolygonIndex, twinedIndex, currentEdge.VertexIndex);
                halfEdgeCollection[twinedIndex] = new HalfEdge(twinedEdge.PolygonIndex, i, twinedEdge.VertexIndex, false);
            }

            return halfEdgeCollection;
        }
        #endregion
    }
}
