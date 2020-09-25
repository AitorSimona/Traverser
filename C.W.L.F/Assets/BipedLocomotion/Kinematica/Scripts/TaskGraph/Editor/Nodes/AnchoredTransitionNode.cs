using Unity.Kinematica;
using Unity.Kinematica.Editor;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Assertions;

using SegmentIndex = Unity.Kinematica.Binary.SegmentIndex;
using MarkerIndex = Unity.Kinematica.Binary.MarkerIndex;
using TypeIndex = Unity.Kinematica.Binary.TypeIndex;

[GraphNode(typeof(AnchoredTransitionTask))]
public class AnchoredTransitionNode : GraphNode
{
    float timeOffset;
    Color currentColor;
    float candidatePercentage;
    bool displaySourceCandidates;
    bool displayTargetCandidates;

    public AnchoredTransitionNode()
    {
        timeOffset = 0.0f;
        currentColor = Color.green;
    }

    public override void OnSelected(ref MotionSynthesizer synthesizer)
    {
        base.OnSelected();

        ref var binary = ref synthesizer.Binary;

        var worldRootTransform = synthesizer.WorldRootTransform;

        AnchoredTransitionTask task = GetDebugObject<AnchoredTransitionTask>();


        var samplingTime = task.samplingTime;

        binary.DebugDrawTrajectory(worldRootTransform,
            samplingTime, binary.TimeHorizon, currentColor);

        DisplayPoseAtOffset(ref binary,
            worldRootTransform, samplingTime,
                timeOffset, currentColor);

        if (displaySourceCandidates)
        {
            DisplaySourceCandidates(ref binary, worldRootTransform, samplingTime);
        }

        var sequences = task.poses.sequences;

        if (sequences.Length > 0)
        {
            var candidateIndex = Missing.truncToInt(
                (sequences.Length - 1) * candidatePercentage);

            var intervalIndex = sequences[candidateIndex].intervalIndex;

            ref var interval = ref binary.GetInterval(intervalIndex);

            DebugDrawPoseAndTrajectory(ref binary,
                interval.segmentIndex, task.contactTransform, timeOffset);

            if (displayTargetCandidates)
            {
                DisplayTargetCandidates(ref binary,
                    interval.segmentIndex, task.contactTransform);
            }
        }

        task.Dispose();
    }

    public override void DrawDefaultInspector()
    {
        HandleCandidate();
        HandleTimeOffset();
        HandlePoseColor();
        HandleShowSourceCandidates();
        HandleShowTargetCandidates();
    }

    public void HandleCandidate()
    {
        var sliderButton = new Slider();

        sliderButton.label = "Candidate";
        sliderButton.value = 0.0f;
        sliderButton.lowValue = 0.0f;
        sliderButton.highValue = 1.0f;

        controlsContainer.Add(sliderButton);

        sliderButton.RegisterValueChangedCallback((e) =>
        {
            candidatePercentage = e.newValue;
        });
    }

    public void HandleTimeOffset()
    {
        var sliderButton = new Slider();

        sliderButton.label = "Time Offset";
        sliderButton.value = 0.0f;
        sliderButton.lowValue = -1.0f;
        sliderButton.highValue = 1.0f;

        controlsContainer.Add(sliderButton);

        sliderButton.RegisterValueChangedCallback((e) =>
        {
            timeOffset = e.newValue;
        });
    }

    public void HandlePoseColor()
    {
        var colorField = new ColorField();

        colorField.label = "Current Color";
        colorField.value = currentColor;

        controlsContainer.Add(colorField);

        colorField.RegisterValueChangedCallback((e) =>
        {
            currentColor = e.newValue;
        });
    }

    public void HandleShowSourceCandidates()
    {
        var toggleButton = new Toggle();

        toggleButton.text = "Show Source Candidates";
        toggleButton.value = displaySourceCandidates;

        controlsContainer.Add(toggleButton);

        toggleButton.RegisterValueChangedCallback((e) =>
        {
            displaySourceCandidates = e.newValue;
        });
    }

    public void HandleShowTargetCandidates()
    {
        var toggleButton = new Toggle();

        toggleButton.text = "Show Target Candidates";
        toggleButton.value = displayTargetCandidates;

        controlsContainer.Add(toggleButton);

        toggleButton.RegisterValueChangedCallback((e) =>
        {
            displayTargetCandidates = e.newValue;
        });
    }

    void DisplaySourceCandidates(ref Binary binary, AffineTransform worldRootTransform, SamplingTime samplingTime)
    {
        float timeHorizon = binary.TimeHorizon;
        float inverseSampleRate = 1.0f / binary.SampleRate;

        int numCandidates = Missing.truncToInt(timeHorizon / inverseSampleRate);

        AffineTransform previousTrajectoryTransform =
            binary.GetTrajectoryTransform(samplingTime);

        Binary.DebugDrawTransform(worldRootTransform, 0.1f);

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

            Binary.DebugDrawTransform(worldRootTransform, 0.1f);
        }
    }

    void DisplayTargetCandidates(ref Binary binary, SegmentIndex segmentIndex, AffineTransform contactTransform)
    {
        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var anchorMarker = ref binary.GetMarker(anchorIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform worldRootTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        Binary.DebugDrawTransform(worldRootTransform, 0.1f);

        AffineTransform previousTrajectoryTransform =
            binary.GetTrajectoryTransform(firstFrame);

        for (int i = 1; i < anchorMarker.frameIndex; ++i)
        {
            AffineTransform currentTrajectoryTransform =
                binary.GetTrajectoryTransform(firstFrame + i);

            worldRootTransform *=
                previousTrajectoryTransform.inverseTimes(
                    currentTrajectoryTransform);

            previousTrajectoryTransform = currentTrajectoryTransform;

            Binary.DebugDrawTransform(worldRootTransform, 0.1f);
        }
    }

    void DisplayPoseAtOffset(ref Binary binary, AffineTransform anchorTransform, SamplingTime samplingTime, float offsetInSeconds, Color color)
    {
        var referenceTransform =
            binary.GetTrajectoryTransform(samplingTime);

        var currentSamplingTime = binary.Advance(samplingTime, offsetInSeconds);

        var deltaTransform = referenceTransform.inverseTimes(
            binary.GetTrajectoryTransform(currentSamplingTime));

        var rootTransform = anchorTransform * deltaTransform;

        binary.DebugDrawPoseWorldSpace(
            rootTransform, currentSamplingTime.samplingTime, color);

        Binary.DebugDrawTransform(rootTransform, 0.2f, color.a);
    }

    public static void DebugDrawPoseAndTrajectory(ref Binary binary, SegmentIndex segmentIndex, AffineTransform contactTransform, float timeOffset)
    {
        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var anchorMarker = ref binary.GetMarker(anchorIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        int poseIndex = math.max(0,
            Missing.truncToInt(anchorMarker.frameIndex * timeOffset));

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform referenceTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        binary.DebugDrawTrajectory(referenceTransform,
            firstFrame, segment.destination.numFrames, Color.yellow);

        referenceTransform *=
            binary.GetTrajectoryTransformBetween(
                firstFrame, poseIndex);

        binary.DebugDrawPoseWorldSpace(referenceTransform,
            firstFrame + poseIndex, Color.magenta);

        Binary.DebugDrawTransform(referenceTransform, 0.2f);
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
}
