using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Unity.Collections;

using Buffer = Unity.SnapshotDebugger.Buffer;
using System.Linq;

namespace Unity.Kinematica
{
    public partial class Kinematica : SnapshotProvider, IFrameDebugProvider, IMotionSynthesizerProvider
    {
        public int GetUniqueIdentifier()
        {
            return gameObject.GetInstanceID();
        }

        public string GetDisplayName()
        {
            return gameObject.name;
        }

        /// <summary>
        /// return the currently active animation frames
        /// </summary>
        /// <returns></returns>
        public virtual void LateUpdate()
        {
            Debugger.frameDebugger.AddFrameRecords<AnimationDebugRecord>(this, synthesizer.GetFrameDebugInfo());
            synthesizer.AddCostRecordsToFrameDebugger(this);
        }

        /// <summary>
        /// Stores the contents of the Kinematica component in the buffer passed as argument.
        /// </summary>
        /// <param name="buffer">Buffer that the contents of the Kinematica component should be written to.</param>
        public override void WriteToStream(Buffer buffer)
        {
            buffer.Write(transform.position);
            buffer.Write(transform.rotation);

            synthesizer.WriteToStream(buffer);
        }

        /// <summary>
        /// Retrieves the contents of the Kinematica component from the buffer passed as argument.
        /// </summary>
        /// <param name="buffer">Buffer that the contents of the Kinematica component should be read from.</param>
        public override void ReadFromStream(Buffer buffer)
        {
            transform.position = buffer.ReadVector3();
            transform.rotation = buffer.ReadQuaternion();

            synthesizer.ReadFromStream(buffer);
        }
    }
}
