using Unity.Collections;
using Unity.Burst;
using System;
using Buffer = Unity.SnapshotDebugger.Buffer;
using Unity.SnapshotDebugger;
using System.Collections.Generic;

namespace Unity.Kinematica
{
    public struct DebugMemory : IDisposable
    {
        NativeList<DebugCostRecord> costRecords;
        Buffer buffer;
        int group;
        ushort version;
        int size;

        struct Header
        {
            public DebugReference reference;
            public int nextAddress;
        }

        public static DebugMemory Create(int capacity, Allocator allocator)
        {
            return new DebugMemory()
            {
                costRecords = new NativeList<DebugCostRecord>(allocator),
                buffer = Buffer.Create(capacity, allocator),
                version = 1,
                group = 0,
                size = 0,
            };
        }

        public void Dispose()
        {
            costRecords.Dispose();
            buffer.Dispose();
        }

        internal IEnumerable<IFrameRecord> CostRecords
        {
            get
            {
                foreach (DebugCostRecord record in costRecords)
                {
                    yield return record;
                }
            }
        }

        public void AddCostRecord(NativeString64 queryDebugName, float poseCost, float trajectoryCost)
        {
            costRecords.Add(new DebugCostRecord()
            {
                queryDebugName = queryDebugName,
                poseCost = poseCost,
                trajectoryCost = trajectoryCost
            });
        }

        public DebugReference FirstOrDefault => size == 0 ? DebugReference.Invalid : GetObjectReference(0);

        public DebugReference Next(DebugReference current)
        {
            Header header = ReadHeader(current.address);
            if (header.nextAddress >= size)
            {
                return DebugReference.Invalid;
            }

            return GetObjectReference(header.nextAddress);
        }

        public DebugReference GetObjectReference(int cursor)
        {
            return ReadHeader(cursor).reference;
        }

        public DebugReference FindObjectReference(DebugIdentifier identifier)
        {
            for (DebugReference debugRef = FirstOrDefault; debugRef.IsValid; debugRef = Next(debugRef))
            {
                if (debugRef.identifier.Equals(identifier))
                {
                    return debugRef;
                }
            }

            return DebugReference.Invalid;
        }

        public void PushGroup()
        {
            ++group;
        }

        public DebugIdentifier WriteUnblittableObject<T>(ref T obj, bool dataOnly = false) where T : struct, IDebugObject, Serializable
        {
            if (IsObjectAlreadyWritten(ref obj))
            {
                return obj.debugIdentifier;
            }

            Header header = CreateAndWriteHeader(obj.debugIdentifier, dataOnly);

            obj.WriteToStream(buffer);

            WriteHeaderOffset(ref header);

            return obj.debugIdentifier;
        }

        public DebugIdentifier WriteBlittableObject<T>(ref T obj, bool dataOnly = false) where T : struct, IDebugObject
        {
            if (IsObjectAlreadyWritten(ref obj))
            {
                return obj.debugIdentifier;
            }

            Header header = CreateAndWriteHeader(obj.debugIdentifier, dataOnly);

            buffer.WriteBlittable(obj);

            WriteHeaderOffset(ref header);

            return obj.debugIdentifier;
        }

        public T ReadObject<T>(DebugReference reference) where T : struct, IDebugObject
        {
            T obj = new T();

            int previousCursor = buffer.SetCursor(reference.address);
            Header header = buffer.ReadBlittable<Header>();

            if (obj is Serializable serializable)
            {
                serializable.ReadFromStream(buffer);
                obj = (T)serializable;
            }
            else
            {
                obj = buffer.ReadBlittable<T>();
            }

            obj.debugIdentifier = reference.identifier;

            buffer.SetCursor(previousCursor);

            return obj;
        }

        public T ReadObjectFromIdentifier<T>(DebugIdentifier identifier) where T : struct, IDebugObject
        {
            DebugReference reference = FindObjectReference(identifier);
            if (!reference.IsValid)
            {
                throw new ArgumentException("Identifier not bound to valid reference in debug memory", "identifier");
            }

            return ReadObject<T>(reference);
        }

        public void Reset()
        {
            costRecords.Clear();
            buffer.Clear();
            group = 0;
            size = 0;
            ++version;
        }

        DebugIdentifier CreateIdentifier<T>() where T : struct
        {
            int typeHashCode = BurstRuntime.GetHashCode32<T>();
            ushort currentIndex = 0;

            for (DebugReference debugRef = FirstOrDefault; debugRef.IsValid; debugRef = Next(debugRef))
            {
                if (debugRef.identifier.typeHashCode == typeHashCode)
                {
                    currentIndex = debugRef.identifier.index;
                    ++currentIndex;
                }
            }

            return new DebugIdentifier()
            {
                index = currentIndex,
                version = version,
                typeHashCode = typeHashCode
            };
        }

        bool IsValid(DebugIdentifier identifier)
        {
            return identifier.IsValid && identifier.version == version;
        }

        bool IsObjectAlreadyWritten<T>(ref T obj) where T : struct, IDebugObject
        {
            if (!obj.debugIdentifier.IsValid)
            {
                obj.debugIdentifier = CreateIdentifier<T>();
                return false;
            }
            else
            {
                for (DebugReference reference = FirstOrDefault; reference.IsValid; reference = Next(reference))
                {
                    if (reference.identifier.EqualsIndexAndVersion(obj.debugIdentifier))
                    {
                        // already written
                        return true;
                    }
                }
            }

            return false;
        }

        Header CreateAndWriteHeader(DebugIdentifier identifier, bool dataOnly)
        {
            Header header = new Header()
            {
                reference = new DebugReference()
                {
                    identifier = identifier,
                    address = buffer.Length,
                    group = group,
                    dataOnly = dataOnly
                },
                nextAddress = -1
            };

            buffer.WriteBlittable(header);

            return header;
        }

        Header ReadHeader(int cursor)
        {
            int prevCursor = buffer.SetCursor(cursor);
            Header header = buffer.ReadBlittable<Header>();
            buffer.SetCursor(prevCursor);

            return header;
        }

        void WriteHeader(int cursor, Header header)
        {
            int prevCursor = buffer.SetCursor(cursor);
            buffer.WriteBlittable(header);
            buffer.SetCursor(prevCursor);
        }

        void WriteHeaderOffset(ref Header header)
        {
            header.nextAddress = buffer.Length;
            WriteHeader(header.reference.address, header);

            size = buffer.Length;
        }
    }
}
