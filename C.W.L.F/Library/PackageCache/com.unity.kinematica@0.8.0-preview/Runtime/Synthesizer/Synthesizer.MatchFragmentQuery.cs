using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine.Assertions;

using BoundingBox = Unity.Kinematica.Binary.CodeBook.Encoding.BoundingBox;
using Quantizer = Unity.Kinematica.Binary.CodeBook.Encoding.Quantizer;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        internal struct MatchFragmentQuery
        {
            NativeArray<float3> poseFeatures;
            NativeArray<float3> trajectoryFeatures;

            NativeArray<float> poseDistances;
            NativeArray<float> trajectoryDistances;

            public struct Header
            {
                public int numFeatures;
                public int numFeaturesFlattened;

                public int featureIndex;
                public int distanceIndex;

                public static Header Create(int numFeatures, int numFeaturesFlattened, int featureIndex, int distanceIndex)
                {
                    return new Header
                    {
                        numFeatures = numFeatures,
                        numFeaturesFlattened = numFeaturesFlattened,

                        featureIndex = featureIndex,
                        distanceIndex = distanceIndex
                    };
                }
            };

            NativeArray<Header> poseHeaders;
            NativeArray<Header> trajectoryHeaders;

            MatchFragmentQuery(ref Binary binary)
            {
                int numCodeBooks = binary.numCodeBooks;

                poseHeaders = new NativeArray<Header>(numCodeBooks, Allocator.Temp);
                trajectoryHeaders = new NativeArray<Header>(numCodeBooks, Allocator.Temp);

                int poseFeatureIndex = 0;
                int poseDistanceIndex = 0;
                int trajectoryFeatureIndex = 0;
                int trajectoryDistanceIndex = 0;

                int numCodeValues = Binary.CodeBook.kNumCodeValues;

                for (int i = 0; i < numCodeBooks; ++i)
                {
                    ref var codeBook = ref binary.GetCodeBook(i);

                    int numPoseFeatures =
                        codeBook.poses.numFeatures;

                    int numPoseFeaturesFlattened =
                        codeBook.poses.numFeaturesFlattened;

                    int numTrajectoryFeatures =
                        codeBook.trajectories.numFeatures;

                    int numTrajectoryFeaturesFlattened =
                        codeBook.trajectories.numFeaturesFlattened;

                    poseHeaders[i] =
                        Header.Create(numPoseFeatures, numPoseFeaturesFlattened,
                            poseFeatureIndex, poseDistanceIndex);

                    trajectoryHeaders[i] =
                        Header.Create(numTrajectoryFeatures, numTrajectoryFeaturesFlattened,
                            trajectoryFeatureIndex, trajectoryDistanceIndex);

                    poseFeatureIndex += numPoseFeatures;
                    poseDistanceIndex += numPoseFeaturesFlattened;

                    trajectoryFeatureIndex += numTrajectoryFeatures;
                    trajectoryDistanceIndex += numTrajectoryFeaturesFlattened;
                }

                poseFeatures = new NativeArray<float3>(poseFeatureIndex, Allocator.Temp);
                trajectoryFeatures = new NativeArray<float3>(trajectoryFeatureIndex, Allocator.Temp);

                poseDistances = new NativeArray<float>(poseDistanceIndex * numCodeValues, Allocator.Temp);
                trajectoryDistances = new NativeArray<float>(trajectoryDistanceIndex * numCodeValues, Allocator.Temp);
            }

            unsafe bool FastDecodePose(ref Binary binary, int codeBookIndex, TimeIndex timeIndex)
            {
                Assert.IsTrue(codeBookIndex < binary.numCodeBooks);

                ref var codeBook =
                    ref binary.GetCodeBook(codeBookIndex);

                int numPoseFeatures =
                    codeBook.poses.numFeatures;

                int numPoseFeaturesFlattened =
                    codeBook.poses.numFeaturesFlattened;

                var poseHeader = poseHeaders[codeBookIndex];

                Assert.IsTrue(numPoseFeatures == poseHeader.numFeatures);
                Assert.IsTrue(numPoseFeaturesFlattened == poseHeader.numFeaturesFlattened);

                int numIntervals = codeBook.intervals.Length;

                int fragmentIndex = 0;

                for (int i = 0; i < numIntervals; ++i)
                {
                    var intervalIndex = codeBook.intervals[i];

                    ref var interval = ref binary.GetInterval(intervalIndex);

                    if (interval.Contains(timeIndex))
                    {
                        var relativeIndex =
                            timeIndex.frameIndex - interval.firstFrame;

                        var index = fragmentIndex + relativeIndex;

                        var poseDestination = new NativeSlice<float3>(
                            poseFeatures, poseHeader.featureIndex, numPoseFeatures);

                        codeBook.poses.DecodeFeatures(poseDestination, index);

                        return true;
                    }

                    fragmentIndex += interval.numFrames;
                }

                return false;
            }

            public unsafe void GeneratePoseDistanceTable(ref Binary binary, int codeBookIndex, float weightFactor = 1.0f)
            {
                ref var codeBook =
                    ref binary.GetCodeBook(codeBookIndex);

                var numFeaturesQuantized =
                    codeBook.poses.numFeaturesQuantized;

                var numFeatures =
                    codeBook.poses.numFeatures;

                int numFeaturesFlattened =
                    codeBook.poses.numFeaturesFlattened;

                int numCodeValues = Binary.CodeBook.kNumCodeValues;
                float maxCodeValue = numCodeValues - 1;

                var header = poseHeaders[codeBookIndex];

                Assert.IsTrue(numFeatures == header.numFeatures);
                Assert.IsTrue(numFeaturesFlattened == header.numFeaturesFlattened);

                var centroids = (float3*)codeBook.poses.centroids.GetUnsafePtr();
                var boundingBoxes = (BoundingBox*)codeBook.poses.boundingBoxes.GetUnsafePtr();
                var quantizers = (Quantizer*)codeBook.poses.quantizers.GetUnsafePtr();

                var features = (float3*)NativeArrayUnsafeUtility.GetUnsafePtr(poseFeatures);
                var distances = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(poseDistances);

                features += header.featureIndex;
                distances += header.distanceIndex;

                weightFactor *= math.rcp(numFeaturesFlattened);

                var writeIndex = 0;

                for (int i = 0; i < numFeaturesQuantized; ++i)
                {
                    var featureValue = features[i];

                    var magnitude = math.length(featureValue);

                    Assert.IsTrue(magnitude >= 0.0f && magnitude <= 1.0f);

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var reference = j / maxCodeValue;

                        var difference =
                            math.abs(magnitude - reference);

                        Assert.IsTrue(difference >= 0.0f && difference <= 1.0f);

                        distances[writeIndex++] = difference * 2.0f * weightFactor;
                    }
                }

                for (int i = 0; i < numFeaturesQuantized; ++i)
                {
                    var featureValue = features[i];

                    var normalizedFeatureValue =
                        math.normalizesafe(features[i], Missing.zero);

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(i << 8) + j];

                        var difference =
                            -math.dot(centroid,
                                normalizedFeatureValue) * 0.5f + 0.5f;

                        distances[writeIndex++] = difference * weightFactor;
                    }
                }

                var numFeaturesNormalized =
                    codeBook.poses.numFeaturesNormalized;

                for (int i = 0; i < numFeaturesNormalized; ++i)
                {
                    var index = codeBook.poses.normalizedIndex + i;

                    var featureValue = features[index];

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(index << 8) + j];

                        var difference =
                            -math.dot(centroid,
                                featureValue) * 0.5f + 0.5f;

                        distances[writeIndex++] = difference * weightFactor;
                    }
                }

                var numFeaturesTransformed =
                    codeBook.poses.numFeaturesTransformed;

                for (int i = 0; i < numFeaturesTransformed; ++i)
                {
                    var index = codeBook.poses.transformedIndex + i;

                    var featureValue = features[index];

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(index << 8) + j];

                        var difference = centroid - featureValue;

                        var normalizedFeatureCost =
                            math.length(difference) * boundingBoxes[i].inverseDiagonal;

                        distances[writeIndex++] = normalizedFeatureCost * weightFactor;
                    }
                }

                Assert.IsTrue(writeIndex == numFeaturesFlattened * numCodeValues);
            }

            public unsafe void DecodePose(ref Binary binary, int codeBookIndex, TimeIndex timeIndex)
            {
                Assert.IsTrue(timeIndex.IsValid);

                if (!FastDecodePose(ref binary, codeBookIndex, timeIndex))
                {
                    int numCodeBooks = binary.numCodeBooks;

                    Assert.IsTrue(codeBookIndex < numCodeBooks);

                    ref var codeBook =
                        ref binary.GetCodeBook(codeBookIndex);

                    var metricIndex = codeBook.metricIndex;

                    var samplingTime = SamplingTime.Create(timeIndex);

                    var poseFragment =
                        binary.CreatePoseFragment(
                            metricIndex, samplingTime);

                    int numPoseFeatures = poseFragment.array.Length;

                    Assert.IsTrue(numPoseFeatures == codeBook.poses.numFeatures);

                    var poseHeader = poseHeaders[codeBookIndex];

                    var poseDestination = new NativeSlice<float3>(
                        poseFeatures, poseHeader.featureIndex,
                        numPoseFeatures);

                    codeBook.poses.Normalize(
                        poseDestination, poseFragment.array);

                    poseFragment.Dispose();
                }
            }

            public unsafe void GenerateTrajectoryDistanceTable(ref Binary binary, int codeBookIndex, float weightFactor = 1.0f)
            {
                ref var codeBook =
                    ref binary.GetCodeBook(codeBookIndex);

                var numFeaturesQuantized =
                    codeBook.trajectories.numFeaturesQuantized;

                var numFeatures =
                    codeBook.trajectories.numFeatures;

                int numFeaturesFlattened =
                    codeBook.trajectories.numFeaturesFlattened;

                int numCodeValues = Binary.CodeBook.kNumCodeValues;
                float maxCodeValue = numCodeValues - 1;

                var header = trajectoryHeaders[codeBookIndex];

                Assert.IsTrue(numFeatures == header.numFeatures);
                Assert.IsTrue(numFeaturesFlattened == header.numFeaturesFlattened);

                var centroids = (float3*)codeBook.trajectories.centroids.GetUnsafePtr();
                var boundingBoxes = (BoundingBox*)codeBook.trajectories.boundingBoxes.GetUnsafePtr();
                var quantizers = (Quantizer*)codeBook.trajectories.quantizers.GetUnsafePtr();

                var features = (float3*)NativeArrayUnsafeUtility.GetUnsafePtr(trajectoryFeatures);
                var distances = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(trajectoryDistances);

                features += header.featureIndex;
                distances += header.distanceIndex;

                weightFactor *= math.rcp(numFeaturesFlattened);

                var writeIndex = 0;

                for (int i = 0; i < numFeaturesQuantized; ++i)
                {
                    var featureValue = features[i];

                    var magnitude = math.length(featureValue);

                    magnitude = math.clamp(magnitude, 0.0f, 1.0f);

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var reference = j / maxCodeValue;

                        var difference =
                            math.abs(magnitude - reference);

                        Assert.IsTrue(difference >= 0.0f && difference <= 1.0f);

                        distances[writeIndex++] = difference * 2.0f * weightFactor;
                    }
                }

                for (int i = 0; i < numFeaturesQuantized; ++i)
                {
                    var featureValue = features[i];

                    var normalizedFeatureValue =
                        math.normalizesafe(features[i], Missing.zero);

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(i << 8) + j];

                        var difference =
                            -math.dot(centroid,
                                normalizedFeatureValue) * 0.5f + 0.5f;

                        distances[writeIndex++] = difference * weightFactor;
                    }
                }

                var numFeaturesNormalized =
                    codeBook.trajectories.numFeaturesNormalized;

                for (int i = 0; i < numFeaturesNormalized; ++i)
                {
                    var index = codeBook.trajectories.normalizedIndex + i;

                    var featureValue = features[index];

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(index << 8) + j];

                        var difference =
                            -math.dot(centroid,
                                featureValue) * 0.5f + 0.5f;

                        distances[writeIndex++] = difference * weightFactor;
                    }
                }

                var numFeaturesTransformed =
                    codeBook.trajectories.numFeaturesTransformed;

                for (int i = 0; i < numFeaturesTransformed; ++i)
                {
                    var index = codeBook.trajectories.transformedIndex + i;

                    var featureValue = features[index];

                    for (int j = 0; j < numCodeValues; ++j)
                    {
                        var centroid = centroids[(index << 8) + j];

                        var difference = centroid - featureValue;

                        var normalizedFeatureCost =
                            math.length(difference) * boundingBoxes[i].inverseDiagonal;

                        distances[writeIndex++] = normalizedFeatureCost * weightFactor;
                    }
                }

                Assert.IsTrue(writeIndex == numFeaturesFlattened * numCodeValues);
            }

            public void DecodeTrajectory(ref Binary binary, int codeBookIndex, Trajectory trajectory)
            {
                int numCodeBooks = binary.numCodeBooks;

                Assert.IsTrue(codeBookIndex < numCodeBooks);

                ref var codeBook =
                    ref binary.GetCodeBook(codeBookIndex);

                var metricIndex = codeBook.metricIndex;

                var trajectoryFragment =
                    binary.CreateTrajectoryFragment(
                        metricIndex, trajectory);

                var header = trajectoryHeaders[codeBookIndex];

                Assert.IsTrue(trajectoryFragment.array.Length == header.numFeatures);

                var trajectoryDestination =
                    new NativeSlice<float3>(trajectoryFeatures,
                        header.featureIndex, header.numFeatures);

                codeBook.trajectories.Normalize(
                    trajectoryDestination, trajectoryFragment.array);

                trajectoryFragment.Dispose();
            }

            public static MatchFragmentQuery Create(ref Binary binary)
            {
                return new MatchFragmentQuery(ref binary);
            }

            public NativeSlice<float3> GetPoseSlice(int index)
            {
                Assert.IsTrue(index < poseHeaders.Length);

                var featureIndex = poseHeaders[index].featureIndex;
                var numFeatures = poseHeaders[index].numFeatures;

                return new NativeSlice<float3>(
                    poseFeatures, featureIndex, numFeatures);
            }

            public NativeSlice<float3> GetTrajectorySlice(int index)
            {
                Assert.IsTrue(index < trajectoryHeaders.Length);

                var featureIndex = trajectoryHeaders[index].featureIndex;
                var numFeatures = trajectoryHeaders[index].numFeatures;

                return new NativeSlice<float3>(
                    trajectoryFeatures, featureIndex, numFeatures);
            }

            public unsafe float* GetPoseDistances(int index)
            {
                Assert.IsTrue(index < poseHeaders.Length);

                var featureIndex = poseHeaders[index].featureIndex << 8;

                return (float*)poseDistances.GetUnsafePtr() + featureIndex;
            }

            public NativeSlice<float> GetPoseDistanceSlice(int index)
            {
                Assert.IsTrue(index < poseHeaders.Length);

                var featureIndex = poseHeaders[index].featureIndex;
                var numFeatures = poseHeaders[index].numFeatures;

                return new NativeSlice<float>(
                    poseDistances, featureIndex << 8, numFeatures << 8);
            }

            public unsafe float* GetTrajectoryDistances(int index)
            {
                Assert.IsTrue(index < trajectoryHeaders.Length);

                var featureIndex = trajectoryHeaders[index].featureIndex << 8;

                return (float*)trajectoryDistances.GetUnsafePtr() + featureIndex;
            }

            public NativeSlice<float> GetTrajectoryDistanceSlice(int index)
            {
                Assert.IsTrue(index < trajectoryHeaders.Length);

                var featureIndex = trajectoryHeaders[index].featureIndex;
                var numFeatures = trajectoryHeaders[index].numFeatures;

                return new NativeSlice<float>(
                    trajectoryDistances, featureIndex << 8, numFeatures << 8);
            }

            public void Dispose()
            {
                poseFeatures.Dispose();
                trajectoryFeatures.Dispose();
                poseHeaders.Dispose();
                trajectoryHeaders.Dispose();
            }
        }
    }
}
