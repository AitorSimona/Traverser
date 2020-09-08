using System.Collections.Generic;
using System.Linq;
using Unity.SnapshotDebugger;

using Buffer = Unity.SnapshotDebugger.Buffer;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        /// <summary>
        /// return the currently active animation frames
        /// </summary>
        /// <returns></returns>
        public List<IFrameRecord> GetFrameDebugInfo()
        {
            List<IFrameRecord> snapshots = new List<IFrameRecord>();

#if UNITY_EDITOR
            if (poseGenerator.CurrentPushIndex >= 0)
            {
                AnimationSampleTimeIndex animSampleTime = Binary.GetAnimationSampleTimeIndex(Time.timeIndex);

                if (animSampleTime.IsValid)
                {
                    AnimationFrameDebugInfo lastFrame = new AnimationFrameDebugInfo()
                    {
                        sequenceIdentifier = poseGenerator.CurrentPushIndex,
                        animName = animSampleTime.clipName,
                        animFrame = animSampleTime.animFrameIndex,
                        weight = poseGenerator.ApproximateTransitionProgression,
                        blendOutDuration = BlendDuration,
                    };
                    snapshots.Add(lastFrame);
                }
            }
#endif

            return snapshots;
        }

        internal void WriteToStream(Buffer buffer)
        {
            buffer.Write(rootTransform);
            buffer.Write(rootDeltaTransform);
            buffer.Write(samplingTime);
            buffer.Write(lastSamplingTime);
            buffer.Write(delayedPushTime);

            poseGenerator.WriteToStream(buffer);
            trajectory.WriteToStream(buffer);
        }

        internal void ReadFromStream(Buffer buffer)
        {
            rootTransform = buffer.ReadAffineTransform();
            rootDeltaTransform = buffer.ReadAffineTransform();
            samplingTime = buffer.ReadSamplingTime();
            lastSamplingTime = buffer.ReadTimeIndex();
            delayedPushTime = buffer.ReadTimeIndex();

            poseGenerator.ReadFromStream(buffer);
            trajectory.ReadFromStream(buffer);
        }

        public bool IsDebugging => isDebugging;

        public void UpdateDebuggingStatus()
        {
            isDebugging = false;

            foreach (SelectedFrameDebugProvider selected in Debugger.frameDebugger.Selection)
            {
                if (selected.providerInfo.provider is IMotionSynthesizerProvider provider)
                {
                    if (provider.Synthesizer.Equals(ref this))
                    {
                        isDebugging = true;
                        break;
                    }
                }
            }
        }

        public void DebugPushGroup()
        {
            writeDebugMemory.PushGroup();
        }

        public DebugIdentifier DebugWriteBlittableObject<T>(ref T obj, bool dataOnly = false) where T : struct, IDebugObject
        {
            return writeDebugMemory.WriteBlittableObject(ref obj, dataOnly);
        }

        public DebugIdentifier DebugWriteUnblittableObject<T>(ref T obj, bool dataOnly = false) where T : struct, IDebugObject, Serializable
        {
            return writeDebugMemory.WriteUnblittableObject(ref obj, dataOnly);
        }

        public T DebugReadObject<T>(DebugReference reference) where T : struct, IDebugObject
        {
            return readDebugMemory.ReadObject<T>(reference);
        }

        public void AddCostRecordsToFrameDebugger(IFrameDebugProvider frameDebugProvider)
        {
            Debugger.frameDebugger.AddFrameRecords<DebugCostAggregate>(frameDebugProvider, ReadDebugMemory.CostRecords.ToList());
        }

        internal DebugMemory ReadDebugMemory => readDebugMemory;
    }
}
