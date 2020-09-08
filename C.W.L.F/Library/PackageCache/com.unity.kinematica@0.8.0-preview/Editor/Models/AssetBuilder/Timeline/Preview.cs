using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Kinematica.Editor
{
    internal class Preview : IDisposable
    {
        AnimationRig targetRig;
        float timeHorizon;
        float assetSampleRate;
        Trajectory sampleTrajectory;
        Color trajectoryColor;

        // Preview game object
        Transform[] targetJoints;
        GameObject targetObject;
        List<EditorCurveBinding> bindings;

        // Clip sampling
        TaggedAnimationClip clip;
        AssetPostprocessCallbacks clipAssetCallbacks;
        Avatar sourceAvatar;
        AnimationSampler sampler;

        // Preview cache data
        float previousTime;
        AffineTransform previousRootTransform;

        public static Preview CreatePreview(Asset asset, GameObject previewTarget)
        {
            var preview = new Preview(asset);

            try
            {
                preview.SetPreviewTarget(previewTarget);
            }
            catch (Exception e)
            {
                preview.Dispose();
                throw e;
            }

            return preview;
        }

        Preview(Asset asset)
        {
            targetRig = AnimationRig.Create(asset.DestinationAvatar);

            int trajectoryLength = Missing.truncToInt(2.0f * asset.TimeHorizon * asset.SampleRate) + 1;
            sampleTrajectory = Trajectory.Create(trajectoryLength, Allocator.Persistent);
            timeHorizon = asset.TimeHorizon;
            assetSampleRate = asset.SampleRate;

            trajectoryColor = Binary.TrajectoryFragmentDisplay.CreateOptions().baseColor;
        }

        public void Dispose()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            ResetAnimationSampler();

            sampleTrajectory.Dispose();
        }

        public void EnableDisplayTrajectory()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public void DisableDisplayTrajectory()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            DebugDraw.Begin(sceneView.camera);
            DebugDraw.SetDepthRendering(false);

            DisplayTrajectory();

            DebugDraw.End();
        }

        void OnClipReimport()
        {
            TaggedAnimationClip importedClip = clip;
            float currentTime = previousTime;

            ResetAnimationSampler();
            if (!InitAnimationSampler(importedClip))
            {
                return;
            }

            if (currentTime >= 0.0f)
            {
                SamplePose(currentTime);
                EnableDisplayTrajectory();
            }
        }

        void OnClipDelete()
        {
            ResetAnimationSampler();
        }

        public void SetPreviewTarget(GameObject previewTarget)
        {
            ResetAnimationSampler();

            targetObject = previewTarget;

            try
            {
                targetJoints = targetRig.MapRigOnTransforms(targetObject.transform);
            }
            catch (Exception e)
            {
                throw new Exception($"Preview error : could not map preview GameObject {previewTarget.name} to avatar {targetRig.Avatar.name}, " + e.Message);
            }

            ComputeAnimatableBindings();

            previousTime = -1.0f;
        }

        void ComputeAnimatableBindings()
        {
            bindings = new List<EditorCurveBinding>();

            foreach (Transform t in targetJoints)
            {
                if (t == null)
                {
                    continue;
                }

                EditorCurveBinding[] jointBindings = AnimationUtility.GetAnimatableBindings(t.gameObject, targetObject);
                foreach (EditorCurveBinding binding in jointBindings)
                {
                    if (binding.propertyName.Contains("Local"))
                    {
                        bindings.Add(binding);
                    }
                }
            }
        }

        void SamplePose(float time)
        {
            if (sampler == null)
            {
                return;
            }

            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }

            AnimationMode.BeginSampling();

            try
            {
                // 1. Sample pose
                using (TransformBuffer.Memory pose = sampler.SamplePose(time))
                {
                    // 2. Save bindings so that we can restore the joints to their initial transform once preview is finished
                    foreach (EditorCurveBinding binding in bindings)
                    {
                        AnimationMode.AddEditorCurveBinding(targetObject, binding);
                    }

                    // 3. Sample trajectory
                    AffineTransform rootTransform = sampler.SampleTrajectory(time);
                    if (previousTime >= 0.0f && targetJoints[0] != null)
                    {
                        // If the character was previously animated, we apply delta trajectory to current transform.
                        // Otherwise, we don't move the character (this allows user to move manually previewed character between frames)
                        var currentRootTransform =
                            Missing.Convert(targetObject.transform) *
                            previousRootTransform.inverseTimes(rootTransform);

                        targetJoints[0].position = currentRootTransform.t;
                        targetJoints[0].rotation = currentRootTransform.q;
                    }

                    previousTime = time;
                    previousRootTransform = rootTransform;

                    // 4. Sample all joints
                    for (int i = 1; i < targetJoints.Length; ++i)
                    {
                        if (targetJoints[i] == null)
                        {
                            continue;
                        }

                        var localJointTransform = pose.Ref.transforms[i];

                        targetJoints[i].localPosition = localJointTransform.t;
                        targetJoints[i].localRotation = localJointTransform.q;
                    }

                    // 5. sample past & future trajectory
                    SampleTrajectory(time);
                }
            }
            finally
            {
                AnimationMode.EndSampling();
            }
        }

        void SampleTrajectory(float time)
        {
            int halfTrajectoryLength = sampleTrajectory.Length / 2;
            if (halfTrajectoryLength == 0)
            {
                sampleTrajectory[0] = AffineTransform.identity;
                return;
            }

            AffineTransform referenceTrajectory = sampler.SampleTrajectory(time);

            for (int i = 0; i < sampleTrajectory.Length; ++i)
            {
                float sampleTime = time + ((float)(i - halfTrajectoryLength) / halfTrajectoryLength) * timeHorizon;
                sampleTime = math.clamp(sampleTime, 0.0f, clip.DurationInSeconds);

                sampleTrajectory[i] = referenceTrajectory.inverseTimes(sampler.SampleTrajectory(sampleTime));
            }
        }

        public event Action PreviewInvalidated;

        void DisplayTrajectory()
        {
            try
            {
                AffineTransform worldRootTransform = AffineTransform.Create(targetJoints[0].position, targetJoints[0].rotation);
                DebugExtensions.DebugDrawTrajectory(worldRootTransform, sampleTrajectory, assetSampleRate, trajectoryColor, trajectoryColor);
            }
            catch (MissingReferenceException)
            {
                PreviewInvalidated?.Invoke();
            }
        }

        internal void PreviewTime(TaggedAnimationClip clip, float time)
        {
            if (targetObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                return;
            }

            if (this.clip != clip)
            {
                //TODO - do we need to call this when hte clip doesn't change but the target avatar does?
                if (!InitAnimationSampler(clip))
                {
                    return;
                }
            }

            SamplePose(time);
            EnableDisplayTrajectory();
        }

        public void PreviewTime(float time)
        {
            if (this.clip == null)
            {
                return;
            }

            SamplePose(time);
            EnableDisplayTrajectory();
        }

        void ResetAnimationSampler()
        {
            DisableDisplayTrajectory();

            clip = null;
            sourceAvatar = null;

            if (clipAssetCallbacks != null)
            {
                clipAssetCallbacks.Dispose();
                clipAssetCallbacks = null;
            }

            if (sampler != null)
            {
                sampler.Dispose();
                sampler = null;
            }

            previousTime = -1.0f;
        }

        bool InitAnimationSampler(TaggedAnimationClip clip)
        {
            this.clip = clip;

            if (clipAssetCallbacks != null)
            {
                clipAssetCallbacks.Dispose();
            }

            AnimationClip animationClip = clip.GetOrLoadClipSync();

            clipAssetCallbacks = new AssetPostprocessCallbacks(animationClip);
            clipAssetCallbacks.importDelegate = OnClipReimport;
            clipAssetCallbacks.deleteDelegate = OnClipDelete;

            sourceAvatar = clip.RetargetSourceAvatar;

            if (sampler != null)
            {
                sampler.Dispose();
                sampler = null;
            }

            try
            {
                sampler = new AnimationSampler(targetRig, animationClip);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }

            previousTime = -1.0f;

            return true;
        }
    }
}
