using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Reflection;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(WeightedTransform))]
    class WeightedTransformDrawer : PropertyDrawer
    {
        private const int k_TransformPadding = 6;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var range = fieldInfo.GetCustomAttribute<RangeAttribute>();
            WeightedTransformHelper.WeightedTransformOnGUI(rect, property, range);
        }
    }
}
