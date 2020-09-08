using System;

namespace Unity.Kinematica
{
    /// <summary>
    /// Attribute used to annotate task properties.
    /// </summary>
    /// <remarks>
    /// Tasks can optionally have properties than be read from
    /// or written to by user code. In order to display these
    /// properties as editable fields in the corresponding
    /// task graph visualization the property attribute can
    /// be used.
    /// <example>
    /// <code>
    /// public struct TimerTask : Task
    /// {
    ///     [Property]
    ///     float timeInSeconds;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Task"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class PropertyAttribute : Attribute
    {
        internal string name;

        /// <summary>
        /// Constructs a property attribute with the name passed as argument.
        /// </summary>
        /// <remarks>
        /// By default the type name is used for display purposes. Alternatively,
        /// an override name can be passed to the property attribute constructor.
        /// </remarks>
        /// <param name="name">Name that is to be used for the property.</param>
        public PropertyAttribute(string name = null)
        {
            this.name = name;
        }
    }
}
