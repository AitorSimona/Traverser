using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.UIElements
{
    static class UIElementsUtils
    {
        static Dictionary<FloatField, Tuple<float, float>> k_FloatFieldRanges = new Dictionary<FloatField, Tuple<float, float>>();

        const string PackagePath = "Packages/com.unity.kinematica/Editor/";
        const string StylesRoot = PackagePath + "Styles/";
        const string TemplateRoot = PackagePath + "Templates/";

        public static void SetFloatFieldRange(this FloatField field, float min = float.MinValue, float max = float.MaxValue)
        {
            k_FloatFieldRanges[field] = new Tuple<float, float>(min, max);
            field.RegisterValueChangedCallback(OnFloatFieldValueChanged);
            field.RegisterCallback<DetachFromPanelEvent>(OnFloatFieldDetachedFromPanel);

            ClampFloatFieldValue(field.value, field);
        }

        static void OnFloatFieldDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.target is FloatField ff)
            {
                ff.UnregisterValueChangedCallback(OnFloatFieldValueChanged);
                ff.UnregisterCallback<DetachFromPanelEvent>(OnFloatFieldDetachedFromPanel);
                k_FloatFieldRanges.Remove(ff);
            }
        }

        static void OnFloatFieldValueChanged(ChangeEvent<float> evt)
        {
            if (evt.target is FloatField ff)
            {
                ClampFloatFieldValue(evt.newValue, ff);
            }
        }

        static void ClampFloatFieldValue(float value, FloatField ff)
        {
            (float item1, float item2) = k_FloatFieldRanges[ff];
            if (value < item1)
            {
                ff.SetValueWithoutNotify(item1);
            }
            else if (value > item2)
            {
                ff.SetValueWithoutNotify(item2);
            }
        }

        public static void CloneTemplateInto(string templateFilename, VisualElement parent)
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplateRoot + templateFilename).CloneTree(parent);
        }

        public static VisualTreeAsset LoadTemplate(string filename)
        {
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplateRoot + filename);
        }

        public static T Load<T>(string fileName) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(PackagePath + fileName);
        }

        static StyleSheet LoadStylesheet(string path)
        {
            return EditorGUIUtility.Load(StylesRoot + path) as StyleSheet;
        }

        static StyleSheet LoadThemeStylesheet(string path)
        {
            string themedPath;
            if (EditorGUIUtility.isProSkin)
            {
                themedPath = path.Replace(".uss", "Dark.uss");
            }
            else
            {
                themedPath = path.Replace(".uss", "Light.uss");
            }

            return EditorGUIUtility.Load(StylesRoot + themedPath) as StyleSheet;
        }

        public static void ApplyStyleSheet(string path, VisualElement ve)
        {
            var ss = LoadStylesheet(path);
            if (ss == null)
            {
                EditorApplication.delayCall += () => ApplyStyleSheet(path, ve);
                return;
            }

            ve.styleSheets.Add(ss);

            var themedSs = LoadThemeStylesheet(path);
            if (themedSs != null)
            {
                ve.styleSheets.Add(themedSs);
            }
        }
    }
}
