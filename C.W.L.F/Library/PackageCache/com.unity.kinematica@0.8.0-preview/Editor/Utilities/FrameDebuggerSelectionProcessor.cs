using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal class FrameDebuggerSelectionProcessor : IFrameDebuggerSelectionProcessor
    {
        DebugDrawOptions drawOptions = new DebugDrawOptions();

        public FrameDebuggerSelectionProcessor()
        {
            drawOptions = DebugDrawOptions.Create();
        }

        public void UpdateSelection()
        {
            for (int i = 0; i < Debugger.frameDebugger.NumSelected; ++i)
            {
                SelectedFrameDebugProvider selected = Debugger.frameDebugger.GetSelected(i);

                IMotionSynthesizerProvider synthesizerProvider = selected.providerInfo.provider as IMotionSynthesizerProvider;
                if (synthesizerProvider == null || !synthesizerProvider.IsSynthesizerInitialized)
                {
                    continue;
                }

                ref MotionSynthesizer synthesizer = ref synthesizerProvider.Synthesizer.Ref;

                DebugMemory debugMemory = synthesizer.ReadDebugMemory;

                if (selected.metadata != null)
                {
                    DebugIdentifier identifier = (DebugIdentifier)selected.metadata;
                    if (debugMemory.FindObjectReference(identifier).IsValid)
                    {
                        break;
                    }
                    else
                    {
                        selected.metadata = null;
                    }
                }

                for (DebugReference reference = debugMemory.FirstOrDefault; reference.IsValid; reference = debugMemory.Next(reference))
                {
                    if (!DataTypes.IsValidType(reference.identifier.typeHashCode))
                    {
                        continue;
                    }

                    Type debugType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item1;
                    if (typeof(IMotionMatchingQuery).IsAssignableFrom(debugType))
                    {
                        selected.metadata = reference.identifier;
                        break;
                    }
                }

                Debugger.frameDebugger.TrySelect(selected.providerInfo.provider, selected.metadata);
            }
        }

        public void DrawSelection(Camera camera)
        {
            if (drawOptions.textWindowIdentifier < 0)
            {
                drawOptions.textWindowIdentifier = DebugDraw.CreateMovableText(new float2(Screen.width * 0.2f, 5.0f));
            }

            DebugDraw.ClearMovableText(drawOptions.textWindowIdentifier);

            DebugDraw.Begin(camera);

            DebugDraw.SetDepthRendering(false);

            foreach (SelectedFrameDebugProvider selected in Debugger.frameDebugger.Selection)
            {
                if (selected.metadata == null)
                {
                    continue;
                }

                IMotionSynthesizerProvider synthesizerProvider = selected.providerInfo.provider as IMotionSynthesizerProvider;
                if (synthesizerProvider == null || !synthesizerProvider.IsSynthesizerInitialized)
                {
                    continue;
                }

                ref MotionSynthesizer synthesizer = ref synthesizerProvider.Synthesizer.Ref;

                DebugMemory debugMemory = synthesizer.ReadDebugMemory;

                DebugIdentifier identifier = (DebugIdentifier)selected.metadata;
                DebugReference reference = debugMemory.FindObjectReference(identifier);
                if (!reference.IsValid)
                {
                    continue;
                }

                SamplingTime debugSamplingTime = RetrieveDebugSamplingTime(ref synthesizer);

                object debugObject = debugMemory.ReadObjectGeneric(reference);

                if (debugObject is IDebugDrawable drawable)
                {
                    drawable.Draw(camera, ref synthesizer, debugMemory, debugSamplingTime, ref drawOptions);
                }

                if (debugObject is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            DebugDraw.End();
        }

        public void DrawInspector()
        {
            if (Debugger.frameDebugger.NumSelected == 0)
            {
                return;
            }

            GUILayout.Space(180);

            string[] drawElements = Enum.GetNames(typeof(DebugDrawFlags)).Select(s => ObjectNames.NicifyVariableName(s)).ToArray();

            drawOptions.drawFlags = (DebugDrawFlags)EditorGUILayout.MaskField("Options", (int)drawOptions.drawFlags, drawElements, new GUILayoutOption[] { GUILayout.Width(400.0f) });

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            drawOptions.distanceOffset = EditorGUILayout.Slider(new GUIContent("Distance offset"), drawOptions.distanceOffset, 0.0f, 5.0f, new GUILayoutOption[] { GUILayout.Width(500.0f) });

            GUILayout.Space(80);

            drawOptions.timeOffset = EditorGUILayout.Slider(new GUIContent("Time offset"), drawOptions.timeOffset, -1.0f, 1.0f, new GUILayoutOption[] { GUILayout.Width(500.0f) });

            GUILayout.Space(80);

            DebugCostTimelineDrawer.enabled = EditorGUILayout.Toggle(new GUIContent("Display cost (experimental)"), DebugCostTimelineDrawer.enabled);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            bool foundQuery = false;

            for (int i = 0; i < Debugger.frameDebugger.NumSelected; ++i)
            {
                SelectedFrameDebugProvider selected = Debugger.frameDebugger.GetSelected(i);

                if (selected.metadata == null)
                {
                    continue;
                }

                IMotionSynthesizerProvider synthesizerProvider = selected.providerInfo.provider as IMotionSynthesizerProvider;
                if (synthesizerProvider == null)
                {
                    continue;
                }

                ref MotionSynthesizer synthesizer = ref synthesizerProvider.Synthesizer.Ref;


                DebugMemory debugMemory = synthesizer.ReadDebugMemory;
                for (DebugReference reference = debugMemory.FirstOrDefault; reference.IsValid; reference = debugMemory.Next(reference))
                {
                    if (!DataTypes.IsValidType(reference.identifier.typeHashCode))
                    {
                        continue;
                    }

                    Type debugType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item1;
                    if (typeof(IMotionMatchingQuery).IsAssignableFrom(debugType))
                    {
                        object debugObject = debugMemory.ReadObjectGeneric(reference);

                        IMotionMatchingQuery query = (IMotionMatchingQuery)debugObject;

                        if (selected.metadata != null && ((DebugIdentifier)selected.metadata).Equals(reference.identifier))
                        {
                            GUI.enabled = false;
                        }

                        if (GUILayout.Button(new GUIContent(query.DebugTitle)))
                        {
                            selected.metadata = reference.identifier;
                            Debugger.frameDebugger.TrySelect(selected.providerInfo.provider, selected.metadata);
                        }

                        foundQuery = true;

                        GUI.enabled = true;
                    }
                }

                break;
            }

            if (!foundQuery)
            {
                GUI.enabled = false;
                GUILayout.Button(new GUIContent("No motion matching query"));
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
        }

        SamplingTime RetrieveDebugSamplingTime(ref MotionSynthesizer synthesizer)
        {
            var builderWindow = Utility.FindBuilderWindow();

            if (builderWindow != null)
            {
                return SamplingTime.Create(builderWindow.RetrieveDebugTimeIndex(ref synthesizer.Binary));
            }

            return SamplingTime.Invalid;
        }
    }
}
