using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.Kinematica
{
    /// <summary>
    /// Example of Burstable animation job that can be ran in a separate thread to execute Kinematica animation pass for
    /// best performances.
    /// </summary>
    [BurstCompile]
    internal struct UpdateAnimationPoseJob : IAnimationJob, System.IDisposable
    {
        NativeArray<TransformStreamHandle> transforms;
        NativeArray<bool> boundJoints;

        MemoryRef<MotionSynthesizer> synthesizer;

        PropertySceneHandle deltaTime;

        bool transformBufferValid;

        /// <summary>
        /// Setup binding between character joints and animator stream, that will be written by Kinematica job
        /// </summary>
        /// <param name="animator">Unity animator associated with the animation job</param>
        /// <param name="transforms">Character joint transforms</param>
        /// <param name="synthesizer">Reference to motion synthesizer</param>
        /// <param name="deltaTimePropertyHandle">Property handle for the deltaTime</param>
        /// <returns></returns>
        public bool Setup(Animator animator, Transform[] transforms, ref MotionSynthesizer synthesizer, PropertySceneHandle deltaTimePropertyHandle)
        {
            this.synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);
            int numJoints = synthesizer.Binary.numJoints;
            this.transforms = new NativeArray<TransformStreamHandle>(numJoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            boundJoints = new NativeArray<bool>(numJoints, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // Root joint is always first transform, and names don't need to match contrary to other joints
            this.transforms[0] = animator.BindStreamTransform(transforms[0]);
            boundJoints[0] = true;

            for (int i = 1; i < transforms.Length; i++)
            {
                int jointNameIndex = synthesizer.Binary.GetStringIndex(transforms[i].name);
                int jointIndex = (jointNameIndex >= 0) ? synthesizer.Binary.animationRig.GetJointIndexForNameIndex(jointNameIndex) : -1;
                if (jointIndex >= 0)
                {
                    this.transforms[jointIndex] = animator.BindStreamTransform(transforms[i]);
                    boundJoints[jointIndex] = true;
                }
            }

            deltaTime = deltaTimePropertyHandle;

            return true;
        }

        public void Dispose()
        {
            transforms.Dispose();
            boundJoints.Dispose();
        }

        /// <summary>
        /// Run Kinematica pass that will run the task graph and eventually apply motion matching,
        /// and then output the resulting root motion.
        /// </summary>
        /// <param name="stream"></param>
        public void ProcessRootMotion(AnimationStream stream)
        {
            ref MotionSynthesizer synthesizer = ref this.synthesizer.Ref;
            float dt = deltaTime.GetFloat(stream);

            var previousTransform = synthesizer.WorldRootTransform;
            transformBufferValid = synthesizer.Update(dt);


            //convert the delta transform to root motion in the stream
            if (transformBufferValid && dt > 0.0f)
            {
                WriteRootMotion(stream, dt, previousTransform, synthesizer.WorldRootTransform);
            }
        }

        /// <summary>
        /// Write the animated joints transforms computed by Kinematica during the last <code>ProcessRootMotion</code>
        /// call into the stream
        /// </summary>
        /// <param name="stream"></param>
        public void ProcessAnimation(AnimationStream stream)
        {
            ref MotionSynthesizer synthesizer = ref this.synthesizer.Ref;

            if (transformBufferValid)
            {
                int numTransforms = synthesizer.LocalSpaceTransformBuffer.Length;
                for (int i = 1; i < numTransforms; ++i)
                {
                    if (!boundJoints[i])
                    {
                        continue;
                    }

                    transforms[i].SetLocalPosition(stream, synthesizer.LocalSpaceTransformBuffer[i].t);
                    transforms[i].SetLocalRotation(stream, synthesizer.LocalSpaceTransformBuffer[i].q);
                }
            }
        }

        void WriteRootMotion(AnimationStream stream, float deltaTime, AffineTransform previousTransform, AffineTransform updatedTransform)
        {
            float invDeltaTime = 1.0f / deltaTime;
            AffineTransform rootMotion = previousTransform.inverse() * updatedTransform;

            Vector3 rootMotionAngles = ((Quaternion)rootMotion.q).eulerAngles;
            rootMotionAngles = math.radians(rootMotionAngles);

            stream.velocity = invDeltaTime * rootMotion.t;
            stream.angularVelocity = invDeltaTime * rootMotionAngles;
        }
    }
}
