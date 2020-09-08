using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    [CustomPropertyDrawer(typeof(BinaryReference))]
    internal class BinaryReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            SerializedProperty assetGuidProp = prop.FindPropertyRelative("assetGuid");
            SerializedProperty val0 = assetGuidProp.FindPropertyRelative("val0");
            SerializedProperty val1 = assetGuidProp.FindPropertyRelative("val1");
            SerializedProperty val2 = assetGuidProp.FindPropertyRelative("val2");
            SerializedProperty val3 = assetGuidProp.FindPropertyRelative("val3");

            BinaryReference binaryRef = new BinaryReference(val0.intValue, val1.intValue, val2.intValue, val3.intValue);

            Asset asset = null;
            string assetGuidStr = "";
            if (binaryRef.IsSet())
            {
                assetGuidStr = binaryRef.assetGuid.GetGuidStr();
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuidStr);
                asset = AssetDatabase.LoadAssetAtPath<Asset>(assetPath);
            }

            string labelStr = asset == null ? label.text : label.text + " (" + assetGuidStr + ")";
            pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(labelStr));
            Asset newAsset = EditorGUI.ObjectField(pos, asset, typeof(Asset), false) as Asset;

            if (newAsset != asset)
            {
                BinaryReference newBinaryRef = new BinaryReference();
                if (newAsset != null)
                {
                    newBinaryRef = newAsset.GetBinaryReference();
                    assetGuidStr = newBinaryRef.assetGuid.GetGuidStr();
                }

                val0.intValue = newBinaryRef.assetGuid.val0;
                val1.intValue = newBinaryRef.assetGuid.val1;
                val2.intValue = newBinaryRef.assetGuid.val2;
                val3.intValue = newBinaryRef.assetGuid.val3;
            }
        }
    }
}
