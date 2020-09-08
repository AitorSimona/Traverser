namespace Unity.Kinematica
{
    /// <summary>
    /// Interface for traits.
    /// </summary>
    /// <remarks>
    /// Traits are user-defined characteristics that can be associated to tags or markers.
    /// Users can define own custom data by using C# structs. These structs
    /// will then show up in the Kinematica builder tool and allow tags or markers
    /// to be created carrying specific instances of the corresponding traits.
    /// A trait itself wraps the actual payload (the instance of the user-defined struct).
    /// <para>
    /// Traits can optionally implement the trait interface which allows to execute
    /// code whenever a marker referring to the trait is "stepped over" during playback.
    /// </para>
    /// <example>
    /// <code>
    /// [Trait, BurstCompile]
    /// public struct Loop : Trait
    /// {
    ///     public void Execute(ref MotionSynthesizer synthesizer)
    ///     {
    ///         synthesizer.Push(synthesizer.Rewind(synthesizer.Time));
    ///     }
    ///
    ///     [BurstCompile]
    ///     public static void ExecuteSelf(ref Loop self, ref MotionSynthesizer synthesizer)
    ///     {
    ///         self.Execute(ref synthesizer);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Binary.Tag"/>
    /// <seealso cref="Binary.Marker"/>
    /// <seealso cref="Query"/>
    public interface Trait
    {
        /// <summary>
        /// Method that is to be executed if a marker
        /// that carries this trait is encountered during playback.
        /// </summary>
        void Execute(ref MotionSynthesizer synthesizer);
    }
}
