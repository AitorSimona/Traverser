using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Kinematica.Editor")]

namespace Unity.Kinematica
{
    /// <summary>
    /// Memory-ready runtime asset that contains the animation library
    /// and supplementary information required by the Kinematica runtime.
    /// </summary>
    /// <remarks>
    /// Kinematica strictly distinguishes between the editor and runtime asset.
    /// The asset builder in the Unity editor generates the runtime asset from
    /// from the editor asset. Conceptionally both representations are identical.
    /// The runtime asset contains additional acceleration structures.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Binary
    {
        // increase this number every time the structure of Binary changes
        internal static int s_CodeVersion = 4;

        internal int FileVersion
        {
            get { return fileVersion; }
            set { fileVersion = value; }
        }

        /// <summary>
        /// Time horizon in seconds.
        /// </summary>
        /// <remarks>
        /// The time horizon is a glocal setting that is used for trajectory matching.
        /// </remarks>
        /// <seealso cref="TrajectoryFragment"/>
        public float TimeHorizon
        {
            get { return timeHorizon; }
            internal set { timeHorizon = value; }
        }

        internal static string GetBinaryFilePathFromAssetGuid(string assetGuid)
        {
            // store binary files in streaming assets so that they're not stripped from the standalone player
            return Application.streamingAssetsPath + "/KinematicaBinary_" + assetGuid + ".bin";
        }

        //
        // Attributes
        //

        // /!\ Must be the first variable of the binary /!\
        private int fileVersion;

        private float timeHorizon;
        private float sampleRate;

        internal AnimationRig animationRig;

        internal BlobArray<Trait> traits;
        internal BlobArray<byte> payloads;

        internal BlobArray<Segment> segments;
        internal BlobArray<Tag> tags;
        internal BlobArray<Marker> markers;

        internal BlobArray<TagIndex> tagIndices;
        internal BlobArray<TagList> tagLists;
        internal BlobArray<Metric> metrics;

        internal BlobArray<Field> fields;
        internal BlobArray<Type> types;
        internal BlobArray<Interval> intervals;
        internal BlobArray<CodeBook> codeBooks;

        internal BlobArray<byte> stringBuffer;
        internal BlobArray<String> stringTable;

        internal BlobArray<AffineTransform> transforms;
    }
}
