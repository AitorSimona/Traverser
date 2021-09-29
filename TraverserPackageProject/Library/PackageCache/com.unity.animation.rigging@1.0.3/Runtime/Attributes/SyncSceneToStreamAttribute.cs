using System;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The [SyncSceneToStream] attribute can be used to ensure constraints properties are read from the scene
    /// and written back in the AnimationStream if they were not previously animated.
    /// Supported value types are: Float, Int, Bool, Vector2, Vector3, Vector4, Quaternion, Vector3Int, Vector3Bool,
    /// Transform, Transform[], WeightedTransform and WeightedTransformArray.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class SyncSceneToStreamAttribute : Attribute { }

    internal enum PropertyType : byte { Bool, Int, Float };

    internal struct PropertyDescriptor
    {
        public int size;
        public PropertyType type;
    }

    internal struct Property
    {
        public string name;
        public PropertyDescriptor descriptor;
    }

    internal struct RigProperties
    {
        public static string s_Weight = "m_Weight";
        public Component component;
    }

    internal struct ConstraintProperties
    {
        public static string s_Weight = "m_Weight";
        public Component component;
        public Property[] properties;
    }

    internal static class PropertyUtils
    {
        public static string ConstructConstraintDataPropertyName(string property)
        {
            return "m_Data." + property;
        }

        public static string ConstructCustomPropertyName(Component component, string property)
        {
            return component.transform.GetInstanceID() + "/" + component.GetType() + "/" + property;
        }
    }
}
