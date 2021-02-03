using Unity.Collections;
using UnityEngine;
using Unity.SnapshotDebugger;
using Unity.Mathematics;

using Buffer = Unity.SnapshotDebugger.Buffer;

public partial class MovementController : SnapshotProvider
{
    class State : Serializable
    {
        public float3 desiredDisplacement;

        public float3 desiredVelocity;

        public Closure current;

        public Closure previous;

        public float3 accumulatedVelocity;

        public NativeList<Force> appliedForces;

        internal static State Create()
        {
            return new State()
            {
                desiredDisplacement = Vector3.zero,
                desiredVelocity = Vector3.zero,

                accumulatedVelocity = Vector3.zero,

                appliedForces = new NativeList<Force>(16, Allocator.Persistent),

                current = Closure.Create(),
                previous = Closure.Create()
            };
        }

        internal void CopyFrom(State rhs)
        {
            desiredDisplacement = rhs.desiredDisplacement;
            desiredVelocity = rhs.desiredVelocity;

            accumulatedVelocity = rhs.accumulatedVelocity;

            current = rhs.current;
            previous = rhs.previous;

            appliedForces.Clear();

            appliedForces.AddRange(rhs.appliedForces);
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.Write(desiredDisplacement);
            buffer.Write(desiredVelocity);

            buffer.Write(accumulatedVelocity);

            current.WriteToStream(buffer);
            previous.WriteToStream(buffer);

            appliedForces.WriteToStream(buffer);
        }

        public void ReadFromStream(Buffer buffer)
        {
            desiredDisplacement = buffer.ReadVector3();
            desiredVelocity = buffer.ReadVector3();

            accumulatedVelocity = buffer.ReadVector3();

            current.ReadFromStream(buffer);
            previous.ReadFromStream(buffer);

            appliedForces.ReadFromStream(buffer);
        }

        internal void Dispose()
        {
            if (appliedForces.IsCreated)
            {
                appliedForces.Dispose();
            }
        }
    }
}
