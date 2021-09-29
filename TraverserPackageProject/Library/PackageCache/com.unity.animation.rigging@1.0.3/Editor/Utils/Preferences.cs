using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.Rigging
{
    internal static class Preferences
    {
        static readonly string k_Prefix = "com.unity.animation.rigging";

        static readonly string k_BakeToConstraintPrefix = k_Prefix + ".bakeToConstraint";
        static readonly string k_BakeToSkeletonPrefix = k_Prefix + ".bakeToSkeleton";

        static readonly string k_UnrollRotation = ".unrollRotation";
        static readonly string k_KeyReduceSuffix = ".keyReduceEnable";
        static readonly string k_KeyReducePositionErrorSuffix = ".keyReducePositionError";
        static readonly string k_KeyReduceRotationErrorSuffix = ".keyReduceRotationError";
        static readonly string k_KeyReduceScaleErrorSuffix = ".keyReduceScaleError";
        static readonly string k_KeyReduceFloatErrorSuffix = ".keyReduceFloatError";

        static readonly string k_RemoveCurvesSuffix = ".removeCurves";
        static readonly string k_ForceWeightSuffix = ".forceWeight";

        static CurveFilterOptions m_BakeToConstraintCurveFilterOptions;
        static CurveFilterOptions m_BakeToSkeletonCurveFilterOptions;

        static bool m_BakeToConstraintAndRemoveCurves;
        static bool m_BakeToSkeletonAndRemoveCurves;

        static bool m_ForceConstraintWeightOnBake;

        static Preferences()
        {
            m_BakeToConstraintCurveFilterOptions = new CurveFilterOptions()
            {
                unrollRotation = EditorPrefs.GetBool(k_BakeToConstraintPrefix + k_UnrollRotation, true),
                keyframeReduction = EditorPrefs.GetBool(k_BakeToConstraintPrefix + k_KeyReduceSuffix, true),
                positionError = EditorPrefs.GetFloat(k_BakeToConstraintPrefix + k_KeyReducePositionErrorSuffix, 0.5f),
                rotationError = EditorPrefs.GetFloat(k_BakeToConstraintPrefix + k_KeyReduceRotationErrorSuffix, 0.5f),
                scaleError = EditorPrefs.GetFloat(k_BakeToConstraintPrefix + k_KeyReduceScaleErrorSuffix, 0.5f),
                floatError = EditorPrefs.GetFloat(k_BakeToConstraintPrefix + k_KeyReduceFloatErrorSuffix, 0.5f)
            };

            m_BakeToSkeletonCurveFilterOptions = new CurveFilterOptions()
            {
                unrollRotation = EditorPrefs.GetBool(k_BakeToSkeletonPrefix + k_UnrollRotation, true),
                keyframeReduction = EditorPrefs.GetBool(k_BakeToSkeletonPrefix + k_KeyReduceSuffix, true),
                positionError = EditorPrefs.GetFloat(k_BakeToSkeletonPrefix + k_KeyReducePositionErrorSuffix, 0.5f),
                rotationError = EditorPrefs.GetFloat(k_BakeToSkeletonPrefix + k_KeyReduceRotationErrorSuffix, 0.5f),
                scaleError = EditorPrefs.GetFloat(k_BakeToSkeletonPrefix + k_KeyReduceScaleErrorSuffix, 0.5f),
                floatError = EditorPrefs.GetFloat(k_BakeToSkeletonPrefix + k_KeyReduceFloatErrorSuffix, 0.5f)
            };

            m_BakeToConstraintAndRemoveCurves = EditorPrefs.GetBool(k_BakeToConstraintPrefix + k_RemoveCurvesSuffix, false);
            m_BakeToSkeletonAndRemoveCurves = EditorPrefs.GetBool(k_BakeToSkeletonPrefix + k_RemoveCurvesSuffix, false);

            m_ForceConstraintWeightOnBake = EditorPrefs.GetBool(k_Prefix + k_ForceWeightSuffix, true);
        }

        public static void SetDefaultValues()
        {
            var defaultOptions = new CurveFilterOptions()
            {
                unrollRotation = true,
                keyframeReduction = true,
                positionError = .5f,
                rotationError = .5f,
                scaleError = .5f,
                floatError = .5f
            };

            bakeToConstraintCurveFilterOptions = defaultOptions;
            bakeToSkeletonCurveFilterOptions = defaultOptions;

            bakeToConstraintAndRemoveCurves = false;
            bakeToSkeletonAndRemoveCurves = false;

            forceConstraintWeightOnBake = true;
        }

        public static CurveFilterOptions bakeToConstraintCurveFilterOptions
        {
            get => m_BakeToConstraintCurveFilterOptions;
            set
            {
                m_BakeToConstraintCurveFilterOptions = value;

                EditorPrefs.SetBool(k_BakeToConstraintPrefix + k_UnrollRotation, m_BakeToConstraintCurveFilterOptions.unrollRotation);
                EditorPrefs.SetBool(k_BakeToConstraintPrefix + k_KeyReduceSuffix, m_BakeToConstraintCurveFilterOptions.keyframeReduction);
                EditorPrefs.SetFloat(k_BakeToConstraintPrefix + k_KeyReducePositionErrorSuffix, m_BakeToConstraintCurveFilterOptions.positionError);
                EditorPrefs.SetFloat(k_BakeToConstraintPrefix + k_KeyReduceRotationErrorSuffix, m_BakeToConstraintCurveFilterOptions.rotationError);
                EditorPrefs.SetFloat(k_BakeToConstraintPrefix + k_KeyReduceScaleErrorSuffix, m_BakeToConstraintCurveFilterOptions.scaleError);
                EditorPrefs.SetFloat(k_BakeToConstraintPrefix + k_KeyReduceFloatErrorSuffix, m_BakeToConstraintCurveFilterOptions.floatError);
            }
        }

        public static bool bakeToConstraintAndRemoveCurves
        {
            get => m_BakeToConstraintAndRemoveCurves;
            set
            {
                m_BakeToConstraintAndRemoveCurves = value;
                EditorPrefs.SetBool(k_BakeToConstraintPrefix + k_RemoveCurvesSuffix, value);
            }
        }

        public static CurveFilterOptions bakeToSkeletonCurveFilterOptions
        {
            get => m_BakeToSkeletonCurveFilterOptions;
            set
            {
                m_BakeToSkeletonCurveFilterOptions = value;

                EditorPrefs.SetBool(k_BakeToSkeletonPrefix + k_UnrollRotation, m_BakeToSkeletonCurveFilterOptions.unrollRotation);
                EditorPrefs.SetBool(k_BakeToSkeletonPrefix + k_KeyReduceSuffix, m_BakeToSkeletonCurveFilterOptions.keyframeReduction);
                EditorPrefs.SetFloat(k_BakeToSkeletonPrefix + k_KeyReducePositionErrorSuffix, m_BakeToSkeletonCurveFilterOptions.positionError);
                EditorPrefs.SetFloat(k_BakeToSkeletonPrefix + k_KeyReduceRotationErrorSuffix, m_BakeToSkeletonCurveFilterOptions.rotationError);
                EditorPrefs.SetFloat(k_BakeToSkeletonPrefix + k_KeyReduceScaleErrorSuffix, m_BakeToSkeletonCurveFilterOptions.scaleError);
                EditorPrefs.SetFloat(k_BakeToSkeletonPrefix + k_KeyReduceFloatErrorSuffix, m_BakeToSkeletonCurveFilterOptions.floatError);
            }
        }

        public static bool bakeToSkeletonAndRemoveCurves
        {
            get => m_BakeToSkeletonAndRemoveCurves;
            set
            {
                m_BakeToSkeletonAndRemoveCurves = value;
                EditorPrefs.SetBool(k_BakeToSkeletonPrefix + k_RemoveCurvesSuffix, value);
            }
        }

        public static bool forceConstraintWeightOnBake
        {
            get => m_ForceConstraintWeightOnBake;
            set
            {
                m_ForceConstraintWeightOnBake = value;
                EditorPrefs.SetBool(k_Prefix + k_ForceWeightSuffix, value);
            }
        }
    }

    class PreferencesProvider : SettingsProvider
    {
        private class Styles
        {
            public static readonly int marginLeft = 10;
            public static readonly int marginTop = 10;
            public static readonly int majorSpacing = 10;
            public static readonly int minorSpacing = 5;
            public static readonly int resetButtonWidth = 120;

            public static readonly GUIContent forceWeightsLabel = EditorGUIUtility.TrTextContent("Force Weights On Bake", "Remove weight curves and set constraints weights to zero or one after baking operation.");
            public static readonly GUIContent resetPreferencesButton = EditorGUIUtility.TrTextContent("Use Defaults", "Reset all the Animation Rigging preferenecs back to default settings.");

            public static readonly GUIContent bakeToConstraintLabel = EditorGUIUtility.TrTextContent("Transfer Motion To Constraint");
            public static readonly GUIContent bakeToSkeletonLabel = EditorGUIUtility.TrTextContent("Transfer Motion To Skeleton");

            public static readonly GUIContent unrollRotationLabel = EditorGUIUtility.TrTextContent("Unroll Rotation", "Unroll rotation will adjust rotation to avoid discontinuity in between keyframes generated by baking operations.");
            public static readonly GUIContent keyReduceEnableLabel = EditorGUIUtility.TrTextContent("Apply keyframe reduction", "Keyframe Reduction will remove unecessary keys in animation curves generated by baking operations.");
            public static readonly GUIContent keyReducePositionErrorLabel = EditorGUIUtility.TrTextContent("Position Error", "Tolerance used in keyframe reduction for position values (percentage value between 0 and 100).");
            public static readonly GUIContent keyReduceRotationErrorLabel = EditorGUIUtility.TrTextContent("Rotation Error", "Tolerance used in keyframe reduction for rotation values (percentage value between 0 and 100).");
            public static readonly GUIContent keyReduceScaleErrorLabel = EditorGUIUtility.TrTextContent("Scale Error", "Tolerance used in keyframe reduction for scale values (percentage value between 0 and 100).");
            public static readonly GUIContent keyReduceFloatErrorLabel = EditorGUIUtility.TrTextContent("Float Error", "Tolerance used in keyframe reduction for float values (percentage value between 0 and 100).");

            public static readonly GUIContent removeCurvesLabel =  EditorGUIUtility.TrTextContent("Remove Curves", "Original curves are removed after baking operation.");
        }

        public PreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
        }

        public override void OnGUI(string searchContext)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Styles.marginLeft);
            GUILayout.BeginVertical();
            GUILayout.Space(Styles.marginTop);

            // Force weights
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(Styles.forceWeightsLabel, Preferences.forceConstraintWeightOnBake);
            if (EditorGUI.EndChangeCheck())
                Preferences.forceConstraintWeightOnBake = newValue;

            GUILayout.Space(Styles.majorSpacing);

            // Transfer to constraint
            EditorGUILayout.LabelField(Styles.bakeToConstraintLabel, EditorStyles.boldLabel);

            // - Remove curves
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.Toggle(Styles.removeCurvesLabel, Preferences.bakeToConstraintAndRemoveCurves);
            if (EditorGUI.EndChangeCheck())
                Preferences.bakeToConstraintAndRemoveCurves = newValue;

            // - Keyframe reduction
            EditorGUI.BeginChangeCheck();

            var curveFilterOptions = Preferences.bakeToConstraintCurveFilterOptions;
            curveFilterOptions.unrollRotation = EditorGUILayout.Toggle(Styles.unrollRotationLabel, curveFilterOptions.unrollRotation);
            curveFilterOptions.keyframeReduction = EditorGUILayout.Toggle(Styles.keyReduceEnableLabel, curveFilterOptions.keyframeReduction);
            using (new EditorGUI.DisabledScope(!curveFilterOptions.keyframeReduction))
            {
                EditorGUI.indentLevel++;
                curveFilterOptions.positionError = EditorGUILayout.Slider(Styles.keyReducePositionErrorLabel, curveFilterOptions.positionError, 0f, 100f);
                curveFilterOptions.rotationError = EditorGUILayout.Slider(Styles.keyReduceRotationErrorLabel, curveFilterOptions.rotationError, 0f, 100f);
                curveFilterOptions.scaleError = EditorGUILayout.Slider(Styles.keyReduceScaleErrorLabel, curveFilterOptions.scaleError, 0f, 100f);
                curveFilterOptions.floatError = EditorGUILayout.Slider(Styles.keyReduceFloatErrorLabel, curveFilterOptions.floatError, 0f, 100f);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                Preferences.bakeToConstraintCurveFilterOptions = curveFilterOptions;

            GUILayout.Space(Styles.majorSpacing);

            // Transfer to Skeleton
            EditorGUILayout.LabelField(Styles.bakeToSkeletonLabel, EditorStyles.boldLabel);

            // - Remove curves
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.Toggle(Styles.removeCurvesLabel, Preferences.bakeToSkeletonAndRemoveCurves);
            if (EditorGUI.EndChangeCheck())
                Preferences.bakeToSkeletonAndRemoveCurves = newValue;

            // - Keyframe reduction
            EditorGUI.BeginChangeCheck();

            curveFilterOptions = Preferences.bakeToSkeletonCurveFilterOptions;
            curveFilterOptions.unrollRotation = EditorGUILayout.Toggle(Styles.unrollRotationLabel, curveFilterOptions.unrollRotation);
            curveFilterOptions.keyframeReduction = EditorGUILayout.Toggle(Styles.keyReduceEnableLabel, curveFilterOptions.keyframeReduction);
            using (new EditorGUI.DisabledScope(!curveFilterOptions.keyframeReduction))
            {
                EditorGUI.indentLevel++;
                curveFilterOptions.positionError = EditorGUILayout.Slider(Styles.keyReducePositionErrorLabel, curveFilterOptions.positionError, 0f, 100f);
                curveFilterOptions.rotationError = EditorGUILayout.Slider(Styles.keyReduceRotationErrorLabel, curveFilterOptions.rotationError, 0f, 100f);
                curveFilterOptions.scaleError = EditorGUILayout.Slider(Styles.keyReduceScaleErrorLabel, curveFilterOptions.scaleError, 0f, 100f);
                curveFilterOptions.floatError = EditorGUILayout.Slider(Styles.keyReduceFloatErrorLabel, curveFilterOptions.floatError, 0f, 100f);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                Preferences.bakeToSkeletonCurveFilterOptions = curveFilterOptions;

            GUILayout.Space(Styles.majorSpacing);

            // Reset to defaults
            if (GUILayout.Button(Styles.resetPreferencesButton, GUILayout.Width(Styles.resetButtonWidth)))
                Preferences.SetDefaultValues();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        [SettingsProvider]
        public static SettingsProvider CreateAnimationRiggingProjectSettingProvider()
        {
            return new PreferencesProvider(
                "Preferences/Animation Rigging",
                SettingsScope.User,
                GetSearchKeywordsFromGUIContentProperties<Styles>());
        }
    }
}
