using UnityEngine;

namespace Unity.Kinematica.Editor.Experimental
{
    /// <summary>
    /// Interface to generate annotations (markers and tags) from animation clip (animation events in clip, automatic foot step generation...)
    /// </summary>
    public interface IProceduralAnnotationsGenerator
    {
        /// <summary>
        /// Indicate if the generation must occur at build time or not.
        /// If false, the generator is available as a right-click option in the Builder window and the generated annotations will be visible
        /// in builder timeline.
        /// If true, generation will happen at build time and generated annotations will be kept hidden in the builder.
        /// </summary>
        /// <returns></returns>
        bool DoesGenerateAtBuildTime { get; }

        /// <summary>
        /// Generate annotations from <paramref name="clip"/> and add them to <paramref name="annotations"/>
        /// </summary>
        void GenerateAnnotations(AnimationClip clip, ProceduralAnnotations annotations);
    }
}
