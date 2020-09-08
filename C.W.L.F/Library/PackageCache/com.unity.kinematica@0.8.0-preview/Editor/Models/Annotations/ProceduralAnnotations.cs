using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Kinematica.Editor.Experimental
{
    /// <summary>
    /// Annotations that can be procedurally created by user from animation clip (animation events in clip, automatic foot step generation...)
    /// </summary>
    public class ProceduralAnnotations
    {
        /// <summary>
        /// Create a tag that will later be added to a clip.
        /// </summary>
        /// <typeparam name="T">Type of the tag, must implement <code>Payload<T></code></typeparam>
        /// <param name="tagPayload">Tag instance</param>
        /// <param name="starTimeInSeconds">start of the tag in seconds (relative to the clip)</param>
        /// <param name="durationInSeconds">duration of the tag in seconds</param>
        public void CreateTag<T>(T tagPayload, float starTimeInSeconds, float durationInSeconds) where T : struct
        {
            TagAnnotation tag = TagAnnotation.Create<T>(tagPayload, starTimeInSeconds, durationInSeconds);
            m_Tags.Add(tag);
        }

        /// <summary>
        /// Create a marker that will later be added to a clip.
        /// </summary>
        /// <typeparam name="T">Type of the marker, must implement <code>Payload<T></code></typeparam>
        /// <param name="tagPayload">Marker instance</param>
        /// <param name="timeInSeconds">marker time in seconds (relative to the clip)</param>
        public void CreateMarker<T>(T markerPayload, float timeInSeconds) where T : struct
        {
            MarkerAnnotation marker = MarkerAnnotation.Create<T>(markerPayload, timeInSeconds);
            m_Markers.Add(marker);
        }

        internal static IProceduralAnnotationsGenerator[] CreateGenerators(bool bBuildTime)
        {
            List<IProceduralAnnotationsGenerator> generators = Utility.InstantiateAllTypesDerivingFrom<IProceduralAnnotationsGenerator>();
            return generators.Where(generator => generator.DoesGenerateAtBuildTime == bBuildTime).ToArray();
        }

        internal int NumAnnotations => m_Tags.Count + m_Markers.Count;

        internal IEnumerable<TagAnnotation> Tags => m_Tags;
        internal IEnumerable<MarkerAnnotation> Markers => m_Markers;

        List<TagAnnotation> m_Tags = new List<TagAnnotation>();
        List<MarkerAnnotation> m_Markers = new List<MarkerAnnotation>();
    }
}
