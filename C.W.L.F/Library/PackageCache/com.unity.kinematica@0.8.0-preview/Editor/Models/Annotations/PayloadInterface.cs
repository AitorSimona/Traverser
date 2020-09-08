namespace Unity.Kinematica.Editor
{
    /// <summary>
    /// Interface to implement in order to build instances of type <typeparamref name="T"/>
    /// and store them inside Kinematica binary (for example Tags, Markers...)
    /// </summary>
    /// <typeparam name="T">Must be blittable</typeparam>
    public interface Payload<T>
    {
        /// <summary>
        /// Build instance of T
        /// </summary>
        /// <param name="builder">Utility allowing to sample joint transforms from the binary</param>
        /// <returns></returns>
        T Build(PayloadBuilder builder);
    }
}
