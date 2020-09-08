using System;

namespace Unity.Kinematica
{
    [Flags]
    public enum InputFlags
    {
        None = 0,
        OnlyAcceptSelfNode = 1 << 0,
    };


    /// <summary>
    /// Attribute used to annotate task input fields.
    /// </summary>
    /// <remarks>
    /// Tasks can optionally have input properties that are
    /// read from during task execution. The input attribute
    /// is used for such properties.
    /// <example>
    /// <code>
    /// public struct MatchFragmentTask : Task
    /// {
    ///     [Input("Trajectory")]
    ///     Identifier<Trajectory> trajectory;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Task"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAttribute : Attribute
    {
        internal string name;
        internal InputFlags flags;

        /// <summary>
        /// Constructs an input attribute with the name passed as argument.
        /// </summary>
        /// <remarks>
        /// By default the property type name will be used for display purposes.
        /// Alternatively, an override name can be passed to the input attribute constructor.
        /// </remarks>
        /// <param name="name">Name that is to be used for the property.</param>
        public InputAttribute(string name = null, InputFlags flags = InputFlags.None)
        {
            this.name = name;
            this.flags = flags;
        }

        public bool AcceptOnlySelfNode => (flags & InputFlags.OnlyAcceptSelfNode) > 0;
    }
}
