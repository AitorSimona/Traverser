using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;
using Buffer = Unity.SnapshotDebugger.Buffer;

namespace Unity.Kinematica
{
    /// <summary>
    /// Data type to be used in a task graph that represents a trajectory.
    /// </summary>
    [Data("Trajectory", DataFlags.SelfInputOutput)]
    public struct Trajectory : IDebugObject, Serializable, IDisposable, IDebugDrawable
    {
        public NativeArray<AffineTransform> transforms;
        public Allocator allocator;

        public DebugIdentifier debugIdentifier { get; set; }

        public static Trajectory Create(int length, Allocator allocator)
        {
            return new Trajectory()
            {
                transforms = new NativeArray<AffineTransform>(length, allocator),
                allocator = allocator,
                debugIdentifier = DebugIdentifier.Invalid,
            };
        }

        public AffineTransform this[int index]
        {
            get => transforms[index];
            set => transforms[index] = value;
        }

        public int Length => transforms.Length;

        public void Dispose()
        {
            if (allocator != Allocator.Invalid)
            {
                transforms.Dispose();
            }
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.WriteNativeArray(transforms, allocator);
        }

        public void ReadFromStream(Buffer buffer)
        {
            transforms = buffer.ReadNativeArray<AffineTransform>(out allocator);
        }

        public static implicit operator NativeSlice<AffineTransform>(Trajectory trajectory)
        {
            return new NativeSlice<AffineTransform>(trajectory.transforms);
        }

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            if (!debugIdentifier.IsValid)
            {
                return;
            }

            using (Trajectory trajectory = debugMemory.ReadObjectFromIdentifier<Trajectory>(debugIdentifier))
            {
                Binary.TrajectoryFragmentDisplay.Options trajectoryOptions = Binary.TrajectoryFragmentDisplay.Options.Create();

                DebugExtensions.DebugDrawTrajectory(synthesizer.WorldRootTransform,
                    trajectory,
                    synthesizer.Binary.SampleRate,
                    options.inputTrajectoryColor,
                    options.inputTrajectoryColor,
                    trajectoryOptions.showForward);
            }
        }
    }
}
