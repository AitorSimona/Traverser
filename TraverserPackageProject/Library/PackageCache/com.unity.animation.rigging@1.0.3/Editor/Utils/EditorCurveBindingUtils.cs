using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// Utility class that provides an easy way of retrieving EditorCurveBindings for common data types.
    /// </summary>
    public static class EditorCurveBindingUtils
    {
        /// <summary>
        /// Collects EditorCurveBindings for a Vector3 on a MonoBehavior.
        /// </summary>
        /// <typeparam name="T">The Type of the MonoBehavior the Vector3 is found on.</typeparam>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Vector3.</param>
        /// <param name="component">The MonoBehavior on which the Vector3 is found.</param>
        /// <param name="propertyName">Name of the Vector3 variable we are constructing a binding for.</param>
        /// <param name="bindings">List to which the bindings for the Vector3 will be appended.</param>
        public static void CollectVector3Bindings<T>(Transform root, T component, string propertyName, List<EditorCurveBinding> bindings)
            where T : MonoBehaviour
        {
            if (root == null || component == null || propertyName == "" || bindings == null)
                throw new ArgumentNullException("Arguments cannot be null.");

            var path = AnimationUtility.CalculateTransformPath(component.transform, root);

            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(T), propertyName + ".x"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(T), propertyName + ".y"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(T), propertyName + ".z"));
        }

        /// <summary>
        /// Collects translation, rotation and scale bindings for a Transform component.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Transform.</param>
        /// <param name="transform">The transform whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectTRSBindings(Transform root, Transform transform, List<EditorCurveBinding> bindings)
        {
            CollectPositionBindings(root, transform, bindings);
            CollectRotationBindings(root, transform, bindings);
            CollectScaleBindings(root, transform, bindings);
        }

        /// <summary>
        /// Collects translation, rotation bindings for a Transform component.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Transform.</param>
        /// <param name="transform">The transform whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectTRBindings(Transform root, Transform transform, List<EditorCurveBinding> bindings)
        {
            CollectPositionBindings(root, transform, bindings);
            CollectRotationBindings(root, transform, bindings);
        }

        /// <summary>
        /// Collects translation bindings for a Transform component.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Transform.</param>
        /// <param name="transform">The transform whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectPositionBindings(Transform root, Transform transform, List<EditorCurveBinding> bindings)
        {
            if (root == null || transform == null || bindings == null)
                throw new ArgumentNullException("Arguments cannot be null.");

            var path = AnimationUtility.CalculateTransformPath(transform, root);

            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"));
        }

        /// <summary>
        /// Collects rotation bindings for a Transform component.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Transform.</param>
        /// <param name="transform">The transform whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectRotationBindings(Transform root, Transform transform, List<EditorCurveBinding> bindings)
        {
            if (root == null || transform == null || bindings == null)
                throw new ArgumentNullException("Arguments cannot be null.");

            var path = AnimationUtility.CalculateTransformPath(transform, root);

            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"));
        }

        /// <summary>
        /// Collects scale bindings for a Transform component.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the Transform.</param>
        /// <param name="transform">The transform whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectScaleBindings(Transform root, Transform transform, List<EditorCurveBinding> bindings)
        {
            if (root == null || transform == null || bindings == null)
                throw new ArgumentNullException("Arguments cannot be null.");

            var path = AnimationUtility.CalculateTransformPath(transform, root);

            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"));
            bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"));
        }

        /// <summary>
        /// Collects the binding for a single float property on a MonoBehavior.
        /// </summary>
        /// <param name="root">The root to which the bindings are relative. Generally the root has the Animator which animates the float property.</param>
        /// <param name="component">The component on which the property is found.</param>
        /// <param name="propertyName">The name of the float property whose bindings are collected.</param>
        /// <param name="bindings">List to which the bindings for the Transform will be appended.</param>
        public static void CollectPropertyBindings(Transform root, MonoBehaviour component, string propertyName, List<EditorCurveBinding> bindings)
        {
            if (root == null || component == null || bindings == null)
                throw new ArgumentNullException("Arguments cannot be null.");

            var path = AnimationUtility.CalculateTransformPath(component.transform, root);

            bindings.Add(EditorCurveBinding.FloatCurve(path, component.GetType(), propertyName));
        }

        internal static bool RemapRotationBinding(AnimationClip clip, EditorCurveBinding binding, ref EditorCurveBinding rotationBinding)
        {
            if (!binding.propertyName.StartsWith("localEulerAngles"))
                return false;

            string suffix = binding.propertyName.Split('.')[1];

            rotationBinding = binding;

            // Euler Angles
            rotationBinding.propertyName = "localEulerAnglesRaw." + suffix;
            if (AnimationUtility.GetEditorCurve(clip, rotationBinding) != null)
                return true;

            // Euler Angles (Quaternion) interpolation
            rotationBinding.propertyName = "localEulerAnglesBaked." + suffix;
            if (AnimationUtility.GetEditorCurve(clip, rotationBinding) != null)
                return true;

            // Quaternion interpolation
            rotationBinding.propertyName = "localEulerAngles." + suffix;
            if (AnimationUtility.GetEditorCurve(clip, rotationBinding) != null)
                return true;

            return false;
        }
    }
}
