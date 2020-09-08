using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.SnapshotDebugger;
using System;
using Buffer = Unity.SnapshotDebugger.Buffer;
using Unity.Burst;

namespace Unity.Kinematica
{
    [Data("Find Closest Pose & Trajectory")]
    internal struct FindClosestPoseTrajectoryDebug : IDebugObject, Serializable, IDebugDrawable, IMotionMatchingQuery
    {
        public DebugIdentifier debugIdentifier { get; set; }

        public string DebugTitle => trajectory.IsValid ? $"{debugName.ToString()} (pose & trajectory)" : $"Match {debugName.ToString()} (pose only)";

        public NativeString64 DebugName => debugName;

        [Input("Trajectory")]
        public DebugIdentifier trajectory;

        [Input("Candidates")]
        public DebugIdentifier candidates;

        [Input("Sampling Time")]
        public DebugIdentifier samplingTime;

        [Output("Closest Match")]
        public DebugIdentifier closestMatch;

        public DebugIdentifier deviationTable;

        public float trajectoryWeight;

        public float maxDeviation;

        public bool deviated;

        public NativeString64 debugName;

        string DebugWindowTitle => trajectoryWeight >= 0.0f ? $"{debugName.ToString()}, trajectory weight:{trajectoryWeight:0.00}" : debugName.ToString();

        static internal SamplingTime GetSamplingTime(DebugMemory debugMemory, DebugIdentifier debugIdentifier)
        {
            return debugIdentifier.IsValid ? debugMemory.ReadObjectFromIdentifier<SamplingTime>(debugIdentifier) : SamplingTime.Invalid;
        }

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            int fragmentSlot = 0;

            DebugDraw.SetMovableTextTitle(options.textWindowIdentifier, DebugWindowTitle);

            DeviationTable deviationTable = this.deviationTable.IsValid ? debugMemory.ReadObjectFromIdentifier<DeviationTable>(this.deviationTable) : DeviationTable.CreateInvalid();

            SamplingTime samplingTime = GetSamplingTime(debugMemory, this.samplingTime);
            if (samplingTime.IsValid)
            {
                if ((options.drawFlags & DebugDrawFlags.InputFragment) > 0)
                {
                    DebugDraw.DrawFragment(ref synthesizer, samplingTime, options.inputOutputFragmentColor, options.timeOffset, synthesizer.WorldRootTransform);
                    ++fragmentSlot;
                }
                DebugDraw.AddMovableTextLine(options.textWindowIdentifier, GetFragmentDebugText(ref synthesizer, "Input Clip", samplingTime, options.timeOffset, ref deviationTable), options.inputOutputFragTextColor);
            }

            SamplingTime closestMatch = GetSamplingTime(debugMemory, this.closestMatch);
            DebugIdentifier outputTimeIdentifier = this.closestMatch;
            if (closestMatch.IsValid)
            {
                if ((options.drawFlags & DebugDrawFlags.BestFragment) > 0)
                {
                    DebugDraw.DrawFragment(ref synthesizer, closestMatch, options.bestFragmentColor, options.timeOffset, GetAnchor(synthesizer.WorldRootTransform, fragmentSlot++ *options.distanceOffset, camera));

                    DebugDraw.AddMovableTextLine(options.textWindowIdentifier, GetFragmentDebugText(ref synthesizer, "Best Match Fragment", closestMatch, options.timeOffset, ref deviationTable), options.bestFragTextColor);
                    if (maxDeviation >= 0.0f)
                    {
                        string deviationMsg = GetDeviationText();
                        DebugDraw.AddMovableTextLine(options.textWindowIdentifier, deviationMsg, options.bestFragTextColor);
                    }

                    Nullable<TrajectoryHeuristicDebug> trajectoryHeuristic = FindTrajectoryHeuristic(debugMemory);
                    if (trajectoryHeuristic.HasValue)
                    {
                        trajectoryHeuristic.Value.DrawDebugText(ref options);

                        outputTimeIdentifier = trajectoryHeuristic.Value.outputTime;
                    }
                }
            }

            SamplingTime outputTime = GetOutputPlayTime(debugMemory, samplingTime, closestMatch, outputTimeIdentifier);
            if ((options.drawFlags & DebugDrawFlags.OutputFragment) > 0)
            {
                DebugDraw.DrawFragment(ref synthesizer, outputTime, options.inputOutputFragmentColor, options.timeOffset, GetAnchor(synthesizer.WorldRootTransform, fragmentSlot++ *options.distanceOffset, camera));
                DebugDraw.AddMovableTextLine(options.textWindowIdentifier, GetFragmentDebugText(ref synthesizer, "Output Clip", outputTime, options.timeOffset, ref deviationTable, false), options.inputOutputFragTextColor);
            }

            if (debugSamplingTime.IsValid)
            {
                if ((options.drawFlags & DebugDrawFlags.SelectedFragment) > 0)
                {
                    int slot = fragmentSlot > 0 ? -1 : 0;
                    DebugDraw.DrawFragment(ref synthesizer, debugSamplingTime, options.selectedFragmentColor, options.timeOffset, GetAnchor(synthesizer.WorldRootTransform, slot * options.distanceOffset, camera));
                    DebugDraw.AddMovableTextLine(options.textWindowIdentifier, GetFragmentDebugText(ref synthesizer, "Selected Clip", debugSamplingTime, options.timeOffset, ref deviationTable), options.selectedFragTextColor);
                }
            }

            deviationTable.Dispose();

            if ((options.drawFlags & DebugDrawFlags.Trajectory) > 0 && this.trajectory.IsValid)
            {
                using (Trajectory trajectory = debugMemory.ReadObjectFromIdentifier<Trajectory>(this.trajectory))
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

        AffineTransform GetAnchor(AffineTransform rootTransform, float distance, Camera camera)
        {
            return AffineTransform.Create(rootTransform.t + (float3)camera.transform.right * distance, rootTransform.q);
        }

        internal string GetFragmentDebugText(ref MotionSynthesizer synthesizer, string fragmentName, SamplingTime time, float timeOffset, ref DeviationTable deviationTable, bool addCost = true)
        {
            Assert.IsTrue(time.IsValid);

            string text = synthesizer.Binary.GetFragmentDebugText(time, fragmentName, timeOffset);

            if (!addCost)
            {
                return text;
            }


            string costInfo = GetCostInformation(ref synthesizer.Binary, ref deviationTable, time);
            if (!string.IsNullOrEmpty(costInfo))
            {
                text  = text + costInfo;
            }

            return text;
        }

        internal string GetDeviationText()
        {
            return deviated ? $"Cost bigger than max allowed {maxDeviation:0.000}, fragment discarded" : $"Cost smaller than max allowed {maxDeviation:0.000}, fragment picked";
        }

        string GetCostInformation(ref Binary binary, ref DeviationTable deviationTable, SamplingTime samplingTime)
        {
            if (!deviationTable.IsValid || !samplingTime.IsValid)
            {
                return "";
            }

            DeviationScore deviation = deviationTable.GetDeviation(ref binary, samplingTime.timeIndex);
            if (!deviation.IsValid)
            {
                return $"\nThis fragment wasn't considered during the query";
            }

            if (deviation.trajectoryDeviation >= 0.0f)
            {
                return $"\n<b>Pose cost:</b> {deviation.poseDeviation:0.000}, <b>Trajectory:</b> {deviation.trajectoryDeviation:0.000}, <b>Total:</b> {deviation.poseDeviation + deviation.trajectoryDeviation:0.000}";
            }
            else
            {
                return $"\n<b>Pose cost:</b> {deviation.poseDeviation:0.000}";
            }
        }

        Nullable<TrajectoryHeuristicDebug> FindTrajectoryHeuristic(DebugMemory debugMemory)
        {
            if (!trajectory.IsValid)
            {
                return null;
            }

            int heuristicTypeCode = BurstRuntime.GetHashCode32<TrajectoryHeuristicDebug>();

            for (DebugReference reference = debugMemory.FirstOrDefault; reference.IsValid; reference = debugMemory.Next(reference))
            {
                if (reference.identifier.typeHashCode != heuristicTypeCode)
                {
                    continue;
                }

                TrajectoryHeuristicDebug heuristicDebug = debugMemory.ReadObject<TrajectoryHeuristicDebug>(reference);
                if (heuristicDebug.closestMatch.Equals(closestMatch))
                {
                    return heuristicDebug;
                }
            }

            return null;
        }

        SamplingTime GetOutputPlayTime(DebugMemory debugMemory, SamplingTime samplingTime, SamplingTime closestMatch, DebugIdentifier outputTimeIdentifier)
        {
            if (!closestMatch.IsValid)
            {
                return samplingTime;
            }

            int playAtTimeTypeCode = BurstRuntime.GetHashCode32<PlayAtTimeDebug>();

            for (DebugReference reference = debugMemory.FirstOrDefault; reference.IsValid; reference = debugMemory.Next(reference))
            {
                if (reference.identifier.typeHashCode != playAtTimeTypeCode)
                {
                    continue;
                }

                PlayAtTimeDebug playAtTime = debugMemory.ReadObject<PlayAtTimeDebug>(reference);
                if (playAtTime.playTime.Equals(outputTimeIdentifier))
                {
                    return closestMatch;
                }
            }

            return samplingTime;
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.WriteBlittable(trajectory);
            buffer.WriteBlittable(candidates);
            buffer.WriteBlittable(samplingTime);
            buffer.WriteBlittable(closestMatch);
            buffer.WriteBlittable(deviationTable);
            buffer.Write(trajectoryWeight);
            buffer.Write(maxDeviation);
            buffer.Write(deviated);
            buffer.Write(debugName);
        }

        public void ReadFromStream(Buffer buffer)
        {
            trajectory = buffer.ReadBlittable<DebugIdentifier>();
            candidates = buffer.ReadBlittable<DebugIdentifier>();
            samplingTime = buffer.ReadBlittable<DebugIdentifier>();
            closestMatch = buffer.ReadBlittable<DebugIdentifier>();
            deviationTable = buffer.ReadBlittable<DebugIdentifier>();
            trajectoryWeight = buffer.ReadSingle();
            maxDeviation = buffer.ReadSingle();
            deviated = buffer.ReadBoolean();
            debugName = buffer.ReadNativeString64();
        }
    }
}
