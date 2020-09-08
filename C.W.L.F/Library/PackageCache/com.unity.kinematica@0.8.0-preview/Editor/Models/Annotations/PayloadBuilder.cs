using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    /// <summary>
    /// Utility allowing to sample joint transforms from the binary in order to
    /// build payload data out of them and store it inside Kinematica binary (Tags, Marker...).
    /// This can be used in order to store a joint position for a contact inside a Marker for example
    /// </summary>
    public interface PayloadBuilder
    {
        int GetJointIndexForName(string jointName);

        /// <summary>
        /// Returns the root transform of the character for the first frame of <code>DestinationInterval</code>
        /// </summary>
        /// <returns></returns>
        AffineTransform GetRootTransform();

        /// <summary>
        /// Returns the joint transform of the character at <paramref name="jointIndex"/> for the first frame of <code>DestinationInterval</code>
        /// </summary>
        /// <returns></returns>
        AffineTransform GetJointTransformCharacterSpace(int jointIndex);

        /// <summary>
        /// Returns the root transform of the character for the given frame
        /// </summary>
        /// <param name="frameIndex">Frame index in Kinematica binary motion library</param>
        /// <returns></returns>
        AffineTransform GetRootTransform(int frameIndex);

        /// <summary>
        /// Returns the joint transform of the character at <paramref name="jointIndex"/> for the given frame
        /// </summary>
        /// <param name="frameIndex">Frame index in Kinematica binary motion library</param>
        /// <returns></returns>
        AffineTransform GetJointTransformCharacterSpace(int frameIndex, int jointIndex);

        /// <summary>
        /// Destination interval of the poses, in Kinematica binary motion library, associated to the Payload currently being built
        /// (Tag or Marker for example). For a Marker, the number of frames of the interval is one.
        /// </summary>
        Interval DestinationInterval { get; }

        /// <summary>
        /// Source to target scale for retargeting
        /// </summary>
        float SourceToTargetScale { get; }
    }
}
