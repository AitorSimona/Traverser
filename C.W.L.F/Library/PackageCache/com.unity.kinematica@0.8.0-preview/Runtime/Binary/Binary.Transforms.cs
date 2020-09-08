using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Unity.Collections;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Pose and trajectory related methods
        //

        /// <summary>
        /// Returns the number of individual animation poses stored in the runtime asset.
        /// </summary>
        public int numPoses => transforms.Length / animationRig.NumJoints;

        /// <summary>
        /// Returns the sample rate as number of samples per second.
        /// </summary>
        public float SampleRate
        {
            get { return sampleRate; }
            internal set { sampleRate = value; }
        }

        internal int GetFrameIndex(TimeIndex timeIndex)
        {
            ref var segment = ref GetSegment(timeIndex.segmentIndex);

            Assert.IsTrue(timeIndex.frameIndex >= 0);
            Assert.IsTrue(timeIndex.frameIndex < segment.destination.NumFrames);

            return segment.destination.FirstFrame + timeIndex.frameIndex;
        }

        internal float3 GetJointPosition(int jointIndex, int poseIndex)
        {
            Assert.IsTrue(jointIndex < numJoints);
            Assert.IsTrue(poseIndex < numPoses);

            return transforms[jointIndex * numPoses + poseIndex].t;
        }

        internal AffineTransform GetJointTransform(int jointIndex, int poseIndex)
        {
            Assert.IsTrue(jointIndex < numJoints);
            Assert.IsTrue(poseIndex < numPoses);

            return transforms[jointIndex * numPoses + poseIndex];
        }

        internal AffineTransform GetJointTransform(int jointIndex, float sampleTimeInSeconds)
        {
            int sampleKeyFrame = Missing.truncToInt(sampleTimeInSeconds * sampleRate);
            Assert.IsTrue(sampleKeyFrame < numPoses);

            float fractionalKeyFrame = sampleTimeInSeconds * sampleRate;
            float theta = math.saturate(fractionalKeyFrame - sampleKeyFrame);

            // This is intentionally *not* Mathf.Epsilon
            if (theta <= Missing.epsilon)
            {
                return GetJointTransform(jointIndex, sampleKeyFrame);
            }

            AffineTransform t0 = GetJointTransform(jointIndex, sampleKeyFrame + 0);
            AffineTransform t1 = GetJointTransform(jointIndex, sampleKeyFrame + 1);

            return Missing.lerp(t0, t1, theta);
        }

        internal AffineTransform GetJointTransform(int jointIndex, SamplingTime samplingTime)
        {
            var frameIndex = GetFrameIndex(samplingTime.timeIndex);

            float theta = math.saturate(samplingTime.theta);

            var numFramesMinusOne = GetSegment(
                samplingTime.segmentIndex).destination.NumFrames - 1;

            if (samplingTime.frameIndex >= numFramesMinusOne)
            {
                return GetJointTransform(jointIndex, frameIndex);
            }
            else if (theta <= Missing.epsilon)
            {
                return GetJointTransform(jointIndex, frameIndex);
            }
            else if (theta >= 1.0f - Missing.epsilon)
            {
                return GetJointTransform(jointIndex, frameIndex + 1);
            }

            AffineTransform t0 = GetJointTransform(jointIndex, frameIndex + 0);
            AffineTransform t1 = GetJointTransform(jointIndex, frameIndex + 1);

            return Missing.lerp(t0, t1, theta);
        }

        internal AffineTransform GetParentJointTransform(int jointIndex, int poseIndex)
        {
            int parentJointIndex = animationRig.GetParentJointIndex(jointIndex);
            Assert.IsTrue(parentJointIndex >= 0);
            if (parentJointIndex > 0)
            {
                return GetJointTransform(parentJointIndex, poseIndex);
            }
            return AffineTransform.identity;
        }

        /// <summary>
        /// Retrieves the root transform for an animation frame.
        /// </summary>
        /// <param name="poseIndex">The pose index for which the root transform is to be retrieved.</param>
        /// <returns>The root transform that corresponds to the pose index passed as argument.</returns>
        public AffineTransform GetTrajectoryTransform(int poseIndex)
        {
            return GetJointTransform(0, poseIndex);
        }

        /// <summary>
        /// Retrieves the root transform for a sample time in seconds.
        /// </summary>
        /// <param name="sampleTimeInSeconds">The sample time in seconds for which the root transform is to be retrieved.</param>
        /// <returns>The root transform that corresponds to the sample time passed as argument.</returns>
        public AffineTransform GetTrajectoryTransform(float sampleTimeInSeconds)
        {
            return GetJointTransform(0, sampleTimeInSeconds);
        }

        /// <summary>
        /// Retrieves the root transform for a sampling time.
        /// </summary>
        /// <param name="samplingTime">The sampling time for which the root transform is to be retrieved.</param>
        /// <returns>The root transform that corresponds to the sampling time passed as argument.</returns>
        public AffineTransform GetTrajectoryTransform(SamplingTime samplingTime)
        {
            return GetJointTransform(0, samplingTime);
        }

        internal AffineTransform GetTrajectoryTransformGap(SegmentIndex fromSegmentIndex, SegmentIndex toSegmentIndex)
        {
            ref var fromSegment = ref GetSegment(fromSegmentIndex);

            ref var toSegment = ref GetSegment(toSegmentIndex);

            var fromFrameIndex = fromSegment.destination.FirstFrame;
            var toFrameIndex = toSegment.destination.OnePastLastFrame - 1;

            return GetTrajectoryTransform(
                fromFrameIndex).inverseTimes(
                GetTrajectoryTransform(toFrameIndex));
        }

        /// <summary>
        /// Retrieves the root transform displacement for a delta sampling time.
        /// </summary>
        /// <param name="deltaTime">The delta time for which the root transform is to be retrieved.</param>
        /// <returns>The root transform that corresponds to the delta sampling time passed as argument.</returns>
        public AffineTransform GetTrajectoryTransform(DeltaSamplingTime deltaTime)
        {
            return deltaTime.deltaTransform * GetTrajectoryTransform(deltaTime.samplingTime);
        }

        internal AffineTransform GetTrajectoryTransformBetween(float sampleTimeInSeconds, float deltaTime)
        {
            return GetTrajectoryTransform(
                sampleTimeInSeconds).inverseTimes(
                GetTrajectoryTransform(
                    sampleTimeInSeconds + deltaTime));
        }

        /// <summary>
        /// Calculates the relative root transform displacement between a frame and an offset.
        /// </summary>
        /// <param name="poseIndex">The reference frame for which the relative root transform should be calculated.</param>
        /// <param name="offset">An offset in frames that determines the number of frames that the relative root transform should span.</param>
        /// <returns>The relative root transform between a reference and offset frame passed as argument.</returns>
        public AffineTransform GetTrajectoryTransformBetween(int poseIndex, int offset)
        {
            return GetTrajectoryTransform(
                poseIndex).inverseTimes(
                GetTrajectoryTransform(
                    poseIndex + offset));
        }

        /// <summary>
        /// Calculates the relative root transform between a sampling time and a delta time in seconds.
        /// </summary>
        /// <param name="samplingTime">The reference sampling time for which the relative root transform should be calculated.</param>
        /// <param name="deltaTime">An delta time in seconds that determines the duration that the relative root transform should span.</param>
        /// <returns>The relative root transform between a sampling time and a delta time in seconds passed as argument.</returns>
        public AffineTransform GetTrajectoryTransformBetween(SamplingTime samplingTime, float deltaTime)
        {
            AffineTransform referenceTransform =
                GetTrajectoryTransform(samplingTime);

            return referenceTransform.inverseTimes(
                GetTrajectoryTransform(
                    Advance(samplingTime, deltaTime)));
        }

        internal AffineTransform GetTrajectoryDeltaTransform(int poseIndex, int offset)
        {
            AffineTransform baseTransform =
                GetTrajectoryTransform(poseIndex);

            return baseTransform.inverseTimes(
                GetTrajectoryTransform(poseIndex + offset));
        }

        internal float3 GetTrajectoryVelocity(SamplingTime samplingTime, float offsetTimeInSeconds, float timeHorizon)
        {
            AffineTransform referenceTransform =
                GetTrajectoryTransform(samplingTime);

            float deltaTime = math.rcp(SampleRate);

            float halfTimeHorizon = timeHorizon * 0.5f;

            float relativeTimeInSeconds =
                offsetTimeInSeconds - halfTimeHorizon;

            float boundaryTimeInSeconds =
                offsetTimeInSeconds + halfTimeHorizon;

            var numStepsAcrossTimeHorizon =
                Missing.roundToInt(timeHorizon / deltaTime);

            float3 result = Missing.zero;

            AffineTransform previousRelativeTransform =
                referenceTransform.inverseTimes(
                    GetTrajectoryTransform(
                        Advance(samplingTime, relativeTimeInSeconds)));

            for (int i = 0; i < numStepsAcrossTimeHorizon; ++i)
            {
                relativeTimeInSeconds =
                    math.min(boundaryTimeInSeconds,
                        relativeTimeInSeconds + deltaTime);

                AffineTransform relativeTransform =
                    referenceTransform.inverseTimes(
                        GetTrajectoryTransform(
                            Advance(samplingTime, relativeTimeInSeconds)));

                var displacement = relativeTransform.t - previousRelativeTransform.t;

                result += displacement / deltaTime;

                previousRelativeTransform = relativeTransform;
            }

            return result / numStepsAcrossTimeHorizon;
        }

        internal float3 GetTrajectoryForward(SamplingTime samplingTime, float offsetTimeInSeconds, float timeHorizon)
        {
            AffineTransform referenceTransform =
                GetTrajectoryTransform(samplingTime);

            float deltaTime = math.rcp(SampleRate);

            float halfTimeHorizon = timeHorizon * 0.5f;

            float relativeTimeInSeconds =
                offsetTimeInSeconds - halfTimeHorizon;

            float boundaryTimeInSeconds =
                offsetTimeInSeconds + halfTimeHorizon;

            var numStepsAcrossTimeHorizon =
                Missing.roundToInt(timeHorizon / deltaTime);

            float3 result = Missing.zero;

            for (int i = 0; i < numStepsAcrossTimeHorizon; ++i)
            {
                AffineTransform relativeTransform =
                    referenceTransform.inverseTimes(
                        GetTrajectoryTransform(
                            Advance(samplingTime, relativeTimeInSeconds)));

                result += Missing.zaxis(relativeTransform.q);

                relativeTimeInSeconds =
                    math.min(boundaryTimeInSeconds,
                        relativeTimeInSeconds + deltaTime);
            }

            return result / numStepsAcrossTimeHorizon;
        }

        internal TransformBuffer SamplePoseAt(int poseIndex, ref TransformBuffer transformBuffer)
        {
            Assert.IsTrue(transformBuffer.Length == this.numJoints);

            int numJoints = this.numJoints;

            for (int i = 1; i < numJoints; ++i)
            {
                transformBuffer.transforms[i] = GetParentJointTransform(
                    i, poseIndex).inverseTimes(GetJointTransform(i, poseIndex));
            }

            transformBuffer.transforms[0] = AffineTransform.identity;

            return transformBuffer;
        }

        internal void SamplePoseAt(int poseIndex, MemoryArray<AffineTransform> transformBuffer)
        {
            Assert.IsTrue(transformBuffer.Length == this.numJoints);

            int numJoints = this.numJoints;

            for (int i = 1; i < numJoints; ++i)
            {
                transformBuffer[i] = GetParentJointTransform(
                    i, poseIndex).inverseTimes(GetJointTransform(i, poseIndex));
            }

            transformBuffer[0] = AffineTransform.identity;
        }

        internal TransformBuffer SamplePoseAt(float sampleTimeInSeconds, ref TransformBuffer transformBuffer)
        {
            int sampleKeyFrame = Missing.truncToInt(sampleTimeInSeconds * sampleRate);
            Assert.IsTrue(sampleKeyFrame < numPoses);

            float fractionalKeyFrame = sampleTimeInSeconds * sampleRate;
            float theta = math.saturate(fractionalKeyFrame - sampleKeyFrame);

            // This is intentionally *not* Mathf.Epsilon
            if (theta <= Missing.epsilon)
            {
                return SamplePoseAt(sampleKeyFrame, ref transformBuffer);
            }
            else
            {
                Assert.IsTrue(transformBuffer.Length == this.numJoints);

                int numJoints = this.numJoints;

                for (int i = 1; i < numJoints; ++i)
                {
                    AffineTransform lhs = GetParentJointTransform(
                        i, sampleKeyFrame).inverseTimes(GetJointTransform(i, sampleKeyFrame));
                    AffineTransform rhs = GetParentJointTransform(
                        i, sampleKeyFrame + 1).inverseTimes(GetJointTransform(i, sampleKeyFrame + 1));

                    transformBuffer.transforms[i] = Missing.lerp(lhs, rhs, theta);
                }

                transformBuffer.transforms[0] = AffineTransform.identity;

                return transformBuffer;
            }
        }

        internal void SamplePoseAt(float sampleTimeInSeconds, MemoryArray<AffineTransform> transformBuffer)
        {
            int sampleKeyFrame = Missing.truncToInt(sampleTimeInSeconds * sampleRate);
            Assert.IsTrue(sampleKeyFrame < numPoses);

            float fractionalKeyFrame = sampleTimeInSeconds * sampleRate;
            float theta = math.saturate(fractionalKeyFrame - sampleKeyFrame);

            // This is intentionally *not* Mathf.Epsilon
            if (theta <= Missing.epsilon)
            {
                SamplePoseAt(sampleKeyFrame, transformBuffer);
            }
            else
            {
                Assert.IsTrue(transformBuffer.Length == this.numJoints);

                int numJoints = this.numJoints;

                for (int i = 1; i < numJoints; ++i)
                {
                    AffineTransform lhs = GetParentJointTransform(
                        i, sampleKeyFrame).inverseTimes(GetJointTransform(i, sampleKeyFrame));
                    AffineTransform rhs = GetParentJointTransform(
                        i, sampleKeyFrame + 1).inverseTimes(GetJointTransform(i, sampleKeyFrame + 1));

                    transformBuffer[i] = Missing.lerp(lhs, rhs, theta);
                }

                transformBuffer[0] = AffineTransform.identity;
            }
        }

        internal void SamplePoseAt(SamplingTime samplingTime, ref TransformBuffer transformBuffer)
        {
            var frameIndex = GetFrameIndex(samplingTime.timeIndex);

            float theta = math.saturate(samplingTime.theta);

            var numFramesMinusOne = GetSegment(
                samplingTime.segmentIndex).destination.NumFrames - 1;

            if (samplingTime.frameIndex >= numFramesMinusOne)
            {
                SamplePoseAt(frameIndex, ref transformBuffer);
            }
            else if (theta <= Missing.epsilon)
            {
                SamplePoseAt(frameIndex, ref transformBuffer);
            }
            else if (theta >= 1.0f - Missing.epsilon)
            {
                SamplePoseAt(frameIndex + 1, ref transformBuffer);
            }
            else
            {
                Assert.IsTrue(transformBuffer.Length == this.numJoints);

                int numJoints = this.numJoints;

                for (int i = 1; i < numJoints; ++i)
                {
                    AffineTransform lhs = GetParentJointTransform(
                        i, frameIndex).inverseTimes(GetJointTransform(i, frameIndex));
                    AffineTransform rhs = GetParentJointTransform(
                        i, frameIndex + 1).inverseTimes(GetJointTransform(i, frameIndex + 1));

                    transformBuffer[i] = Missing.lerp(lhs, rhs, theta);
                }

                transformBuffer[0] = AffineTransform.identity;
            }
        }

        internal void SamplePoseAt(SamplingTime samplingTime, MemoryArray<AffineTransform> transformBuffer)
        {
            var frameIndex = GetFrameIndex(samplingTime.timeIndex);

            float theta = math.saturate(samplingTime.theta);

            var numFramesMinusOne = GetSegment(
                samplingTime.segmentIndex).destination.NumFrames - 1;

            if (samplingTime.frameIndex >= numFramesMinusOne)
            {
                SamplePoseAt(frameIndex, transformBuffer);
            }
            else if (theta <= Missing.epsilon)
            {
                SamplePoseAt(frameIndex, transformBuffer);
            }
            else if (theta >= 1.0f - Missing.epsilon)
            {
                SamplePoseAt(frameIndex + 1, transformBuffer);
            }
            else
            {
                Assert.IsTrue(transformBuffer.Length == this.numJoints);

                int numJoints = this.numJoints;

                for (int i = 1; i < numJoints; ++i)
                {
                    AffineTransform lhs = GetParentJointTransform(
                        i, frameIndex).inverseTimes(GetJointTransform(i, frameIndex));
                    AffineTransform rhs = GetParentJointTransform(
                        i, frameIndex + 1).inverseTimes(GetJointTransform(i, frameIndex + 1));

                    transformBuffer[i] = Missing.lerp(lhs, rhs, theta);
                }

                transformBuffer[0] = AffineTransform.identity;
            }
        }

        /// <summary>
        /// Debug visualization for trajectories.
        /// </summary>
        /// <param name="referenceTransform">A world space transform that determines the anchor that is to be used for the trajectory visualization.</param>
        /// <param name="poseIndex">A pose index that determines where the trajectory should start.</param>
        /// <param name="numFrames">A duration in frames that determines the length of the trajectory.</param>
        /// <param name="color">The color that the trajectory should be drawn with.</param>
        public void DebugDrawTrajectory(AffineTransform referenceTransform, int poseIndex, int numFrames, Color color)
        {
            AffineTransform previousTransform = GetTrajectoryTransform(poseIndex);

            for (int i = 1; i < numFrames; ++i)
            {
                AffineTransform currentTransform = GetTrajectoryTransform(poseIndex + i);

                float3 previousPosition = referenceTransform.t;

                referenceTransform *=
                    previousTransform.inverseTimes(currentTransform);

                Debug.DrawLine(previousPosition, referenceTransform.t, color);

                previousTransform = currentTransform;
            }
        }

        /// <summary>
        /// Debug visualization for trajectories.
        /// </summary>
        /// <param name="referenceTransform">A world space transform that determines the anchor that is to be used for the trajectory visualization.</param>
        /// <param name="sampleTimeInSeconds">A time in seconds that determines where the trajectory should start.</param>
        /// <param name="duration">A duration in seconds that determines the length of the trajectory.</param>
        /// <param name="color">The color that the trajectory should be drawn with.</param>
        public AffineTransform DebugDrawTrajectory(AffineTransform referenceTransform, float sampleTimeInSeconds, float duration, Color color)
        {
            AffineTransform previousTransform =
                GetTrajectoryTransform(sampleTimeInSeconds);

            float deltaTime = 1.0f / SampleRate;

            while (duration > 0.0f)
            {
                float timeInSecondsToConsume = math.min(deltaTime, duration);

                sampleTimeInSeconds += timeInSecondsToConsume;

                duration -= timeInSecondsToConsume;

                AffineTransform currentTransform = GetTrajectoryTransform(sampleTimeInSeconds);

                float3 previousPosition = referenceTransform.t;

                referenceTransform *=
                    previousTransform.inverseTimes(currentTransform);

                Debug.DrawLine(previousPosition, referenceTransform.t, color);

                previousTransform = currentTransform;
            }

            return referenceTransform;
        }

        /// <summary>
        /// Debug visualization for trajectories.
        /// </summary>
        /// <param name="anchorTransform">A world space transform that determines the anchor that is to be used for the trajectory visualization.</param>
        /// <param name="samplingTime">A sampling time that determines where the trajectory should start.</param>
        /// <param name="timeHorizon">A duration in seconds that determines the length of the trajectory.</param>
        /// <param name="color">The color that the trajectory should be drawn with.</param>
        public void DebugDrawTrajectory(AffineTransform anchorTransform, SamplingTime samplingTime, float timeHorizon, Color color)
        {
            int size = (int)math.ceil(timeHorizon * SampleRate) * 2 + 1;
            NativeArray<AffineTransform> trajectory = new NativeArray<AffineTransform>(size, Allocator.Temp);

            var offsetTime = -timeHorizon;

            float deltaTime = math.rcp(SampleRate);

            AffineTransform referenceTransform =
                GetTrajectoryTransform(samplingTime);

            AffineTransform previousRootTransform =
                referenceTransform.inverseTimes(
                    GetTrajectoryTransform(
                        Advance(samplingTime, offsetTime)));

            int index = 0;
            trajectory[index++] = previousRootTransform;

            while (offsetTime < timeHorizon)
            {
                offsetTime =
                    math.min(timeHorizon,
                        offsetTime + deltaTime);

                AffineTransform rootTransform =
                    referenceTransform.inverseTimes(
                        GetTrajectoryTransform(
                            Advance(samplingTime, offsetTime)));

                trajectory[index++] = rootTransform;
            }

            DebugExtensions.DebugDrawTrajectory(anchorTransform, trajectory, SampleRate, color, color, true);

            trajectory.Dispose();
        }

        internal void DebugDrawPyramid(AffineTransform transform, float3 scale, Color color)
        {
            void DrawLine(float3 a, float3 b)
            {
                Debug.DrawLine(
                    transform.transform(a * scale),
                    transform.transform(b * scale), color);
            }

            // Edges to the top of pyramid
            DrawLine(new float3(0.0f, 0.0f, 0.0f), new float3(1.0f, 1.0f, 1.0f));
            DrawLine(new float3(0.0f, 0.0f, 0.0f), new float3(-1.0f, 1.0f, 1.0f));
            DrawLine(new float3(0.0f, 0.0f, 0.0f), new float3(-1.0f, -1.0f, 1.0f));
            DrawLine(new float3(0.0f, 0.0f, 0.0f), new float3(1.0f, -1.0f, 1.0f));

            // Bottom of pyramid
            DrawLine(new float3(1.0f, 1.0f, 1.0f), new float3(1.0f, -1.0f, 1.0f));
            DrawLine(new float3(1.0f, -1.0f, 1.0f), new float3(-1.0f, -1.0f, 1.0f));
            DrawLine(new float3(-1.0f, -1.0f, 1.0f), new float3(-1.0f, 1.0f, 1.0f));
            DrawLine(new float3(-1.0f, 1.0f, 1.0f), new float3(1.0f, 1.0f, 1.0f));
        }

        internal void DebugDrawBone(AffineTransform transform, float3 p, Color color)
        {
            float3 d = math.normalize(p - transform.t);

            quaternion q = Missing.forRotation(new float3(0.0f, 0.0f, 1.0f), d);

            float3 test = Missing.rotateVector(q, new float3(0.0f, 0.0f, 1.0f));

            float length = math.length(p - transform.t);
            float width = length / 20.0f;

            DebugDrawPyramid(new AffineTransform(transform.t, q), new float3(width, width, length * 0.8f), color);
            DebugDrawPyramid(new AffineTransform(p, q), new float3(width, width, -length * 0.2f), color);
        }

        /// <summary>
        /// Debug visualization for transforms, i.e. a position and rotation.
        /// </summary>
        /// <param name="transform">The transforms that should be displayed.</param>
        /// <param name="scale">A scale that determines the length of the rotation axis of the transform.</param>
        /// <param name="alpha">An color alpha value that is to be used for the rotation axis display.</param>
        public static void DebugDrawTransform(AffineTransform transform, float scale, float alpha = 1.0f)
        {
            Debug.DrawLine(transform.t, transform.t + Missing.xaxis(transform.q) * scale, new Color(1.0f, 0.0f, 0.0f, alpha));
            Debug.DrawLine(transform.t, transform.t + Missing.yaxis(transform.q) * scale, new Color(0.0f, 1.0f, 0.0f, alpha));
            Debug.DrawLine(transform.t, transform.t + Missing.zaxis(transform.q) * scale, new Color(0.0f, 0.0f, 1.0f, alpha));
        }

        internal void DebugDrawPoseWorldSpace(int poseIndex, Color color)
        {
            DebugDrawPoseWorldSpace(
                GetTrajectoryTransform(poseIndex),
                poseIndex, color);
        }

        internal void DebugDrawPoseWorldSpace(float sampleTimeInSeconds, Color color)
        {
            DebugDrawPoseWorldSpace(
                GetTrajectoryTransform(sampleTimeInSeconds),
                sampleTimeInSeconds, color);
        }

        internal void DebugDrawPoseWorldSpace(SamplingTime samplingTime, Color color)
        {
            DebugDrawPoseWorldSpace(
                GetTrajectoryTransform(samplingTime),
                samplingTime, color);
        }

        /// <summary>
        /// Debug visualization for animation poses.
        /// </summary>
        /// <param name="rootTransform">A world space transform that determines the position and orientation of the animation pose.</param>
        /// <param name="poseIndex">The pose index for which the root transform is to be retrieved.</param>
        /// <param name="color">The color that the pose should be drawn with.</param>
        public void DebugDrawPoseWorldSpace(AffineTransform rootTransform, int poseIndex, Color color)
        {
            int numJoints = this.numJoints;
            for (int jointIndex = 1; jointIndex < numJoints; ++jointIndex)
            {
                int parentJointIndex = animationRig.GetParentJointIndex(jointIndex);
                Assert.IsTrue(parentJointIndex >= 0);
                if (parentJointIndex > 0)
                {
                    AffineTransform thisTransform = rootTransform * GetJointTransform(jointIndex, poseIndex);
                    AffineTransform parentTransform = rootTransform * GetJointTransform(parentJointIndex, poseIndex);

                    DebugDrawBone(thisTransform, parentTransform.t, color);

                    float length = math.length(thisTransform.t - parentTransform.t);

                    DebugDrawTransform(thisTransform, length * 0.1f);
                }
            }
        }

        /// <summary>
        /// Debug visualization for animation poses.
        /// </summary>
        /// <param name="rootTransform">A world space transform that determines the position and orientation of the animation pose.</param>
        /// <param name="sampleTimeInSeconds">A time in seconds that determines the animation pose.</param>
        /// <param name="color">The color that the pose should be drawn with.</param>
        public void DebugDrawPoseWorldSpace(AffineTransform rootTransform, float sampleTimeInSeconds, Color color)
        {
            int numJoints = this.numJoints;
            for (int jointIndex = 1; jointIndex < numJoints; ++jointIndex)
            {
                int parentJointIndex = animationRig.GetParentJointIndex(jointIndex);
                Assert.IsTrue(parentJointIndex >= 0);
                if (parentJointIndex > 0)
                {
                    AffineTransform thisTransform = rootTransform * GetJointTransform(jointIndex, sampleTimeInSeconds);
                    AffineTransform parentTransform = rootTransform * GetJointTransform(parentJointIndex, sampleTimeInSeconds);

                    DebugDrawBone(thisTransform, parentTransform.t, color);

                    //float length = math.length(thisTransform.t - parentTransform.t);

                    //DebugDrawTransform(thisTransform, length * 0.1f, color.a);
                }
            }
        }

        /// <summary>
        /// Debug visualization for animation poses.
        /// </summary>
        /// <param name="rootTransform">A world space transform that determines the position and orientation of the animation pose.</param>
        /// <param name="samplingTime">A sampling time that determines the animation pose.</param>
        /// <param name="color">The color that the pose should be drawn with.</param>
        public void DebugDrawPoseWorldSpace(AffineTransform rootTransform, SamplingTime samplingTime, Color color)
        {
            int numJoints = this.numJoints;
            for (int jointIndex = 1; jointIndex < numJoints; ++jointIndex)
            {
                int parentJointIndex = animationRig.GetParentJointIndex(jointIndex);
                Assert.IsTrue(parentJointIndex >= 0);
                if (parentJointIndex > 0)
                {
                    AffineTransform thisTransform = rootTransform * GetJointTransform(jointIndex, samplingTime);
                    AffineTransform parentTransform = rootTransform * GetJointTransform(parentJointIndex, samplingTime);

                    DebugDrawBone(thisTransform, parentTransform.t, color);

                    //float length = math.length(thisTransform.t - parentTransform.t);

                    //DebugDrawTransform(thisTransform, length * 0.1f, color.a);
                }
            }
        }
    }
}
