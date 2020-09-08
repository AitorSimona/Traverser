using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using Metric = Unity.Kinematica.Editor.Asset.Metric;

namespace Unity.Kinematica.Editor.Inspectors
{
    [CustomPropertyDrawer(typeof(Metric.TagTypeMask))]
    class TagTypePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Asset asset = property.serializedObject.targetObject as Asset;
            var metric = GetMetric(asset, property);
            int mask = metric.TagTypes.MaskTypeNames();

            List<string> tagNames = TagAttribute.GetAllDescriptions();
            EditorGUI.BeginChangeCheck();

            int newValue = EditorGUI.MaskField(position, new GUIContent("Tag Types", "Tag types that apply to this metric."), mask, tagNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                OnTagChanged(mask, newValue, property);
            }
        }

        void OnTagChanged(int oldValue, int newValue, SerializedProperty property)
        {
            Asset asset = property?.serializedObject.targetObject as Asset;
            int metricIndex = GetMetricIndex(property);
            int change = oldValue ^ newValue;

            List<Type> tagTypes = TagAttribute.GetVisibleTypesInInspector().ToList();

            if ((change & (change - 1)) == 0) // power of 2, only one change, perform below to avoid looping over all tags
            {
                var index = (int)Math.Log(change, 2);
                if (index < tagTypes.Count)
                {
                    Type tagType = tagTypes[index];
                    ApplyTagChange(asset, metricIndex, newValue, index, tagType);
                }
            }
            else // multiple changes
            {
                for (int index = 0; index < TagAttribute.GetAllDescriptions().Count; ++index)
                {
                    if ((change & (1 << index)) != 0)
                    {
                        Type tagType = tagTypes[index];
                        ApplyTagChange(asset, metricIndex, newValue, index, tagType);
                    }
                }
            }
        }

        void ApplyTagChange(Asset asset, int metricIndex, int newValue, int index, Type tagType)
        {
            if ((newValue & (1 << index)) == 0)
            {
                asset.RemoveTagFromMetric(metricIndex, tagType);
            }
            else
            {
                asset.AssignTagToMetric(metricIndex, tagType);
            }
        }

        readonly Regex regex = new Regex($@"{Asset.k_MetricsPropertyPath}\.Array.data\[(?<index>\d+)\]", RegexOptions.Compiled);

        Metric GetMetric(Asset asset, SerializedProperty property)
        {
            int index = GetMetricIndex(property);

            if (index < 0)
            {
                return null;
            }

            return asset.Metrics[index];
        }

        int GetMetricIndex(SerializedProperty property)
        {
            foreach (Match match in regex.Matches(property.propertyPath))
            {
                return int.Parse(match.Groups["index"].Value);
            }

            return -1;
        }
    }
}
