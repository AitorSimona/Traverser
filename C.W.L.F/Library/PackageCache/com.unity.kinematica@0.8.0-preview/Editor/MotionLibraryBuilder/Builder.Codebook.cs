using System;
using System.Collections.Generic;

using UnityEditor;

using Unity.Mathematics;
using Unity.Collections;

using UnityEngine.Assertions;

using PoseFragment = Unity.Kinematica.Binary.PoseFragment;
using TrajectoryFragment = Unity.Kinematica.Binary.TrajectoryFragment;
using MetricIndex = Unity.Kinematica.Binary.MetricIndex;
using Unity.Jobs;
using System.Collections;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public interface Fragment : IDisposable
        {
            NativeArray<float3> Features();

            float3 Feature(int index);
        }

        struct PoseFragmentWrapper : Fragment
        {
            public NativeArray<float3> Features() => fragment.array;

            public float3 Feature(int index) => fragment[index];

            public void Dispose()
            {
                fragment.Dispose();
            }

            public static Fragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
            {
                var fragment = PoseFragment.Create(ref binary, metricIndex, samplingTime);

                return new PoseFragmentWrapper
                {
                    fragment = fragment
                };
            }

            PoseFragment fragment;
        }

        struct TrajectoryFragmentWrapper : Fragment
        {
            public NativeArray<float3> Features() => fragment.array;

            public float3 Feature(int index) => fragment[index];

            public void Dispose()
            {
                fragment.Dispose();
            }

            public static Fragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
            {
                var fragment = TrajectoryFragment.Create(ref binary, metricIndex, samplingTime);

                return new TrajectoryFragmentWrapper
                {
                    fragment = fragment
                };
            }

            TrajectoryFragment fragment;
        }

        internal struct FragmentArray : IDisposable
        {
            public int metricIndex;
            public int numFragments;
            public int numFeatures;
            public NativeArray<SamplingTime> samplingTimes;
            public NativeArray<float3> features; // flat array of fragments features

            public static FragmentArray Create(int metricIndex, int numFragments, int numFeatures)
            {
                return new FragmentArray()
                {
                    metricIndex = metricIndex,
                    numFragments = numFragments,
                    numFeatures = numFeatures,
                    samplingTimes = new NativeArray<SamplingTime>(numFragments, Allocator.Persistent),
                    features = new NativeArray<float3>(numFragments * numFeatures, Allocator.Persistent)
                };
            }

            public MemoryArray<float3> Features => new MemoryArray<float3>(features);

            public MemoryArray<float3> FragmentFeatures(int fragmentIndex) => new MemoryArray<float3>(features, fragmentIndex * numFeatures, numFeatures);

            public float3 Feature(int fragmentIndex, int featureIndex) => features[fragmentIndex * numFeatures + featureIndex];

            public void Dispose()
            {
                samplingTimes.Dispose();
                features.Dispose();
            }
        }

        public interface ICreateFragmentsJob
        {
            JobHandle Schedule();
        }

        public interface FragmentFactory
        {
            int GetNumFeatures(ref Binary binary, MetricIndex metricIndex);

            int GetNumQuantizedFeatures(ref Binary binary, MetricIndex metricIndex);

            int GetNumNormalizedFeatures(ref Binary binary, MetricIndex metricIndex);

            Fragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime);

            ICreateFragmentsJob PrepareFragmentCreateJob(ref FragmentArray fragmentArray, ref Binary binary);
        }


        struct PoseFragmentFactory : FragmentFactory
        {
            public int GetNumFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return PoseFragment.GetNumFeatures(ref binary, metricIndex);
            }

            public int GetNumQuantizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return 0;
            }

            public int GetNumNormalizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return 0;
            }

            public Fragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
            {
                return PoseFragmentWrapper.Create(ref binary, metricIndex, samplingTime);
            }

            public ICreateFragmentsJob PrepareFragmentCreateJob(ref FragmentArray fragmentArray, ref Binary binary)
            {
                return CreatePoseFragmentsJob.Prepare(ref fragmentArray, ref binary);
            }

            public static FragmentFactory Create()
            {
                return new PoseFragmentFactory();
            }
        }

        struct TrajectoryFragmentFactory : FragmentFactory
        {
            public int GetNumFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return TrajectoryFragment.GetNumFeatures(ref binary, metricIndex);
            }

            public int GetNumQuantizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return TrajectoryFragment.GetNumQuantizedFeatures(ref binary, metricIndex);
            }

            public int GetNumNormalizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return TrajectoryFragment.GetNumNormalizedFeatures(ref binary, metricIndex);
            }

            public Fragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
            {
                return TrajectoryFragmentWrapper.Create(ref binary, metricIndex, samplingTime);
            }

            public ICreateFragmentsJob PrepareFragmentCreateJob(ref FragmentArray fragmentArray, ref Binary binary)
            {
                return CreateTrajectoryFragmentsJob.Prepare(ref fragmentArray, ref binary);
            }

            public static FragmentFactory Create()
            {
                return new TrajectoryFragmentFactory();
            }
        }

        class CodeBook
        {
            public int index;

            public Metric metric;

            public int traitIndex;

            public List<TaggedInterval> intervals = new List<TaggedInterval>();

            public class FragmentEncoder : IDisposable
            {
                public NativeArray<byte>? codes;
                public NativeArray<byte> Codes => codes.Value;

                public NativeArray<byte>? quantizedValues;
                public NativeArray<byte> QuantizedValues => quantizedValues.Value;

                public NativeArray<float3>? codeWords;
                public NativeArray<float3> CodeWords => codeWords.Value;

                public NativeArray<BoundingBox>? boundingBoxes;
                public NativeArray<BoundingBox> BoundingBoxes => boundingBoxes.Value;

                public NativeArray<Quantizer>? quantizers;
                public NativeArray<Quantizer> Quantizers => quantizers.Value;

                public int numFragments;

                Settings settings;

                bool bCancel;

                public struct Settings
                {
                    public int metricIndex;
                    public int numAttempts;
                    public int numIterations;
                    public int minimumNumberSamples;
                    public int maximumNumberSamples;

                    public static Settings Default => new Settings();
                }

                public static FragmentEncoder Create(FragmentEncoder.Settings settings)
                {
                    return new FragmentEncoder(settings);
                }

                public void Dispose()
                {
                    codes?.Dispose();

                    quantizedValues?.Dispose();

                    codeWords?.Dispose();

                    boundingBoxes?.Dispose();

                    quantizers?.Dispose();
                }

                FragmentEncoder(FragmentEncoder.Settings settings)
                {
                    this.settings = settings;
                    codes = null;
                    quantizedValues = null;
                    codeWords = null;
                    boundingBoxes = null;
                    quantizers = null;
                    bCancel = false;
                }

                public void CancelEncoding()
                {
                    bCancel = true;
                }

                public IEnumerator EncodeFragments(MemoryRef<Binary> binary, TaggedInterval[] intervals, FragmentFactory factory, string fragmentTypeName, Action<ProgressInfo> progressFeedback)
                {
                    progressFeedback.Invoke(new ProgressInfo()
                    {
                        title = $"Start {fragmentTypeName} fragment encoder",
                        progress = 0.0f
                    });
                    //
                    // Prologue
                    //

                    int metricIndex = settings.metricIndex;

                    var numFeatures = factory.GetNumFeatures(ref binary.Ref, metricIndex);

                    var numQuantizedFeatures = factory.GetNumQuantizedFeatures(ref binary.Ref, metricIndex);

                    var numNormalizedFeatures = factory.GetNumNormalizedFeatures(ref binary.Ref, metricIndex);

                    var numTransformedFeatures = numFeatures - numQuantizedFeatures - numNormalizedFeatures;

                    numFragments = 0;

                    foreach (var interval in intervals)
                    {
                        numFragments += interval.numFrames;
                    }

                    //
                    // Generate fragments
                    //

                    FragmentArray fragmentArray = FragmentArray.Create(metricIndex, numFragments, numFeatures);

                    int writeIndex = 0;

                    foreach (var interval in intervals)
                    {
                        int segmentIndex = interval.segmentIndex;

                        Assert.IsTrue(
                            interval.firstFrame >=
                            interval.segment.destination.FirstFrame);

                        Assert.IsTrue(
                            interval.onePastLastFrame <=
                            interval.segment.destination.OnePastLastFrame);

                        int relativeFirstFrame =
                            interval.firstFrame -
                            interval.segment.destination.FirstFrame;

                        int numFrames = interval.numFrames;

                        for (int i = 0; i < numFrames; ++i)
                        {
                            int frameIndex = relativeFirstFrame + i;

                            var timeIndex =
                                TimeIndex.Create(
                                    segmentIndex, frameIndex);

                            var samplingTime =
                                SamplingTime.Create(timeIndex);


                            fragmentArray.samplingTimes[writeIndex++] = samplingTime;
                        }
                    }

                    Assert.IsTrue(writeIndex == numFragments);

                    progressFeedback.Invoke(new ProgressInfo()
                    {
                        title = $"Create {fragmentTypeName} fragments",
                        progress = 0.0f
                    });

                    ICreateFragmentsJob createFragmentsJob = factory.PrepareFragmentCreateJob(ref fragmentArray, ref binary.Ref);
                    JobHandle createFragmentsHandle = createFragmentsJob.Schedule();

                    yield return null;
                    if (bCancel)
                    {
                        createFragmentsHandle.Complete();
                        fragmentArray.Dispose();
                        yield break;
                    }

                    createFragmentsHandle.Complete();

                    progressFeedback.Invoke(new ProgressInfo()
                    {
                        title = $"Quantize {fragmentTypeName} fragments",
                        progress = 0.0f
                    });

                    //
                    // Generate feature quantizers
                    //

                    quantizers =
                        new NativeArray<Quantizer>(
                            numQuantizedFeatures, Allocator.Persistent);

                    ComputeQuantizersJob computeQuantizersJob = new ComputeQuantizersJob()
                    {
                        fragmentArray = fragmentArray,
                        quantizers = Quantizers
                    };

                    JobHandle computeQuantizersHandle = computeQuantizersJob.Schedule(numQuantizedFeatures, 1);
                    computeQuantizersHandle.Complete();


                    //
                    // Quantize magnitudes and normalize fragments
                    //

                    int numQuantizedValues = numFragments * numQuantizedFeatures;

                    quantizedValues =
                        new NativeArray<byte>(
                            numQuantizedValues, Allocator.Persistent);

                    NormalizeFeaturesJob normalizeFeaturesJob = new NormalizeFeaturesJob()
                    {
                        numQuantizedFeatures = numQuantizedFeatures,
                        quantizers = Quantizers,
                        quantizedValues = new MemoryArray<byte>(QuantizedValues),
                        fragmentArray = fragmentArray
                    };

                    JobHandle normalizeFeaturesHandle = normalizeFeaturesJob.Schedule(numFragments, 1);
                    normalizeFeaturesHandle.Complete();

                    //
                    // Generate bounding boxes for feature normalization
                    //

                    boundingBoxes =
                        new NativeArray<BoundingBox>(
                            numTransformedFeatures, Allocator.Persistent);

                    ComputeBoundingBoxesJob computeBoundingBoxesJob = new ComputeBoundingBoxesJob()
                    {
                        fragmentArray = fragmentArray,
                        numTransformedFeatures = numTransformedFeatures,
                        transformedIndex = numFeatures - numTransformedFeatures,
                        boundingBoxes = BoundingBoxes
                    };

                    JobHandle computeBoundingBoxesHandle = computeBoundingBoxesJob.Schedule(numTransformedFeatures, 1);
                    computeBoundingBoxesHandle.Complete();

                    //
                    // Normalize fragments
                    //

                    NormalizeFragmentsJob normalizeFragmentsJob = new NormalizeFragmentsJob()
                    {
                        numTransformedFeatures = numTransformedFeatures,
                        transformedIndex = numFeatures - numTransformedFeatures,
                        boundingBoxes = BoundingBoxes,
                        fragmentArray = fragmentArray
                    };

                    JobHandle normalizeFragmentsHandle = normalizeFragmentsJob.Schedule(numFragments, 1);
                    normalizeFragmentsHandle.Complete();


                    //
                    // Product Quantization
                    //

                    progressFeedback.Invoke(new ProgressInfo()
                    {
                        title = $"Prepare training {fragmentTypeName} fragments",
                        progress = 0.0f
                    });

                    yield return null;
                    if (bCancel)
                    {
                        fragmentArray.Dispose();
                        yield break;
                    }

                    int numCodes = numFragments * numFeatures;

                    int numCodeWords = numFeatures * Binary.CodeBook.kNumCodeValues;

                    codes = new NativeArray<byte>(numCodes, Allocator.Persistent);

                    codeWords = new NativeArray<float3>(numCodeWords, Allocator.Persistent);

                    var pqs = ProductQuantizer.Settings.Default;

                    pqs.numAttempts = settings.numAttempts;
                    pqs.numIterations = settings.numIterations;
                    pqs.minimumNumberSamples = settings.minimumNumberSamples;
                    pqs.maximumNumberSamples = settings.maximumNumberSamples;

                    using (var pq = new ProductQuantizer(numFeatures * 3, numFeatures, pqs))
                    {
                        using (ProductQuantizer.TrainingData trainingData = pq.ScheduleTraining(ref fragmentArray))
                        {
                            float progression = 0.0f;
                            do
                            {
                                progression = trainingData.FrameUpdate();

                                progressFeedback.Invoke(new ProgressInfo()
                                {
                                    title = $"Train {fragmentTypeName} fragments",
                                    progress = progression
                                });

                                yield return null;
                                if (bCancel)
                                {
                                    trainingData.ForceCompleteCurrentBatch();
                                    fragmentArray.Dispose();
                                    yield break;
                                }
                            }
                            while (progression < 1.0f);

                            trainingData.ForceCompleteCurrentBatch();
                        }

                        progressFeedback.Invoke(new ProgressInfo()
                        {
                            title = $"Compute {fragmentTypeName} codes",
                            progress = 0.0f
                        });
                        pq.ComputeCodes(ref fragmentArray, Codes);

                        Assert.IsTrue(pq.centroids.Length == numCodeWords * 3);

                        for (int i = 0; i < numCodeWords; ++i)
                        {
                            float x = pq.centroids[i * 3 + 0];
                            float y = pq.centroids[i * 3 + 1];
                            float z = pq.centroids[i * 3 + 2];

                            var centroid = new float3(x, y, z);

                            var words = CodeWords;
                            words[i] = centroid;
                        }
                    }

                    fragmentArray.Dispose();
                }
            }

            public static CodeBook Create(Metric metric, int traitIndex)
            {
                return new CodeBook
                {
                    metric = metric,
                    traitIndex = traitIndex
                };
            }

            public FragmentEncoder CreateEncoder(ref Binary binary)
            {
                var settings = FragmentEncoder.Settings.Default;

                settings.metricIndex = metric.index;
                settings.numAttempts = metric.numAttempts;
                settings.numIterations = metric.numIterations;
                settings.minimumNumberSamples = metric.minimumNumberSamples;
                settings.maximumNumberSamples = metric.maximumNumberSamples;

                return FragmentEncoder.Create(settings);
            }
        }

        List<CodeBook> codeBooks = new List<CodeBook>();

        IEnumerator BuildFragments()
        {
            GenerateCodeBooks();

            allocator.Allocate(codeBooks.Count, ref Binary.codeBooks);

            int codeBookIndex = 0;

            foreach (var codeBook in codeBooks)
            {
                var metricIndex = codeBook.metric.index;
                var traitIndex = codeBook.traitIndex;

                MemoryRef<Binary.CodeBook> destination = new MemoryRef<Binary.CodeBook>(ref Binary.codeBooks[codeBookIndex]);

                destination.Ref.metricIndex = metricIndex;
                destination.Ref.traitIndex = traitIndex;

                var numIntervals = codeBook.intervals.Count;

                allocator.Allocate(numIntervals, ref destination.Ref.intervals);

                int intervalIndex = 0;

                foreach (var interval in codeBook.intervals)
                {
                    destination.Ref.intervals[intervalIndex] = interval.index;

                    intervalIndex++;
                }

                Assert.IsTrue(intervalIndex == codeBook.intervals.Count);

                {
                    FragmentFactory fragmentFactory = PoseFragmentFactory.Create();

                    using (CodeBook.FragmentEncoder factory = codeBook.CreateEncoder(ref Binary))
                    {
                        IEnumerator fragmentEncoder = factory.EncodeFragments(binary, codeBook.intervals.ToArray(), fragmentFactory, "pose", progressInfo => progressFeedback?.Invoke(progressInfo));
                        while (fragmentEncoder.MoveNext())
                        {
                            yield return null;
                            if (bCancel)
                            {
                                factory.CancelEncoding();
                                fragmentEncoder.MoveNext(); // release resources

                                yield break;
                            }
                        }

                        int numFragments = factory.numFragments;

                        Assert.IsTrue(destination.Ref.numFragments == 0);

                        destination.Ref.numFragments = numFragments;

                        var numFeatures = PoseFragment.GetNumFeatures(ref Binary, metricIndex);

                        var numQuantizedFeatures = PoseFragment.GetNumQuantizedFeatures(ref Binary, metricIndex);

                        var numNormalizedFeatures = PoseFragment.GetNumNormalizedFeatures(ref Binary, metricIndex);

                        var numTransformedFeatures = numFeatures - numQuantizedFeatures - numNormalizedFeatures;

                        destination.Ref.poses.numFragments = numFragments;
                        destination.Ref.poses.numFeatures = (short)numFeatures;
                        destination.Ref.poses.numFeaturesQuantized = (short)numQuantizedFeatures;
                        destination.Ref.poses.numFeaturesNormalized = (short)numNormalizedFeatures;
                        destination.Ref.poses.numFeaturesTransformed = (short)numTransformedFeatures;

                        var numCodes = factory.Codes.Length + factory.QuantizedValues.Length;

                        allocator.Allocate(numCodes, ref destination.Ref.poses.codes);

                        int writeIndex = 0;
                        int readIndexQuantized = 0;
                        int readIndex = 0;

                        for (int i = 0; i < numFragments; ++i)
                        {
                            for (int j = 0; j < numQuantizedFeatures; ++j)
                            {
                                var quantizedValue = factory.QuantizedValues[readIndexQuantized++];

                                destination.Ref.poses.codes[writeIndex++] = quantizedValue;
                            }

                            for (int j = 0; j < numFeatures; ++j)
                            {
                                var code = factory.Codes[readIndex++];

                                destination.Ref.poses.codes[writeIndex++] = code;
                            }
                        }

                        Assert.IsTrue(readIndexQuantized == factory.QuantizedValues.Length);
                        Assert.IsTrue(readIndex == factory.Codes.Length);
                        Assert.IsTrue(writeIndex == numCodes);

                        var numCodeWords = factory.CodeWords.Length;

                        allocator.Allocate(numCodeWords, ref destination.Ref.poses.centroids);

                        for (int i = 0; i < numCodeWords; ++i)
                        {
                            destination.Ref.poses.centroids[i] = factory.CodeWords[i];
                        }

                        allocator.Allocate(numTransformedFeatures, ref destination.Ref.poses.boundingBoxes);

                        Assert.IsTrue(numTransformedFeatures == factory.BoundingBoxes.Length);

                        for (int i = 0; i < numTransformedFeatures; ++i)
                        {
                            destination.Ref.poses.boundingBoxes[i].transform = factory.BoundingBoxes[i].transform;
                            destination.Ref.poses.boundingBoxes[i].extent = factory.BoundingBoxes[i].extent;
                            destination.Ref.poses.boundingBoxes[i].inverseDiagonal = factory.BoundingBoxes[i].inverseDiagonal;
                        }

                        allocator.Allocate(numQuantizedFeatures, ref destination.Ref.poses.quantizers);

                        Assert.IsTrue(numQuantizedFeatures == factory.Quantizers.Length);

                        for (int i = 0; i < numQuantizedFeatures; ++i)
                        {
                            destination.Ref.poses.quantizers[i].minimum = factory.Quantizers[i].minimum;
                            destination.Ref.poses.quantizers[i].range = factory.Quantizers[i].range;
                        }
                    }
                }

                {
                    FragmentFactory fragmentFactory = TrajectoryFragmentFactory.Create();

                    using (CodeBook.FragmentEncoder factory = codeBook.CreateEncoder(ref Binary))
                    {
                        IEnumerator fragmentEncoder = factory.EncodeFragments(binary, codeBook.intervals.ToArray(), fragmentFactory, "trajectory", progressInfo => progressFeedback?.Invoke(progressInfo));
                        while (fragmentEncoder.MoveNext())
                        {
                            yield return null;
                            if (bCancel)
                            {
                                factory.CancelEncoding();
                                fragmentEncoder.MoveNext(); // release resources

                                yield break;
                            }
                        }

                        int numFragments = factory.numFragments;

                        Assert.IsTrue(destination.Ref.numFragments == numFragments);

                        var numFeatures = TrajectoryFragment.GetNumFeatures(ref Binary, metricIndex);

                        var numQuantizedFeatures = TrajectoryFragment.GetNumQuantizedFeatures(ref Binary, metricIndex);

                        var numNormalizedFeatures = TrajectoryFragment.GetNumNormalizedFeatures(ref Binary, metricIndex);

                        var numTransformedFeatures = numFeatures - numQuantizedFeatures - numNormalizedFeatures;

                        destination.Ref.trajectories.numFragments = numFragments;
                        destination.Ref.trajectories.numFeatures = (short)numFeatures;
                        destination.Ref.trajectories.numFeaturesQuantized = (short)numQuantizedFeatures;
                        destination.Ref.trajectories.numFeaturesNormalized = (short)numNormalizedFeatures;
                        destination.Ref.trajectories.numFeaturesTransformed = (short)numTransformedFeatures;

                        var numCodes = factory.Codes.Length + factory.QuantizedValues.Length;

                        allocator.Allocate(numCodes, ref destination.Ref.trajectories.codes);

                        int writeIndex = 0;
                        int readIndexQuantized = 0;
                        int readIndex = 0;

                        for (int i = 0; i < numFragments; ++i)
                        {
                            for (int j = 0; j < numQuantizedFeatures; ++j)
                            {
                                var quantizedValue = factory.QuantizedValues[readIndexQuantized++];

                                destination.Ref.trajectories.codes[writeIndex++] = quantizedValue;
                            }

                            for (int j = 0; j < numFeatures; ++j)
                            {
                                var code = factory.Codes[readIndex++];

                                destination.Ref.trajectories.codes[writeIndex++] = code;
                            }
                        }

                        Assert.IsTrue(readIndexQuantized == factory.QuantizedValues.Length);
                        Assert.IsTrue(readIndex == factory.Codes.Length);
                        Assert.IsTrue(writeIndex == numCodes);

                        var numCodeWords = factory.CodeWords.Length;

                        allocator.Allocate(numCodeWords, ref destination.Ref.trajectories.centroids);

                        for (int i = 0; i < numCodeWords; ++i)
                        {
                            destination.Ref.trajectories.centroids[i] = factory.CodeWords[i];
                        }

                        allocator.Allocate(numTransformedFeatures, ref destination.Ref.trajectories.boundingBoxes);

                        Assert.IsTrue(numTransformedFeatures == factory.BoundingBoxes.Length);

                        for (int i = 0; i < numTransformedFeatures; ++i)
                        {
                            destination.Ref.trajectories.boundingBoxes[i].transform = factory.BoundingBoxes[i].transform;
                            destination.Ref.trajectories.boundingBoxes[i].extent = factory.BoundingBoxes[i].extent;
                            destination.Ref.trajectories.boundingBoxes[i].inverseDiagonal = factory.BoundingBoxes[i].inverseDiagonal;
                        }

                        allocator.Allocate(numQuantizedFeatures, ref destination.Ref.trajectories.quantizers);

                        Assert.IsTrue(numQuantizedFeatures == factory.Quantizers.Length);

                        for (int i = 0; i < numQuantizedFeatures; ++i)
                        {
                            destination.Ref.trajectories.quantizers[i].minimum = factory.Quantizers[i].minimum;
                            destination.Ref.trajectories.quantizers[i].range = factory.Quantizers[i].range;
                        }
                    }
                }

                codeBookIndex++;
            }

            Assert.IsTrue(codeBookIndex == codeBooks.Count);
        }

        void GenerateCodeBooks()
        {
            int intervalIndex = 0;

            ref Binary binary = ref Binary;

            foreach (var taggedInterval in intervals)
            {
                (var metric, var traitIndex) = GetMetric(taggedInterval);

                Assert.IsTrue(
                    binary.intervals[intervalIndex].codeBookIndex ==
                    Binary.CodeBookIndex.Invalid);

                if (metric != null)
                {
                    var codeBook = GetOrCreateCodeBook(metric, traitIndex);

                    codeBook.intervals.Add(taggedInterval);

                    binary.intervals[intervalIndex].codeBookIndex = codeBook.index;
                }

                intervalIndex++;
            }

            Assert.IsTrue(intervalIndex == intervals.Count);
        }

        CodeBook GetOrCreateCodeBook(Metric metric, int traitIndex)
        {
            foreach (var codeBook in codeBooks)
            {
                if (codeBook.metric == metric)
                {
                    if (codeBook.traitIndex == traitIndex)
                    {
                        return codeBook;
                    }
                }
            }

            var result = CodeBook.Create(metric, traitIndex);

            result.index = codeBooks.Count;

            codeBooks.Add(result);

            return result;
        }
    }
}
