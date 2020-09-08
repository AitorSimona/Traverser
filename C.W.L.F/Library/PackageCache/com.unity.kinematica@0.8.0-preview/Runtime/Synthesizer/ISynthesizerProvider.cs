namespace Unity.Kinematica
{
    /// <summary>
    /// <para>
    /// Interface to provide a reference to a Kinematica motion synthesizer. This interface should be implemented
    /// by all MonoBehaviours that use Kinematica, so that Kinematica tools (task graph, debugger) can have access
    /// to the synthesizer associated to the component
    /// </para>
    /// <para>
    /// Implementing an interface is more flexible than inheriting a component with a strong reference to the motion
    /// synthesizer. It allows for example to not store directly the synthesizer inside the component, which can be
    /// useful if the synthesizer must be stored inside an optional playable graph the component have access to.
    /// </para>
    /// </summary>
    public interface IMotionSynthesizerProvider
    {
        /// <summary>
        /// Return a synthesizer, it can be an invalid memory reference, indicating the provider don't provide
        /// synthesizer for the moment.
        /// </summary>
        MemoryRef<MotionSynthesizer> Synthesizer { get; }

        bool IsSynthesizerInitialized { get; }
    }
}
