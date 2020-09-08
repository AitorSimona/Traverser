using System;
using UnityEngine;
using ColorUtility = Unity.SnapshotDebugger.ColorUtility;

namespace Unity.Kinematica
{
    [Flags]
    public enum DataFlags
    {
        None = 0,
        DataOnly = 1 << 0,
        SelfInputOutput = 1 << 1,
    }

    /// <summary>
    /// Attribute used to annotate data types that are
    /// used as elements in the execution graph
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code>
    /// [Data("Trajectory", "#2A3756")]
    /// public struct Trajectory
    /// {
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="DataType"/>
    [AttributeUsage(AttributeTargets.Struct)]
    public class DataAttribute : Attribute
    {
        Color color;
        string displayName;
        DataFlags flags;


        /// <summary>
        /// Construct a new data attribute with the name and flags passed as argument.
        /// </summary>
        /// <param name="displayName">Display name to be used for the data type.</param>
        /// <param name="flag">Flags that control the behavior of this data type.</param>
        public DataAttribute(string displayName, DataFlags flags = DataFlags.None)
        {
            this.displayName = displayName;
            this.flags = flags;
        }

        /// <summary>
        /// Constructs a new data attribute with the name and flags passed as argument.
        /// </summary>
        /// <param name="displayName">Display name to be used for the data type.</param>
        /// <param name="color">Color to be used for display purposes of this data type.</param>
        /// <param name="flag">Flags that control the behavior of this data type.</param>
        public DataAttribute(string displayName, string color, DataFlags flags = DataFlags.None)
        {
            this.color = ColorUtility.FromHtmlString(color);
            this.displayName = displayName;
            this.flags = flags;
        }

        internal static string GetDescription(Type type)
        {
            var attribute = GetAttribute(type);

            if (attribute == null)
            {
                return type.Name;
            }

            return attribute.displayName;
        }

        internal static Color GetColor(Type type)
        {
            var attribute = GetAttribute(type);

            if (attribute == null)
            {
                return Color.gray;
            }

            return attribute.color;
        }

        internal static bool IsInputOutput(Type type)
        {
            var attribute = GetAttribute(type);

            if (attribute == null)
            {
                return false;
            }

            return (attribute.flags & DataFlags.SelfInputOutput) > 0;
        }

        internal static bool IsGraphNode(Type type)
        {
            var attribute = GetAttribute(type);

            if (attribute == null)
            {
                return false;
            }

            return (attribute.flags & DataFlags.DataOnly) == 0;
        }

        static DataAttribute GetAttribute(Type type)
        {
            var attributes =
                type.GetCustomAttributes(
                    typeof(DataAttribute), false);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes[0] as DataAttribute;
        }
    }
}
