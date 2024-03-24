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
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

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
            && Selection.activeTransform.TryGetComponent<ProBuilderMesh>(out var proBuilderMesh)
            && proBuilderMesh.faces.All(f => f.IsQuad());

        static void CreateBrushFromMesh(ProBuilderMesh proBuilderComponent, out ControlMesh controlMesh, out Shape shape)
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
                    {
                        var tris = f.indexes.ToArray();
                        var newVertexOrderTris = new int[]
                        {
                            tris[1],
                            tris[2],
                            tris[0]
                        };
                        return new Polygon(newVertexOrderTris, i);
                    }

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
                    RotationAngle = faces[i].uv.rotation,
                    SmoothingGroup = (uint)faces[i].smoothingGroup
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
                if (twinedIndex == -1) { continue; }

                halfEdgeCollection[i] = new HalfEdge(currentEdge.PolygonIndex, twinedIndex, currentEdge.VertexIndex);
                halfEdgeCollection[twinedIndex] = new HalfEdge(twinedEdge.PolygonIndex, i, twinedEdge.VertexIndex, false);
            }

            return halfEdgeCollection;
        }
        #endregion

        #region
        const string CONVERT_CST_TO_PROBUILDER = "Convert to Probuilder Model";

        [MenuItem(PARENT_FOLDER + CONVERT_CST_TO_PROBUILDER, false, PRIORITY)]
        public static void ConvertCSGToProBuilder(MenuCommand command)
        {
            var selectedObject = ((GameObject)command.context);

            if (selectedObject.TryGetComponent<CSGBrush>(out var brush))
            {
                var vertices = brush.ControlMesh.Vertices
                    .Select((v, i) => new Vertex()
                    {
                        position = v,
                        tangent = brush.Shape.Surfaces[i / 4].Plane.ToVector4()
                    })
                    .ToList();

                var materials = brush.Shape.TexGens
                    .Select(t => t.RenderMaterial)
                    .ToList();

                List<Face> faces = BrushToFaces(brush.ControlMesh, brush.Shape, materials).ToList();
                vertices = MakeVerticesUniquePerFace(faces, vertices);

                Undo.IncrementCurrentGroup();
                var gameObject = ProBuilderMesh.Create(vertices, faces.ToList(),
                    materials: materials,
                    sharedVertices: SharedVertex.GetSharedVerticesWithPositions(vertices.Select(v => v.position).ToList())
                );

                gameObject.name = selectedObject.name;
                gameObject.transform.position = selectedObject.transform.position;
                Selection.activeGameObject = gameObject.gameObject;
            }
            else if (selectedObject.TryGetComponent<CSGModel>(out var model))
            {
                // TODO: Work on it another daty...
                // Look into making it generate a mesh and going from there...
            }
        }

        [MenuItem(PARENT_FOLDER + CONVERT_CST_TO_PROBUILDER, true)]
        static bool Validate_ConvertCSGToProBuilder()
            => Selection.activeTransform != null
            && (Selection.activeTransform.TryGetComponent<CSGBrush>(out var _)
            || Selection.activeTransform.TryGetComponent<CSGModel>(out var _));

        static List<Vertex> MakeVerticesUniquePerFace(List<Face> faces, List<Vertex> vertices)
        {
            var uniqueVerticesPerFaceIndex = new List<Vertex>();
            for (int i = 0; i < faces.Count(); i++)
            {
                var faceVertices = faces[i].distinctIndexes.Select(j => vertices[j]);

                var currentCount = uniqueVerticesPerFaceIndex.Count();
                uniqueVerticesPerFaceIndex.AddRange(faceVertices);

                var oldIndexToNew = faceVertices.Select((v, i) => uniqueVerticesPerFaceIndex[currentCount + i])
                    .Select((v, i) => (key: vertices.IndexOf(v), value: i))
                    .ToDictionary(k => k.key, k => k.value + currentCount);

                faces[i].SetIndexes(faces[i].indexes.Select(i => oldIndexToNew[i]));
            }

            return uniqueVerticesPerFaceIndex;
        }

        static IEnumerable<Face> BrushToFaces(ControlMesh controlMesh, Shape shape, List<Material> materials)
        {
            var vertices = new List<Vector3>(controlMesh.Vertices);

            IEnumerable<int[]> verticesPerFace = controlMesh.Polygons
                .Select(p => controlMesh.GetVertices(p.EdgeIndices))
                .Select(p => p.Select(i => vertices.IndexOf(i)).ToArray())
                .ToList();

            return verticesPerFace.Select((v, i) => new Face(new int[6] { v[3], v[0], v[2], v[0], v[1], v[2] })
            {
                submeshIndex = materials.IndexOf(shape.TexGens[i].RenderMaterial),
                uv = new AutoUnwrapSettings()
                {
                    offset = shape.TexGens[i].Translation,
                    scale = -shape.TexGens[i].Scale,
                    rotation = shape.TexGens[i].RotationAngle,
                    useWorldSpace = shape.TexGenFlags[i].HasFlag(TexGenFlags.WorldSpaceTexture),
                    fill = Fill.Fit
                },
                manualUV = true,
                smoothingGroup = (int)shape.TexGens[i].SmoothingGroup
            });
        }
        #endregion
    }
}
