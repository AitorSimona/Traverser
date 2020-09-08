using Unity.Mathematics;
using Unity.Collections;

using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        /// <summary>
        /// A codebook contains the necessary information to perform fast comparisons
        /// for poses and trajectories. Poses and trajectories can be compared separately.
        /// </summary>
        /// <remarks>
        /// Codedbooks are defined based on metrics which in turn are defined on a per-tag basis.
        /// The reason for this is to logically group similar poses into a combined codebook.
        /// </remarks>
        public struct CodeBook
        {
            /// <summary>
            /// Every float from each fragment will be encoded in a code
            /// of <code>kNumCodeBits</code> bits. This code is interpreted as an index
            /// pointing to the closest centroid to the encoded float.
            /// </summary>
            public static readonly int kNumCodeBits = 8;

            /// <summary>
            /// Number of values a code (encoding a float from a fragment) can have.
            /// A code can have any value in <code>[0, kNumCodeValues - 1]</code>
            /// </summary>
            public static readonly int kNumCodeValues = 1 << kNumCodeBits;

            /// <summary>
            /// Denotes the metric this codebook has been created for.
            /// </summary>
            public MetricIndex metricIndex;

            /// <summary>
            /// Denotes the trait this codebook has been created for.
            /// </summary>
            public TraitIndex traitIndex;

            /// <summary>
            /// Denotes the number of fragments this codebook contains.
            /// </summary>
            public int numFragments;

            internal BlobArray<IntervalIndex> intervals;

            /// <summary>
            /// An encoding contains the necessary information to perform fast comparisons
            /// between fragments. Fragments represent either poses or trajectories and
            /// are automatically created for all animation frames the codebook covers.
            /// </summary>
            public struct Encoding
            {
                internal int numFragments;

                internal short numFeatures;

                internal short numFeaturesQuantized;

                internal short numFeaturesNormalized;

                internal short numFeaturesTransformed;

                internal int numFeaturesFlattened => numFeatures + numFeaturesQuantized;

                internal int numCodes => numFeaturesFlattened * numFragments;

                internal int transformedIndex => numFeatures - numFeaturesTransformed;

                internal int normalizedIndex => numFeaturesQuantized;

                internal BlobArray<byte> codes;

                internal BlobArray<float3> centroids;

                internal struct BoundingBox
                {
                    public AffineTransform transform;
                    public float3 extent;
                    public float inverseDiagonal;

                    public float3 transformPoint(float3 position)
                    {
                        return transform.transform(position);
                    }

                    public float3 inverseTransformPoint(float3 position)
                    {
                        return transform.inverseTransform(position);
                    }

                    public float3 normalize(float3 position)
                    {
                        return inverseTransformPoint(position) / extent;
                    }

                    public float3 inverseNormalize(float3 position)
                    {
                        return transformPoint(position * extent);
                    }
                }

                internal BlobArray<BoundingBox> boundingBoxes;

                internal struct Quantizer
                {
                    public float minimum;
                    public float range;

                    public float Decode(byte value)
                    {
                        return Decode(value / 255.0f);
                    }

                    public float Decode(float value)
                    {
                        return minimum + range * value;
                    }

                    public byte Quantize(float value)
                    {
                        return (byte)(Normalize(value) * 255.0f);
                    }

                    public float Normalize(float value)
                    {
                        return math.clamp((value - minimum) / range, 0.0f, 1.0f);
                    }
                }

                internal BlobArray<Quantizer> quantizers;

                internal unsafe MemoryArray<byte> GetCodes()
                {
                    Assert.IsTrue(codes.Length == numCodes);

                    return new MemoryArray<byte>
                    {
                        ptr = codes.GetUnsafePtr(),
                        length = codes.Length
                    };
                }

                internal unsafe MemoryArray<byte> GetCodes(int fragmentIndex)
                {
                    Assert.IsTrue(fragmentIndex < numFragments);
                    Assert.IsTrue(codes.Length == numCodes);

                    int offset = fragmentIndex * numFeaturesFlattened;

                    byte* ptr = (byte*)codes.GetUnsafePtr() + offset;

                    return new MemoryArray<byte>
                    {
                        ptr = ptr,
                        length = numFeaturesFlattened
                    };
                }

                internal unsafe MemoryArray<float3> GetCentroids()
                {
                    Assert.IsTrue(centroids.Length == numFeatures << 8);

                    return new MemoryArray<float3>
                    {
                        ptr = centroids.GetUnsafePtr(),
                        length = centroids.Length
                    };
                }

                internal unsafe MemoryArray<BoundingBox> GetBoundingBoxes()
                {
                    Assert.IsTrue(boundingBoxes.Length == numFeaturesTransformed);

                    return new MemoryArray<BoundingBox>
                    {
                        ptr = boundingBoxes.GetUnsafePtr(),
                        length = boundingBoxes.Length
                    };
                }

                internal void DecodeFeatures(NativeSlice<float3> destination, int fragmentIndex)
                {
                    Assert.IsTrue(destination.Length == this.numFeatures);

                    var codes = GetCodes(fragmentIndex);
                    var centroids = GetCentroids();

                    Assert.IsTrue(codes.Length == numFeaturesFlattened);

                    float codeMaxValue = Binary.CodeBook.kNumCodeValues - 1;

                    for (int i = 0; i < numFeaturesQuantized; ++i)
                    {
                        var normalizedLength = codes[i] / codeMaxValue;

                        var code = codes[numFeaturesQuantized + i];

                        var readIndex = (i << 8) + code;

                        var featureValue = centroids[readIndex];

                        destination[i] =
                            math.normalizesafe(featureValue) *
                            normalizedLength;
                    }

                    for (int i = numFeaturesQuantized; i < numFeatures; ++i)
                    {
                        var code = codes[numFeaturesQuantized + i];

                        var readIndex = (i << 8) + code;

                        var featureValue = centroids[readIndex];

                        destination[i] = featureValue;
                    }
                }

                /// <summary>
                /// Normalizes all features of an encoding.
                /// </summary>
                /// <remarks>
                /// Encodings contain fragments which represent either poses or trajectories.
                /// Fragments in turn store features that have been normalized subject to their
                /// respective domain (positions and velocities will be normalized differently).
                /// </remarks>
                /// <param name="destination">Destination array that will contain the normalized features.</param>
                /// <param name="features">Source array that the features are read from.</param>
                /// <seealso cref="Encoding"/>
                /// <seealso cref="InverseNormalize"/>
                public void Normalize(NativeSlice<float3> destination, NativeArray<float3> features)
                {
                    Assert.IsTrue(features.Length == numFeatures);
                    Assert.IsTrue(destination.Length == numFeatures);

                    for (int i = 0; i < numFeaturesQuantized; ++i)
                    {
                        var featureValue = features[i];

                        var length = math.length(featureValue);

                        var normalizedLength = quantizers[i].Normalize(length);

                        var defaultDirection = Missing.zero;

                        if (length < 0.02f)
                        {
                            featureValue = defaultDirection;
                        }
                        else
                        {
                            featureValue =
                                math.normalizesafe(
                                    featureValue, defaultDirection);
                        }

                        destination[i] = featureValue * normalizedLength;
                    }

                    var boundingBoxes = GetBoundingBoxes();

                    for (int i = 0; i < numFeaturesTransformed; ++i)
                    {
                        var index = transformedIndex + i;

                        var normalizedFeatureValue =
                            boundingBoxes[i].normalize(features[index]);

                        destination[index] = normalizedFeatureValue;
                    }
                }

                /// <summary>
                /// Normalizes all features of an encoding.
                /// </summary>
                /// <param name="fragment">Feature array that should be normalized.</param>
                public void Normalize(NativeArray<float3> fragment)
                {
                    Normalize(fragment, fragment);
                }

                /// <summary>
                /// Inverse normalizes all features of an encoding.
                /// </summary>
                /// <remarks>
                /// Encodings contain fragments which represent either poses or trajectories.
                /// Fragments in turn store features that have been normalized subject to their
                /// respective domain (positions and velocities will be normalized differently).
                /// </remarks>
                /// <param name="destination">Destination array that will contain the inverse normalized features.</param>
                /// <param name="features">Source array that the features are read from.</param>
                /// <seealso cref="Encoding"/>
                /// <seealso cref="Normalize"/>
                public void InverseNormalize(NativeSlice<float3> destination, NativeArray<float3> features)
                {
                    Assert.IsTrue(features.Length == numFeatures);
                    Assert.IsTrue(destination.Length == numFeatures);

                    for (int i = 0; i < numFeaturesQuantized; ++i)
                    {
                        var normalizedLength = math.length(features[i]);

                        var length = quantizers[i].Decode(normalizedLength);

                        destination[i] = math.normalizesafe(features[i]) * length;
                    }

                    var boundingBoxes = GetBoundingBoxes();

                    for (int i = 0; i < numFeaturesTransformed; ++i)
                    {
                        var index = transformedIndex + i;

                        var normalizedFeatureValue =
                            boundingBoxes[i].inverseNormalize(features[index]);

                        destination[index] = normalizedFeatureValue;
                    }
                }

                /// <summary>
                /// Inverse normalizes all features of an encoding.
                /// </summary>
                /// <param name="fragment">Feature array that should be inverse normalized.</param>
                public void InverseNormalize(NativeArray<float3> fragment)
                {
                    InverseNormalize(fragment, fragment);
                }

                /// <summary>
                /// Calculates the normalized deviation between normalized encodings.
                /// </summary>
                /// <remarks>
                /// Given a pair of normalized feature encodings this method calculates
                /// a normalized value denoting the corresponding similarity value.
                /// <para>
                /// The resulting value is always in the range between 0 and 1.
                /// A value of 0 indicates perfect similarity and a value of 1 indicates
                /// the maximum possible deviation subject to the training data.
                /// </para>
                /// </remarks>
                /// <param name="current">First element of the encoding pair.</param>
                /// <param name="candidate">Second element of the encoding pair.</param>
                /// <returns>The normalized similarity value.</returns>
                public float FeatureDeviation(NativeArray<float3> current, NativeArray<float3> candidate)
                {
                    Assert.IsTrue(current.Length == numFeatures);
                    Assert.IsTrue(candidate.Length == numFeatures);

                    float deviation = 0.0f;

                    for (int i = 0; i < numFeaturesQuantized; ++i)
                    {
                        var currentValue = current[i];
                        var candidateValue = candidate[i];

                        var currentMagnitude = math.length(currentValue);
                        var candidateMagnitude = math.length(candidateValue);

                        var magnitudeDelta =
                            math.abs(currentMagnitude - candidateMagnitude);

                        //Assert.IsTrue(magnitudeDelta >= 0.0f && magnitudeDelta <= 1.0f);

                        deviation += magnitudeDelta;

                        currentValue = math.normalizesafe(currentValue);
                        candidateValue = math.normalizesafe(candidateValue);

                        var directionDelta =
                            -math.dot(currentValue,
                                candidateValue) * 0.5f + 0.5f;

                        directionDelta = math.clamp(directionDelta, -0.01f, 1.01f);

                        deviation += directionDelta;
                    }

                    for (int i = 0; i < numFeaturesNormalized; ++i)
                    {
                        var index = normalizedIndex + i;

                        var currentValue = current[index];
                        var candidateValue = candidate[index];

                        var difference =
                            -math.dot(currentValue,
                                candidateValue) * 0.5f + 0.5f;

                        difference = math.clamp(difference, -0.01f, 1.01f);

                        deviation += difference;
                    }

                    var boundingBoxes = GetBoundingBoxes();

                    for (int i = 0; i < numFeaturesTransformed; ++i)
                    {
                        var index = transformedIndex + i;

                        var currentValue = current[index];
                        var candidateValue = candidate[index];

                        var difference = currentValue - candidateValue;

                        var normalizedFeatureCost =
                            math.length(difference) * boundingBoxes[i].inverseDiagonal;

                        //Assert.IsTrue(normalizedFeatureCost >= 0.0f && normalizedFeatureCost <= 1.0f);

                        deviation += normalizedFeatureCost;
                    }

                    deviation /= numFeaturesFlattened;

                    return deviation;
                }
            }

            /// <summary>
            /// Denotes the encoding for all poses contained in this codebook.
            /// </summary>
            public Encoding poses;

            /// <summary>
            /// Denotes the encoding for all trajectories contained in this codebook.
            /// </summary>
            public Encoding trajectories;

            internal bool Contains(IntervalIndex intervalIndex)
            {
                int numIntervals = intervals.Length;

                for (int i = 0; i < numIntervals; ++i)
                {
                    if (intervals[i] == intervalIndex)
                    {
                        return true;
                    }
                }

                return false;
            }

            internal int GetFragmentIndex(MemoryArray<Interval> intervals, IntervalIndex index)
            {
                int fragmentIndex = 0;

                int numIntervals = intervals.Length;

                for (int i = 0; i < numIntervals; ++i)
                {
                    var intervalIndex = this.intervals[i];

                    if (intervalIndex == index)
                    {
                        return fragmentIndex;
                    }

                    fragmentIndex += intervals[intervalIndex].numFrames;
                }

                return -1;
            }
        }

        /// <summary>
        /// Denotes an index into the list of codebooks contained in the runtime asset.
        /// </summary>
        public struct CodeBookIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two codebook indices are equal.
            /// </summary>
            /// <param name="codeBookIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(CodeBookIndex codeBookIndex)
            {
                return value == codeBookIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a codebook index to an integer value.
            /// </summary>
            public static implicit operator int(CodeBookIndex codeBookIndex)
            {
                return codeBookIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a codebook index.
            /// </summary>
            public static implicit operator CodeBookIndex(int codeBookIndex)
            {
                return Create(codeBookIndex);
            }

            internal static CodeBookIndex Create(int codeBookIndex)
            {
                return new CodeBookIndex
                {
                    value = codeBookIndex
                };
            }

            /// <summary>
            /// Invalid codebook index.
            /// </summary>
            public static CodeBookIndex Invalid => - 1;

            /// <summary>
            /// Determines if the given codebook index is valid or not.
            /// </summary>
            /// <returns>True if the codebook index is valid; false otherwise.</returns>
            public bool IsValid => value >= 0;
        }

        /// <summary>
        /// Returns the number of code books stored in the runtime asset.
        /// </summary>
        /// <seealso cref="CodeBook"/>
        public int numCodeBooks => codeBooks.Length;

        /// <summary>
        /// Retrieves a reference to a codebook stored in the runtime asset.
        /// </summary>
        /// <param name="index">The codebook index to retrieve the reference for.</param>
        /// <returns>Codebook reference that corresponds to the index passed as argument.</returns>
        public ref CodeBook GetCodeBook(CodeBookIndex index)
        {
            Assert.IsTrue(index < numCodeBooks);
            return ref codeBooks[index];
        }

        /// <summary>
        /// Retrieve fragment index inside codebook that is associated to a time index
        /// </summary>
        /// <returns>Returns -1 if the time index isn't covered by the codebook</returns>
        public int GetCodeBookFragmentIndex(ref CodeBook codeBook, TimeIndex timeIndex)
        {
            int numIntervals = codeBook.intervals.Length;

            int fragmentIndex = 0;

            for (int i = 0; i < numIntervals; ++i)
            {
                ref Interval interval = ref GetInterval(codeBook.intervals[i]);
                if (interval.Contains(timeIndex))
                {
                    fragmentIndex += timeIndex.frameIndex - interval.firstFrame;
                    return fragmentIndex;
                }

                fragmentIndex += interval.numFrames;
            }

            return -1;
        }
    }
}
