using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer : IDisposable
    {
        MotionSynthesizer(BlobAssetReference<Binary> binary, AffineTransform worldRootTransform, float blendDuration, Allocator allocator)
        {
            m_binary = binary;

            arrayMemory = ArrayMemory.Create();

            ReserveTraitTypes(ref arrayMemory);
            PoseGenerator.Reserve(ref arrayMemory, ref binary.Value);
            TrajectoryModel.Reserve(ref arrayMemory, ref binary.Value);

            arrayMemory.Allocate(allocator);

            // We basically copy statically available data into this instance
            // so that the burst compiler does not complain about accessing static data.
            traitTypes = ConstructTraitTypes(ref arrayMemory, ref binary.Value);

            poseGenerator = new PoseGenerator(ref arrayMemory, ref binary.Value, blendDuration);

            trajectory = new TrajectoryModel(ref arrayMemory, ref binary.Value);

            rootTransform = worldRootTransform;
            rootDeltaTransform = AffineTransform.identity;

            updateInProgress = false;

            _deltaTime = 0.0f;

            lastSamplingTime = TimeIndex.Invalid;

            samplingTime = SamplingTime.Invalid;

            delayedPushTime = TimeIndex.Invalid;

            frameCount = -1;

            lastProcessedFrameCount = -1;

            isValid = true;

            isDebugging = false;
            readDebugMemory = DebugMemory.Create(1024, allocator);
            writeDebugMemory = DebugMemory.Create(1024, allocator);
        }

        public void Dispose()
        {
            if (isValid)
            {
                arrayMemory.Dispose();
                readDebugMemory.Dispose();
                writeDebugMemory.Dispose();

                isValid = false;
            }
        }

        [ReadOnly]
        private BlobAssetReference<Binary> m_binary;

        ArrayMemory arrayMemory;

        /// <summary>
        /// The trajectory model maintains a representation of the simulated
        /// character movement over the global time horizon.
        /// </summary>
        public TrajectoryModel trajectory;

        /// <summary>
        /// Denotes the delta time in seconds during the last update.
        /// </summary>
        public float deltaTime => _deltaTime;

        internal AffineTransform rootTransform;

        internal AffineTransform rootDeltaTransform;

        NativeSlice<TraitType> traitTypes;

        BlittableBool updateInProgress;

        PoseGenerator poseGenerator;

        float _deltaTime;

        SamplingTime samplingTime;

        TimeIndex lastSamplingTime;

        TimeIndex delayedPushTime;

        private int frameCount;

        private int lastProcessedFrameCount;

        bool isValid;

        bool isDebugging;
        DebugMemory readDebugMemory;
        DebugMemory writeDebugMemory;
    }
}
