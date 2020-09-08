using System;
using UnityEngine;
using Unity.Mathematics;

namespace Unity.SnapshotDebugger
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SnapshotDebugger/SnapshotTransform")]
    public class SnapshotTransform : SnapshotProvider
    {
        public enum Type
        {
            Local,
            World,
        }

        public Type frame = Type.Local;

        public bool scale = false;

        public bool quantize = false;

        [Flags]
        [Serializable]
        enum Flags
        {
            Local = 1,
            Quantized = 2,
            Scale = 4,
        }

        Flags GetFlags()
        {
            Flags flags = 0;

            if (frame == Type.Local)
            {
                flags |= Flags.Local;
            }

            if (scale)
            {
                flags |= Flags.Scale;
            }

            if (quantize)
            {
                flags |= Flags.Quantized;
            }

            return flags;
        }

        RigidTransform GetTransform(Flags flags)
        {
            if (flags.HasFlag(Flags.Local))
            {
                return new RigidTransform(
                    transform.localRotation,
                    transform.localPosition);
            }
            else
            {
                return new RigidTransform(
                    transform.rotation,
                    transform.position);
            }
        }

        void SetTransform(RigidTransform transform, Flags flags)
        {
            if (flags.HasFlag(Flags.Local))
            {
                this.transform.localPosition = transform.pos;
                this.transform.localRotation = transform.rot;
            }
            else
            {
                this.transform.position = transform.pos;
                this.transform.rotation = transform.rot;
            }
        }

        void WriteTransform(Buffer buffer, Flags flags)
        {
            if (flags.HasFlag(Flags.Quantized))
            {
                WriteQuantized(buffer, GetTransform(flags));

                if (flags.HasFlag(Flags.Scale))
                {
                    buffer.WriteQuantized(transform.localScale);
                }
            }
            else
            {
                Write(buffer, GetTransform(flags));

                if (flags.HasFlag(Flags.Scale))
                {
                    buffer.Write(transform.localScale);
                }
            }
        }

        void ReadTransform(Buffer buffer, Flags flags)
        {
            if (flags.HasFlag(Flags.Quantized))
            {
                SetTransform(ReadTransformQuantized(buffer), flags);

                if (flags.HasFlag(Flags.Scale))
                {
                    transform.localScale = buffer.ReadVector3Quantized();
                }
            }
            else
            {
                SetTransform(ReadTransform(buffer), flags);

                if (flags.HasFlag(Flags.Scale))
                {
                    transform.localScale = buffer.ReadVector3();
                }
            }
        }

        public override void WriteToStream(Buffer buffer)
        {
            var flags = GetFlags();

            buffer.Write((short)flags);

            WriteTransform(buffer, flags);
        }

        public override void ReadFromStream(Buffer buffer)
        {
            ReadTransform(buffer, (Flags)buffer.Read16());
        }

        public static void Write(Buffer buffer, RigidTransform transform)
        {
            buffer.Write(transform.pos);
            buffer.Write(transform.rot);
        }

        public static void WriteQuantized(Buffer buffer, RigidTransform transform)
        {
            buffer.WriteQuantized(transform.pos);
            buffer.WriteQuantized(transform.rot);
        }

        public static RigidTransform ReadTransform(Buffer buffer)
        {
            float3 t = buffer.ReadVector3();
            quaternion q = buffer.ReadQuaternion();

            return new RigidTransform(q, t);
        }

        public static RigidTransform ReadTransformQuantized(Buffer buffer)
        {
            float3 t = buffer.ReadVector3Quantized();
            quaternion q = buffer.ReadQuaternionQuantized();

            return new RigidTransform(q, t);
        }
    }
}
