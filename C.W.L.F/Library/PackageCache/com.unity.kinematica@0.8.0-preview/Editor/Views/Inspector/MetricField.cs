using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    internal class MetricField : VisualElement
    {
        const string k_ArrayEntryContentsStyle = "arrayEntryContents";
        internal const string k_MetricFieldClass = "metricField";

        public MetricField(Asset asset, SerializedProperty metricProperty, int metricIndex)
        {
            foreach (var child in metricProperty.GetChildren())
            {
                if (child.name.Equals(nameof(Asset.Metric.joints)))
                {
                    var jointEditor = new JointField(asset, child.Copy(), asset.Metrics[metricIndex]);
                    Add(jointEditor);
                }
                else
                {
                    Add(new PropertyField(child));
                }
            }

            AddToClassList(k_ArrayEntryContentsStyle);
            AddToClassList(k_MetricFieldClass);
        }

        internal void SetInputEnabled(bool enable)
        {
            foreach (var input in Children())
            {
                if (input is JointField jf)
                {
                    jf.SetInputEnabled(enable);
                }
                else
                {
                    input.SetEnabled(enable);
                }
            }
        }
    }
}
