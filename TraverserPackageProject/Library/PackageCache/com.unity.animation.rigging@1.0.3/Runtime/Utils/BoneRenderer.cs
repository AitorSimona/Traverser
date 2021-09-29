using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The BoneRenderer component is responsible for displaying pickable bones in the Scene View.
    /// This component does nothing during runtime.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Animation Rigging/Setup/Bone Renderer")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/index.html")]
    public class BoneRenderer : MonoBehaviour
    {
    #if UNITY_EDITOR
        public enum BoneShape
        {
            Line,
            Pyramid,
            Box
        };

        public struct TransformPair
        {
            public Transform first;
            public Transform second;
        };

        public BoneShape boneShape = BoneShape.Pyramid;

        public bool drawBones = true;
        public bool drawTripods = false;

        [Range(0.01f, 5.0f)]
        public float boneSize = 1.0f;

        [Range(0.01f, 5.0f)]
        public float tripodSize = 1.0f;

        public Color boneColor = new Color(0f, 0f, 1f, 0.5f);

        [SerializeField]
        private Transform[] m_Transforms;

        private TransformPair[] m_Bones;

        private Transform[] m_Tips;

        public Transform[] transforms
        {
            get { return m_Transforms; }
            set
            {
                m_Transforms = value;
                ExtractBones();
            }
        }

        public TransformPair[] bones { get => m_Bones; }

        public Transform[] tips { get => m_Tips; }

        public delegate void OnAddBoneRendererCallback(BoneRenderer boneRenderer);
        public delegate void OnRemoveBoneRendererCallback(BoneRenderer boneRenderer);

        public static OnAddBoneRendererCallback onAddBoneRenderer;
        public static OnRemoveBoneRendererCallback onRemoveBoneRenderer;

        void OnEnable()
        {
            ExtractBones();
            onAddBoneRenderer?.Invoke(this);
        }

        void OnDisable()
        {
            onRemoveBoneRenderer?.Invoke(this);
        }

        public void Invalidate()
        {
            ExtractBones();
        }

        public void Reset()
        {
            ClearBones();
        }

        public void ClearBones()
        {
            m_Bones = null;
            m_Tips = null;
        }

        public void ExtractBones()
        {
            if (m_Transforms == null || m_Transforms.Length == 0)
            {
                ClearBones();
                return;
            }

            var transformsHashSet = new HashSet<Transform>(m_Transforms);

            var bonesList = new List<TransformPair>(m_Transforms.Length);
            var tipsList = new List<Transform>(m_Transforms.Length);

            for (int i = 0; i < m_Transforms.Length; ++i)
            {
                bool hasValidChildren = false;

                var transform  = m_Transforms[i];
                if (transform == null)
                    continue;

                if (UnityEditor.SceneVisibilityManager.instance.IsHidden(transform.gameObject, false))
                    continue;

                if (transform.childCount > 0)
                {
                    for (var k = 0; k < transform.childCount; ++k)
                    {
                        var childTransform = transform.GetChild(k);

                        if (transformsHashSet.Contains(childTransform))
                        {
                            bonesList.Add(new TransformPair() { first = transform, second = childTransform });
                            hasValidChildren = true;
                        }
                    }
                }

                if (!hasValidChildren)
                {
                    tipsList.Add(transform);
                }
            }

            m_Bones = bonesList.ToArray();
            m_Tips = tipsList.ToArray();
        }
    #endif // UNITY_EDITOR
    }
}
