using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [Serializable]
    internal class RigEffector : ScriptableObject, IRigEffector
    {
        [SerializeField] private RigEffectorData m_Data;

        public Transform transform
        {
            get => m_Data.transform;
        }

        public bool visible
        {
            get => m_Data.visible;
            set => m_Data.visible = value;
        }

        private static int s_ButtonHash = "RigEffector".GetHashCode();

        public void Initialize(RigEffectorData data)
        {
            m_Data = data;
        }

        private static Material s_Material;

        public static Material material
        {
            get
            {
                if (!s_Material)
                {
                    var shader = EditorHelper.LoadShader("BoneHandles.shader");
                    s_Material = new Material(shader);
                    s_Material.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Material;
            }
        }

        public static RigEffectorData.Style defaultStyle
        {
            get
            {
                var style = new RigEffectorData.Style()
                {
                    shape =  EditorHelper.LoadShape("LocatorEffector.asset"),
                    color = new Color(1f, 0f, 0f, 0.5f),
                    size = 0.10f,
                    position = Vector3.zero,
                    rotation = Vector3.zero
                };

                return style;
            }
        }

        public void OnSceneGUI()
        {
            if (!m_Data.visible)
                return;

            // Might happen if we delete transform while effector still exists.
            if (transform == null)
                return;

            var style = m_Data.style;

            // Disregard effectors without shapes.
            if (style.shape == null)
                return;

            if (SceneVisibilityManager.instance.IsHidden(transform.gameObject, false))
                return;

            int id = GUIUtility.GetControlID(s_ButtonHash, FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    {
                        HandleUtility.AddControl(id, DistanceToEffector(transform, style.shape, style.position, style.rotation, style.size));
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (evt.alt)
                            break;

                        if (HandleUtility.nearestControl == id && evt.button == 0)
                        {
                            GameObject targetGameObject = transform.gameObject;
                            if (!SceneVisibilityManager.instance.IsPickingDisabled(targetGameObject, false))
                            {
                                GUIUtility.hotControl = id; // Grab mouse focus
                                EditorHelper.HandleClickSelection(targetGameObject, evt);
                                evt.Use();
                            }
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (!evt.alt && GUIUtility.hotControl == id)
                        {
                            GameObject targetGameObject = transform.gameObject;
                            if (!SceneVisibilityManager.instance.IsPickingDisabled(targetGameObject, false))
                            {
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.objectReferences = new UnityEngine.Object[] {transform};
                                DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(transform));

                                GUIUtility.hotControl = 0;

                                evt.Use();
                            }
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                case EventType.Repaint:
                    {
                        Matrix4x4 matrix = GetEffectorMatrix(transform, style.position, style.rotation, style.size);

                        Color highlight = style.color;

                        bool hoveringEffector = GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id;
                        hoveringEffector = hoveringEffector &&
                                           !SceneVisibilityManager.instance.IsPickingDisabled(transform.gameObject, false);

                        if (hoveringEffector)
                        {
                            highlight = Handles.preselectionColor;
                        }
                        else if (Selection.Contains(transform.gameObject) || Selection.activeObject == transform.gameObject)
                        {
                            highlight = Handles.selectedColor;
                        }

                        Material mat = material;

                        var shapeHighlight = MeshHasWireframeShapes(style.shape) ? style.color : highlight;
                        var wireHighlight = new Color(highlight.r, highlight.g, highlight.b, 1f);

                        if (style.shape.subMeshCount > 0)
                        {
                            //  Draw every sub meshes separately to control highlight vs shape colors.
                            for (int i = 0; i < style.shape.subMeshCount; ++i)
                            {
                                MeshTopology topology = style.shape.GetTopology(i);
                                bool isFilled = (topology == MeshTopology.Triangles || topology == MeshTopology.Quads);

                                mat.SetColor("_Color", isFilled ? shapeHighlight : wireHighlight);
                                mat.SetPass(0);

                                Graphics.DrawMeshNow(style.shape, matrix, i);
                            }
                        }
                        else
                        {
                            MeshTopology topology = style.shape.GetTopology(0);
                            bool isFilled = (topology == MeshTopology.Triangles || topology == MeshTopology.Quads);

                            mat.SetColor("_Color", isFilled ? shapeHighlight : wireHighlight);
                            mat.SetPass(0);

                            Graphics.DrawMeshNow(style.shape, matrix);
                        }
                    }
                    break;
            }
        }

        private bool FindMeshTopology(Mesh shape, IEnumerable<MeshTopology> topologies)
        {
            if (shape.subMeshCount > 0)
            {
                for (int i = 0; i < shape.subMeshCount; ++i)
                {
                    MeshTopology topology = shape.GetTopology(i);
                    foreach (var topologyQuery in topologies)
                    {
                        if (topologyQuery == topology)
                            return true;
                    }
                }
            }
            else
            {
                var topology = shape.GetTopology(0);
                foreach (var topologyQuery in topologies)
                {
                    if (topologyQuery == topology)
                            return true;
                }
            }

            return false;
        }

        private bool MeshHasFilledShapes(Mesh shape) =>
            FindMeshTopology(shape, new MeshTopology[] {MeshTopology.Triangles, MeshTopology.Quads});

        private bool MeshHasWireframeShapes(Mesh shape) =>
            FindMeshTopology(shape, new MeshTopology[] {MeshTopology.Lines, MeshTopology.LineStrip});

        private Matrix4x4 GetEffectorMatrix(Transform transform, Vector3 position, Vector3 rotation, float size)
        {
            return Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one) * Matrix4x4.TRS(position, Quaternion.Euler(rotation), new Vector3(size, size, size));
        }

        private float DistanceToEffector(Transform transform, Mesh shape, Vector3 position, Vector3 rotation, float size)
        {
            Matrix4x4 matrix = GetEffectorMatrix(transform, position, rotation, size);
            Vector3 origin = matrix.MultiplyPoint(Vector3.zero);

            if (shape == null)
                return HandleUtility.DistanceToCircle(origin, size * 0.5f);

            if (MeshHasFilledShapes(shape))
            {
                var bounds = shape.bounds;
                var extents = Mathf.Max( Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z);
                return HandleUtility.DistanceToCircle(origin + bounds.center * size, extents * size);
            }

            float nearestDistance = Mathf.Infinity;

            Vector3[] vertices = shape.vertices;

            for (int i = 0; i < shape.subMeshCount; ++i)
            {
                MeshTopology topology = shape.GetTopology(i);
                if (topology == MeshTopology.Lines)
                {
                    int[] indices = shape.GetIndices(i);

                    int count = 0;
                    while (count < indices.Length)
                    {
                        float d = HandleUtility.DistanceToLine(matrix.MultiplyPoint(vertices[indices[count]]), matrix.MultiplyPoint(vertices[indices[count+1]]));
                        if (d < nearestDistance)
                            nearestDistance = d;

                        count += 2;
                    }
                }
                else if (topology == MeshTopology.LineStrip)
                {
                    int[] indices = shape.GetIndices(i);

                    if (indices.Length > 0)
                    {
                        int count = 0;
                        float d = 0f;
                        Vector3 v0 = matrix.MultiplyPoint(vertices[indices[count]]);
                        Vector3 v1 = v0;

                        while (++count < indices.Length)
                        {
                            Vector3 v2 = matrix.MultiplyPoint(vertices[indices[count]]);

                            d = HandleUtility.DistanceToLine(v1, v2);
                            if (d < nearestDistance)
                                nearestDistance = d;

                            v1 = v2;
                        }

                        d = HandleUtility.DistanceToLine(v1, v0);
                        if (d < nearestDistance)
                            nearestDistance = d;
                    }
                }
            }

            return nearestDistance;
        }
    }
}
