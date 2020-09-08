using System;

namespace Unity.Kinematica
{
    [Flags]
    public enum OutputFlags
    {
        None = 0,
        OnlyAcceptSelfNode = 1 << 0,
    };

    /// <summary>
    /// Attribute used to annotate task output fields.
    /// </summary>
    /// <remarks>
    /// Tasks can optionally have output properties that are
    /// written to during task execution. The output attribute
    /// is used for such properties.
    /// <example>
    /// <code>
    /// public struct CurrentPoseTask : Task
    /// {
    ///     [Output("Time Index")]
    ///     Identifier<SamplingTime> samplingTime;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Task"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class OutputAttribute : Attribute
    {
        internal string name;
        internal OutputFlags flags;

        /// <summary>
        /// Constructs an output attribute with the name passed as argument.
        /// </summary>
        /// <remarks>
        /// By default the property type name will be used for display purposes.
        /// Alternatively, an override name can be passed to the output attribute constructor.
        /// </remarks>
        /// <param name="name">Name that is to be used for the property.</param>
        public OutputAttribute(string name = null, OutputFlags flags = OutputFlags.None)
        {
            this.name = name;
            this.flags = flags;
        }

        public bool AcceptOnlySelfNode => (flags & OutputFlags.OnlyAcceptSelfNode) > 0;
    }
}
