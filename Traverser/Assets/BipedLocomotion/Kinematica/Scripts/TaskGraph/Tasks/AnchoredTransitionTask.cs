using Unity.Burst;
using Unity.Collections;
using Unity.Kinematica;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Assertions;

using SegmentIndex = Unity.Kinematica.Binary.SegmentIndex;
using MarkerIndex = Unity.Kinematica.Binary.MarkerIndex;
using TypeIndex = Unity.Kinematica.Binary.TypeIndex;
using Unity.Jobs;
using Unity.SnapshotDebugger;

[BurstCompile(CompileSynchronously = true)]
public struct AnchoredTransitionJob : IJob
{
    public static JobHandle Schedule(ref AnchoredTransitionTask transition)
    {
        var job = new AnchoredTransitionJob()
        {
            anchoredTransition = MemoryRef<AnchoredTransitionTask>.Create(ref transition)
        };

        return job.Schedule();
    }

    MemoryRef<AnchoredTransitionTask> anchoredTransition;


    public void Execute()
    {
        anchoredTransition.Ref.Execute();
    }
}

[Data("AnchoredTransitionTask", "#2A3756")]
public struct AnchoredTransitionTask : System.IDisposable, IDebugObject, Serializable
{
    public MemoryRef<MotionSynthesizer> synthesizer;

    public DebugIdentifier debugIdentifier{ get; set; }

    [Input("Candidates")]
    public DebugIdentifier inputPoses;

    [Output("Transition  time")]
    public DebugIdentifier outputTime;

    public PoseSet poses;



    NativeArray<PoseSequence> sequences => poses.sequences;

    public SamplingTime samplingTime;

    public AffineTransform contactTransform;

    public float maximumLinearError;
    public float maximumAngularError;

    public float timeInSecondsSoFar;
    public float timeInSecondsTotal;
    public float timeInSecondsUntilContact;

    public AffineTransform sourceRootTransform;
    public AffineTransform targetRootTransform;

    public TimeIndex sourceTimeIndex;
    public TimeIndex targetTimeIndex;

    public BlittableBool rootAdjust;
    public bool isValid;

    public static AnchoredTransitionTask Invalid => new AnchoredTransitionTask()
    {
        isValid = false
    };

    public enum State
    {
        Initializing,
        Waiting,
        Active,
        Complete,
        Failed,
    }

    public State state;

    public void SetState(State state)
    {
        this.state = state;
    }

    public bool IsState(State state)
    {
        return this.state == state;
    }

    public bool IsComplete()
    {
        return IsState(State.Complete);
    }

    public bool Execute()
    {
        ref var synthesizer = ref this.synthesizer.Ref;



        if (IsState(State.Initializing))
        {
            samplingTime = synthesizer.Time;

            if (FindTransition(samplingTime))
            {
                if (!rootAdjust)
                {
                    timeInSecondsTotal = -1.0f;

                    sourceTimeIndex = synthesizer.Time.timeIndex;
                    sourceRootTransform = synthesizer.WorldRootTransform;
                }

                SetState(State.Waiting);
            }
            else
            {
                SetState(State.Failed);

                return false;
            }
        }

        if (!IsState(State.Complete) && !IsState(State.Failed))
        {
            //
            // Do we still have time left before we make contact?
            //

            var deltaTime = synthesizer.deltaTime;

            if (timeInSecondsSoFar <= timeInSecondsTotal)
            {
                if (timeInSecondsSoFar + deltaTime >= timeInSecondsTotal)
                {
                    float remainingTimeInSeconds =
                        timeInSecondsTotal - timeInSecondsSoFar;
                    Assert.IsTrue(remainingTimeInSeconds <= deltaTime);
                }

                //
                // Calculate root movement such that we'll reach the contact transform
                //

                AffineTransform desiredDeltaTransform =
                    GetTrajectoryDeltaTransform(ref synthesizer, deltaTime);

                //
                // Calculate the root movement that the synthesizer will perform this frame.
                //

                AffineTransform actualDeltaTransform =
                    synthesizer.GetTrajectoryDeltaTransform(deltaTime);

                //
                // Move the character by the inverse of the actual delta movement
                // (such that the synthesizers' movement will have no effect) and perform
                // the desired root movement (such that we'll reach the contact transform).
                //

                AffineTransform deltaRootTransform =
                    actualDeltaTransform.inverse() * desiredDeltaTransform;

                synthesizer.AdjustTrajectory(deltaRootTransform);
            }

            timeInSecondsSoFar += deltaTime;
        }

        synthesizer.DebugPushGroup();
        outputTime = DebugIdentifier.Invalid;

        if (IsState(State.Waiting))
        {
            var timeIndex = synthesizer.Time.timeIndex;

            if (timeIndex.frameIndex >= sourceTimeIndex.frameIndex)
            {
                SamplingTime targetTime = SamplingTime.Create(targetTimeIndex);
                outputTime = synthesizer.DebugWriteBlittableObject(ref targetTime, true);

                synthesizer.PlayAtTime(targetTime);

                SetState(State.Active);
            }
        }

        if (IsState(State.Active))
        {
            var samplingTime = synthesizer.Time;

            var frameIndex = GetEscapeFrameIndex();

            if (samplingTime.timeIndex.frameIndex >= frameIndex)
            {
                SetState(State.Complete);
            }
        }

        
        synthesizer.DebugWriteUnblittableObject(ref poses);
        inputPoses = poses.debugIdentifier;

        synthesizer.DebugWriteUnblittableObject(ref this);

        return true;
    }

    AffineTransform GetTrajectoryDeltaTransform(ref MotionSynthesizer synthesizer, float deltaTime)
    {
        AffineTransform nextTrajectoryTransform =
            GetTransformAtTime(ref synthesizer,
                timeInSecondsSoFar + deltaTime);

        return synthesizer.WorldRootTransform.inverseTimes(nextTrajectoryTransform);
    }

    AffineTransform GetTransformAtTime(ref MotionSynthesizer synthesizer, float sampleTimeInSeconds)
    {
        float durationInSecondsUntilTransition =
            timeInSecondsTotal - timeInSecondsUntilContact;
        Assert.IsTrue(durationInSecondsUntilTransition >= 0.0f);

        ref Binary binary = ref synthesizer.Binary;

        float theta = math.clamp(
            sampleTimeInSeconds / timeInSecondsTotal, 0.0f, 1.0f);

        if (sampleTimeInSeconds <= durationInSecondsUntilTransition)
        {
            var deltaTimeInSeconds =
                -durationInSecondsUntilTransition + sampleTimeInSeconds;

            AffineTransform deltaTransform =
                binary.GetTrajectoryTransformBetween(
                    SamplingTime.Create(sourceTimeIndex),
                        deltaTimeInSeconds);

            AffineTransform currentSourceRootTransform =
                sourceRootTransform * deltaTransform;

            AffineTransform currentTargetRootTransform =
                targetRootTransform * deltaTransform;

            return Missing.lerp(currentSourceRootTransform,
                currentTargetRootTransform, theta);
        }
        else
        {
            var deltaTimeInSeconds =
                sampleTimeInSeconds - durationInSecondsUntilTransition;

            AffineTransform deltaTransform =
                binary.GetTrajectoryTransformBetween(
                    SamplingTime.Create(targetTimeIndex),
                        deltaTimeInSeconds);

            AffineTransform currentSourceRootTransform =
                sourceRootTransform * deltaTransform;

            AffineTransform currentTargetRootTransform =
                targetRootTransform * deltaTransform;

            return Missing.lerp(currentSourceRootTransform,
                currentTargetRootTransform, theta);
        }
    }

    int GetEscapeFrameIndex()
    {
        Assert.IsTrue(targetTimeIndex.IsValid);

        ref var synthesizer = ref this.synthesizer.Ref;

        ref var binary = ref synthesizer.Binary;

        var escapeTypeIndex = binary.GetTypeIndex<Escape>();

        var samplingTime = synthesizer.Time;

        var escapeIndex = GetMarkerOfType(
            ref binary, samplingTime.segmentIndex, escapeTypeIndex);

        if (escapeIndex.IsValid)
        {
            return binary.GetMarker(escapeIndex).frameIndex;
        }

        ref var segment = ref binary.GetSegment(samplingTime.segmentIndex);

        var numFrames = segment.destination.numFrames - 1;

        return numFrames;
    }

    bool FindTransition(SamplingTime samplingTime)
    {
        ref var synthesizer = ref this.synthesizer.Ref;

        ref var binary = ref synthesizer.Binary;

        float inverseSampleRate = 1.0f / binary.SampleRate;

        var sourceCandidates =
            CreateSourceCandidates(samplingTime);

        var numSourceCandidates = sourceCandidates.Length;

        var numCandidates = sequences.Length;

        var minimumPoseCost = float.MaxValue;

        for (int i=0; i<numCandidates; ++i)
        {
            var intervalIndex = sequences[i].intervalIndex;

            var segmentIndex =
                binary.GetInterval(
                    intervalIndex).segmentIndex;

            var targetCandidates =
                CreateTargetCandidates(segmentIndex);

            var numTargetCandidates = targetCandidates.Length;

            for (int j=0; j< numTargetCandidates; ++j)
            {
                var targetTransform = targetCandidates[j].worldRootTransform;

                var targetForward = Missing.zaxis(targetTransform.q);

                var foundTranition = false;

                for (int k=0; k<numSourceCandidates; ++k)
                {
                    var sourceTransform = sourceCandidates[k].worldRootTransform;

                    var distance = sourceTransform.t - targetTransform.t;
                    
                    float linearError = math.length(distance);

                    if (linearError <= maximumLinearError)
                    {
                        var sourceForward = Missing.zaxis(sourceTransform.q);
                        float dot = math.dot(targetForward, sourceForward);
                        float clampedDot = math.clamp(dot, -1.0f, 1.0f);
                        float angularError = math.acos(clampedDot);

                        if (angularError <= maximumAngularError)
                        {
                            var codeBookIndex =
                                binary.GetCodeBookAt(sourceCandidates[k].timeIndex);

                            var sourceFragment =
                                binary.ReconstructPoseFragment(
                                    SamplingTime.Create(sourceCandidates[k].timeIndex));

                            var currentPoseCost = 0.0f;

                            if (sourceFragment.IsValid)
                            {
                                ref var codeBook = ref binary.GetCodeBook(codeBookIndex);

                                var metricIndex = codeBook.metricIndex;

                                codeBook.poses.Normalize(sourceFragment.array);

                                var timeIndex = targetCandidates[j].timeIndex;

                                var targetFragment =
                                    binary.CreatePoseFragment(metricIndex,
                                        SamplingTime.Create(timeIndex));

                                codeBook.poses.Normalize(targetFragment.array);

                                currentPoseCost =
                                    codeBook.poses.FeatureDeviation(
                                        sourceFragment.array, targetFragment.array);

                                targetFragment.Dispose();

                                sourceFragment.Dispose();
                            }

                            if (currentPoseCost < minimumPoseCost)
                            {
                                minimumPoseCost = currentPoseCost;

                                sourceTimeIndex = sourceCandidates[k].timeIndex;
                                targetTimeIndex = targetCandidates[j].timeIndex;

                                sourceRootTransform = sourceTransform;
                                targetRootTransform = targetTransform;

                                int numFramesUntilTransition =
                                    sourceTimeIndex.frameIndex - samplingTime.frameIndex;

                                float timeInSecondsUntilTransition =
                                    numFramesUntilTransition * inverseSampleRate;

                                int numFramesUntilContact = numTargetCandidates - j;

                                timeInSecondsUntilContact =
                                    numFramesUntilContact * inverseSampleRate;

                                timeInSecondsTotal =
                                    timeInSecondsUntilTransition +
                                        timeInSecondsUntilContact;

                                timeInSecondsSoFar = 0.0f;
                            }

                            foundTranition = true;

                            break;
                        }
                    }
                }

                if (foundTranition)
                {
                    break;
                }
            }

            targetCandidates.Dispose();
        }

        sourceCandidates.Dispose();

        return sourceTimeIndex.IsValid;
    }

    public struct SourceCandidate
    {
        public AffineTransform worldRootTransform;
        public TimeIndex timeIndex;

        public static SourceCandidate Create(AffineTransform worldRootTransform, TimeIndex timeIndex)
        {
            return new SourceCandidate
            {
                worldRootTransform = worldRootTransform,
                timeIndex = timeIndex
            };
        }
    }

    public NativeArray<SourceCandidate> CreateSourceCandidates(SamplingTime samplingTime)
    {
        ref var synthesizer = ref this.synthesizer.Ref;

        ref var binary = ref synthesizer.Binary;

        float timeHorizon = binary.TimeHorizon;
        float inverseSampleRate = 1.0f / binary.SampleRate;

        int numCandidates = Missing.truncToInt(timeHorizon / inverseSampleRate);

        AffineTransform previousTrajectoryTransform =
            binary.GetTrajectoryTransform(samplingTime);

        var worldRootTransform = synthesizer.WorldRootTransform;

        var candidates =
            new NativeArray<SourceCandidate>(
                numCandidates, Allocator.Temp);

        candidates[0] = SourceCandidate.Create(
            worldRootTransform, samplingTime.timeIndex);

        for (int i = 1; i < numCandidates; ++i)
        {
            samplingTime = binary.Advance(
                samplingTime, inverseSampleRate).samplingTime;

            AffineTransform currentTrajectoryTransform =
                binary.GetTrajectoryTransform(samplingTime);

            worldRootTransform *=
                previousTrajectoryTransform.inverseTimes(
                    currentTrajectoryTransform);

            previousTrajectoryTransform = currentTrajectoryTransform;

            candidates[i] = SourceCandidate.Create(
                worldRootTransform, samplingTime.timeIndex);
        }

        return candidates;
    }

    public struct TargetCandidate
    {
        public AffineTransform worldRootTransform;
        public TimeIndex timeIndex;

        public static TargetCandidate Create(AffineTransform worldRootTransform, TimeIndex timeIndex)
        {
            return new TargetCandidate
            {
                worldRootTransform = worldRootTransform,
                timeIndex = timeIndex
            };
        }
    }

    public NativeArray<TargetCandidate> CreateTargetCandidates(SegmentIndex segmentIndex)
    {
        ref var synthesizer = ref this.synthesizer.Ref;

        ref var binary = ref synthesizer.Binary;

        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var contactTypeIndex = binary.GetTypeIndex<Contact>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var anchorMarker = ref binary.GetMarker(anchorIndex);

        var contactIndex = GetMarkerOfType(
            ref binary, segmentIndex, contactTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var contactMarker = ref binary.GetMarker(contactIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform worldRootTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        var candidates =
            new NativeArray<TargetCandidate>(
                contactMarker.frameIndex, Allocator.Temp);

        candidates[0] = TargetCandidate.Create(
            worldRootTransform, TimeIndex.Create(segmentIndex));

        AffineTransform previousTrajectoryTransform =
            binary.GetTrajectoryTransform(firstFrame);

        for (int i = 1; i < contactMarker.frameIndex; ++i)
        {
            AffineTransform currentTrajectoryTransform =
                binary.GetTrajectoryTransform(firstFrame + i);

            worldRootTransform *=
                previousTrajectoryTransform.inverseTimes(
                    currentTrajectoryTransform);

            previousTrajectoryTransform = currentTrajectoryTransform;

            candidates[i] = TargetCandidate.Create(
                worldRootTransform, TimeIndex.Create(segmentIndex, i));
        }

        return candidates;
    }

    public static MarkerIndex GetMarkerOfType(ref Binary binary, SegmentIndex segmentIndex, TypeIndex typeIndex)
    {
        ref var segment = ref binary.GetSegment(segmentIndex);

        var numMarkers = segment.numMarkers;

        for (int i = 0; i < numMarkers; ++i)
        {
            var markerIndex = segment.markerIndex + i;

            if (binary.IsType(markerIndex, typeIndex))
            {
                return markerIndex;
            }
        }

        return MarkerIndex.Invalid;
    }

    public static AnchoredTransitionTask Create(ref MotionSynthesizer synthesizer, PoseSet poses, AffineTransform contactTransform, float maximumLinearError, float maximumAngularError, bool rootAdjust = true)
    {
        return new AnchoredTransitionTask
        {
            synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer),
            poses = poses,
            contactTransform = contactTransform,
            maximumLinearError = maximumLinearError,
            maximumAngularError = maximumAngularError,
            sourceTimeIndex = TimeIndex.Invalid,
            targetTimeIndex = TimeIndex.Invalid,
            rootAdjust = rootAdjust,
            state = State.Initializing,
            isValid = true
        };
    }

    public void WriteToStream(Buffer buffer)
    {
        buffer.Write(isValid);
        if (!isValid)
        {
            return;
        }

        poses.WriteToStream(buffer);
        buffer.Write(samplingTime);
        buffer.Write(contactTransform);
        buffer.Write(maximumLinearError);
        buffer.Write(maximumAngularError);
        buffer.Write(timeInSecondsSoFar);
        buffer.Write(timeInSecondsTotal);
        buffer.Write(timeInSecondsUntilContact);
        buffer.Write(sourceRootTransform);
        buffer.Write(targetRootTransform);
        buffer.Write(sourceTimeIndex);
        buffer.Write(targetTimeIndex);
        buffer.WriteBlittable(rootAdjust);
        buffer.WriteBlittable(state);

        buffer.WriteBlittable(inputPoses);
        buffer.WriteBlittable(outputTime);
    }

    public void ReadFromStream(Buffer buffer)
    {
        isValid = buffer.ReadBoolean();
        if (!isValid)
        {
            return;
        }

        poses.ReadFromStream(buffer);
        samplingTime = buffer.ReadSamplingTime();
        contactTransform = buffer.ReadAffineTransform();
        maximumLinearError = buffer.ReadSingle();
        maximumAngularError = buffer.ReadSingle();
        timeInSecondsSoFar = buffer.ReadSingle();
        timeInSecondsTotal = buffer.ReadSingle();
        timeInSecondsUntilContact = buffer.ReadSingle();
        sourceRootTransform = buffer.ReadAffineTransform();
        targetRootTransform = buffer.ReadAffineTransform();
        sourceTimeIndex = buffer.ReadTimeIndex();
        targetTimeIndex = buffer.ReadTimeIndex();
        rootAdjust = buffer.ReadBlittable<BlittableBool>();
        state = buffer.ReadBlittable<State>();

        inputPoses = buffer.ReadBlittable<DebugIdentifier>();
        outputTime = buffer.ReadBlittable<DebugIdentifier>();
    }

    public void Dispose()
    {
        if (isValid)
        {
            poses.Dispose();
        }
    }
}
