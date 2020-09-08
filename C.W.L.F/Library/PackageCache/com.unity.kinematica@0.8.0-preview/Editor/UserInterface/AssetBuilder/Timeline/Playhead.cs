using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ColorUtility = Unity.SnapshotDebugger.ColorUtility;

namespace Unity.Kinematica.Editor
{
    class Playhead : VisualElement
    {
        VisualElement m_Handle;
        bool m_DebugPlayhead;

        public Playhead(bool debugPlayhead)
        {
            m_DebugPlayhead = debugPlayhead;
            style.display = DisplayStyle.None;
            AddToClassList("playHead");

            m_Handle = new VisualElement() { name = "handle" };
            m_Handle.AddToClassList("playHeadHandle");

            m_ShowHandle = !EditorApplication.isPlaying;
            m_Handle.generateVisualContent += GenerateTickHeadContent;

            Add(m_Handle);
        }

        bool m_ShowHandle;

        public bool ShowHandle
        {
            set
            {
                if (m_ShowHandle != value)
                {
                    m_ShowHandle = value;
                    m_Handle.style.display = m_ShowHandle ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        public void GenerateTickHeadContent(MeshGenerationContext context)
        {
            const float tipY = 20f;
            const float height = 6f;
            const float width = 11f;
            const float middle = 6f;

            Color color;
            if (EditorGUIUtility.isProSkin)
            {
                color = m_DebugPlayhead ? ColorUtility.FromHtmlString("#234A6C") : Color.white;
            }
            else
            {
                color = m_DebugPlayhead ? ColorUtility.FromHtmlString("#2E5B8D") : Color.white;
            }

            MeshWriteData mesh = context.Allocate(3, 3);
            Vertex[] vertices = new Vertex[3];
            vertices[0].position = new Vector3(middle, tipY, Vertex.nearZ);
            vertices[1].position = new Vector3(width, tipY - height, Vertex.nearZ);
            vertices[2].position = new Vector3(0, tipY - height, Vertex.nearZ);

            vertices[0].tint = color;
            vertices[1].tint = color;
            vertices[2].tint = color;

            mesh.SetAllVertices(vertices);
            mesh.SetAllIndices(new ushort[] { 0, 2, 1 });
        }
    }
}
