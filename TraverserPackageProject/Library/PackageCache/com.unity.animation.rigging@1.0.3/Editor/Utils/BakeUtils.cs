using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// Utility class that groups bi-directional baking utilities.
    /// </summary>
    public static class BakeUtils
    {
        private const string kBakeToSkeletonUndoLabel = "Transfer motion to skeleton";
        private const string kBakeToConstraintUndoLabel = "Transfer motion to constraint";

        interface IEvaluationGraph
        {
            void Evaluate(float time);
        }

        class EvaluationGraph : IDisposable, IEvaluationGraph
        {
            SyncSceneToStreamLayer m_SyncSceneToStreamLayer;
            List<IRigLayer> m_RigLayers;
            PlayableGraph m_Graph;

            AnimationClip m_Clip;
            bool m_ClipLoopTime;

            public EvaluationGraph(RigBuilder rigBuilder, AnimationClip clip, AnimationClip defaultPoseClip, IReadOnlyDictionary<IRigConstraint, IRigConstraint> overrides, IRigConstraint lastConstraint = null)
            {
                m_SyncSceneToStreamLayer = new SyncSceneToStreamLayer();

                bool stopBuilding = false;

                var layers = rigBuilder.layers;
                m_RigLayers = new List<IRigLayer>(layers.Count);
                for (int i = 0; i < layers.Count; ++i)
                {
                    if (stopBuilding == true)
                        break;

                    if (layers[i].rig == null || !layers[i].active)
                        continue;

                    IRigConstraint[] constraints = RigUtils.GetConstraints(layers[i].rig);
                    if (constraints == null || constraints.Length == 0)
                        continue;

                    var newConstraints = new List<IRigConstraint>(constraints.Length);
                    foreach (IRigConstraint constraint in constraints)
                    {
                        if (overrides.TryGetValue(constraint, out IRigConstraint newConstraint))
                        {
                            if (newConstraint != null)
                            {
                                newConstraints.Add(newConstraint);
                            }
                        }
                        else
                        {
                            newConstraints.Add(constraint);
                        }

                        if (constraint == lastConstraint)
                        {
                            stopBuilding = true;
                            break;
                        }
                    }

                    m_RigLayers.Add(new OverrideRigLayer(layers[i].rig, newConstraints.ToArray()));
                }

                m_Graph = PlayableGraph.Create("Evaluation-Graph");
                m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                var animator = rigBuilder.GetComponent<Animator>();

                m_Clip = clip;

                var settings = AnimationUtility.GetAnimationClipSettings(m_Clip);
                m_ClipLoopTime = settings.loopTime;

                // Override loop time in clip asset.
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(m_Clip, settings);

                var defaultPosePlayable = AnimationClipPlayable.Create(m_Graph, defaultPoseClip);
                var clipPlayable = AnimationClipPlayable.Create(m_Graph, m_Clip);

                defaultPosePlayable.SetApplyFootIK(false);
                clipPlayable.SetApplyFootIK(false);

                AnimationLayerMixerPlayable mixer = AnimationLayerMixerPlayable.Create(m_Graph, 2);
                mixer.ConnectInput(0, defaultPosePlayable, 0, 1.0f);
                mixer.ConnectInput(1, clipPlayable, 0, 1.0f);

                Playable inputPlayable = mixer;

                var playableChains = RigBuilderUtils.BuildPlayables(animator, m_Graph, m_RigLayers, m_SyncSceneToStreamLayer);
                foreach (var chain in playableChains)
                {
                    if (!chain.IsValid())
                        continue;

                    chain.playables[0].AddInput(inputPlayable, 0, 1);
                    inputPlayable = chain.playables[chain.playables.Length - 1];
                }

                var output = AnimationPlayableOutput.Create(m_Graph, "bake-output", animator);
                output.SetSourcePlayable(inputPlayable);
            }

            public void Evaluate(float time)
            {
                if (!AnimationMode.InAnimationMode())
                    return;

                m_SyncSceneToStreamLayer.Update(m_RigLayers);

                foreach (var layer in m_RigLayers)
                {
                    if (layer.IsValid() && layer.active)
                        layer.Update();
                }

                AnimationMode.BeginSampling();
                AnimationMode.SamplePlayableGraph(m_Graph, 0, time);
                AnimationMode.EndSampling();
            }

            public void Dispose()
            {
                m_Graph.Destroy();
                for (int i = 0; i < m_RigLayers.Count; ++i)
                {
                    m_RigLayers[i].Reset();
                }
                m_RigLayers.Clear();
                m_SyncSceneToStreamLayer.Reset();

                // Restore loop time in clip asset.
                var settings = AnimationUtility.GetAnimationClipSettings(m_Clip);
                settings.loopTime = m_ClipLoopTime;

                AnimationUtility.SetAnimationClipSettings(m_Clip, settings);
            }
        }

        /// <summary>
        /// Validates if the Editor and the provided RigBuilder are in a correct state to do motion transfer.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder that will be used for motion transfer.</param>
        /// <returns>Returns true if both the editor and the provided RigBuilder are in a valid state for motion transfer. Returns false if the requirements are not met.</returns>
        public static bool TransferMotionValidate(RigBuilder rigBuilder)
        {
            if (!AnimationWindowUtils.isPreviewing || AnimationWindowUtils.activeAnimationClip == null)
                return false;

            var selected = Selection.instanceIDs;
            if (selected.Length != 1)
                return false;

            var selectedGO = EditorUtility.InstanceIDToObject(selected[0]) as GameObject;
            if (selectedGO != rigBuilder.gameObject)
                return false;

            var animator = rigBuilder.GetComponent<Animator>();
            if (animator.isHuman)
                return false;

            return true;
        }

        /// <summary>
        /// Validates if the Editor and the provided Rig are in a correct state to do motion transfer.
        /// </summary>
        /// <param name="rig">The Rig that will be used for motion transfer.</param>
        /// <returns>Returns true if both the editor and the provided Rig are in a valid state for motion transfer. Returns false if the requirements are not met.</returns>
        public static bool TransferMotionValidate(Rig rig)
        {
            if (!AnimationWindowUtils.isPreviewing || AnimationWindowUtils.activeAnimationClip == null)
                return false;

            var selected = Selection.instanceIDs;
            if (selected.Length != 1)
                return false;

            var selectedGO = EditorUtility.InstanceIDToObject(selected[0]) as GameObject;
            if (selectedGO != rig.gameObject)
                return false;

            var rigBuilder = rig.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                return false;

            var animator = rigBuilder.GetComponent<Animator>();
            if (animator.isHuman)
                return false;

            bool inRigBuilder = false;
            var layers = rigBuilder.layers;
            for (int i = 0; i < layers.Count; ++i)
            {
                if (layers[i].rig == rig && layers[i].active)
                    inRigBuilder = true;
            }

            return inRigBuilder;
        }

        /// <summary>
        /// Validates if the Editor and the provided RigConstraint are in a correct state to do motion transfer.
        /// </summary>
        /// <typeparam name="T">Type of RigConstraint that is to be validated.</typeparam>
        /// <param name="constraint">The RigConstraint that will be used for motion transfer.</param>
        /// <returns>Returns true if both the editor and the provided RigConstraint are in a valid state for motion transfer. Returns false if the requirements are not met.</returns>
        public static bool TransferMotionValidate<T>(T constraint)
            where T : MonoBehaviour, IRigConstraint
        {
            if (!AnimationWindowUtils.isPreviewing || AnimationWindowUtils.activeAnimationClip == null)
                return false;

            var selected = Selection.instanceIDs;
            if (selected.Length != 1)
                return false;

            var selectedGO = EditorUtility.InstanceIDToObject(selected[0]) as GameObject;
            if (selectedGO != constraint.gameObject)
                return false;

            var rig = constraint.GetComponentInParent<Rig>();
            if (rig == null)
                return false;

            var rigBuilder = rig.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                return false;

            var animator = rigBuilder.GetComponent<Animator>();
            if (animator.isHuman)
                return false;

            bool inRigBuilder = false;
            var layers = rigBuilder.layers;
            for (int i = 0; i < layers.Count; ++i)
            {
                if (layers[i].rig == rig && layers[i].active)
                    inRigBuilder = true;
            }

            if (!inRigBuilder)
                return false;

            return constraint.IsValid();
        }

        /// <summary>
        /// Bakes motion from any RigConstraints in the RigBuilder to the skeleton.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder whose RigConstraints are to be baked.</param>
        public static void TransferMotionToSkeleton(RigBuilder rigBuilder)
        {
            List<RigLayer> layers = rigBuilder.layers;
            List<Rig> rigs = new List<Rig>(layers.Count);
            foreach(var layer in layers)
            {
                if (layer.rig != null && layer.active)
                {
                    rigs.Add(layer.rig);
                }
            }

            TransferMotionToSkeleton(rigBuilder, rigs);
        }

        /// <summary>
        /// Bakes motion from any RigConstraints in the Rig to the skeleton.
        /// </summary>
        /// <param name="rig">The Rig whose RigConstraints are to be baked.</param>
        public static void TransferMotionToSkeleton(Rig rig)
        {
            var rigBuilder = rig.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                throw new InvalidOperationException("No rigbuilder was found in the hierarchy.");

            TransferMotionToSkeleton(rigBuilder, new Rig[]{rig});
        }

        private static void TransferMotionToSkeleton(RigBuilder rigBuilder, IEnumerable<Rig> rigs)
        {
            var constraints = new List<IRigConstraint>();
            foreach(var rig in rigs)
            {
                constraints.AddRange(RigUtils.GetConstraints(rig));
            }

            var clip = AnimationWindowUtils.activeAnimationClip;

            // Make sure we have a clip selected
            if (clip == null)
            {
                throw new InvalidOperationException(
                    "There is no clip to work on." +
                    " The animation window must be open with an active clip!");
            }

            AnimationClip editableClip = clip;
            if (!GetEditableClip(ref editableClip))
                return;

            AnimationClip defaultPoseClip = CreateDefaultPose(rigBuilder);

            Undo.RegisterCompleteObjectUndo(editableClip, kBakeToSkeletonUndoLabel);

            var animator = rigBuilder.GetComponent<Animator>();
            if (editableClip != clip)
                AddClipToAnimatorController(animator, editableClip);

            var bindingsToRemove = new HashSet<EditorCurveBinding>();

            foreach(IRigConstraint constraint in constraints)
            {
                var bakeParameters = FindBakeParameters(constraint);
                if (bakeParameters == null || !bakeParameters.canBakeToSkeleton)
                    continue;

                // Flush out animation mode modifications
                AnimationMode.BeginSampling();
                AnimationMode.EndSampling();

                var bindings = bakeParameters.GetConstrainedCurveBindings(rigBuilder, constraint);
                BakeToSkeleton(rigBuilder, constraint, editableClip, defaultPoseClip, bindings, Preferences.bakeToSkeletonCurveFilterOptions);

                bindingsToRemove.UnionWith(bakeParameters.GetSourceCurveBindings(rigBuilder, constraint));
            }

            // Remove weight curve & force constraint to be active
            if (Preferences.forceConstraintWeightOnBake)
            {
                AnimationCurve zeroWeightCurve = AnimationCurve.Constant(0f, editableClip.length, 0f);

                foreach(var rig in rigs)
                {
                    AnimationUtility.SetEditorCurve(editableClip, GetWeightCurveBinding(rigBuilder, rig), zeroWeightCurve);
                }
            }

            if (Preferences.bakeToSkeletonAndRemoveCurves)
                RemoveCurves(editableClip, bindingsToRemove);
        }

        /// <summary>
        /// Bakes motion from the RigConstraint to the skeleton.
        /// </summary>
        /// <typeparam name="T">Type of RigConstraint that is to be baked.</typeparam>
        /// <param name="constraint">The RigConstraint that will be baked to the skeleton.</param>
        public static void TransferMotionToSkeleton<T>(T constraint)
            where T : MonoBehaviour, IRigConstraint
        {
            var rigBuilder = constraint.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                throw new InvalidOperationException("No rigbuilder was found in the hierarchy.");

            var bakeParameters = FindBakeParameters(constraint);
            if (bakeParameters == null)
                throw new InvalidOperationException(string.Format("Could not find BakeParameters class for constraint {0}.", constraint != null ? constraint.ToString() : "no-name"));

            if (!bakeParameters.canBakeToSkeleton)
                throw new InvalidOperationException("Constraint disallows transfering motion to skeleton.");

            var bindings = bakeParameters.GetConstrainedCurveBindings(rigBuilder, constraint);
            var clip = AnimationWindowUtils.activeAnimationClip;

            // Make sure we have a clip selected
            if (clip == null)
            {
                throw new InvalidOperationException(
                    "There is no clip to work on." +
                    " The animation window must be open with an active clip!");
            }

            AnimationClip editableClip = clip;
            if (!GetEditableClip(ref editableClip))
                return;

            AnimationClip defaultPoseClip = CreateDefaultPose(rigBuilder);

            Undo.RegisterCompleteObjectUndo(editableClip, kBakeToSkeletonUndoLabel);

            var animator = rigBuilder.GetComponent<Animator>();
            if (editableClip != clip)
                AddClipToAnimatorController(animator, editableClip);

            BakeToSkeleton(constraint, editableClip, defaultPoseClip, bindings, Preferences.bakeToSkeletonCurveFilterOptions);

            if (Preferences.forceConstraintWeightOnBake)
            {
                AnimationCurve zeroWeightCurve = AnimationCurve.Constant(0f, editableClip.length, 0f);
                AnimationUtility.SetEditorCurve(editableClip, GetWeightCurveBinding(rigBuilder, constraint), zeroWeightCurve);
            }

            if (Preferences.bakeToSkeletonAndRemoveCurves)
                RemoveCurves(editableClip, bakeParameters.GetSourceCurveBindings(rigBuilder, constraint));

        }

        internal static void BakeToSkeleton<T>(T constraint, AnimationClip clip, AnimationClip defaultPoseClip, IEnumerable<EditorCurveBinding> bindings, CurveFilterOptions filterOptions)
            where T : MonoBehaviour, IRigConstraint
        {
            // Make sure we have a rigbuilder (which guarantees an animator).
            var rigBuilder = constraint.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
            {
                throw new InvalidOperationException(
                    "No rigbuilder was found in the hierarchy. " +
                    "A RigBuilder and Animator are required to construct valid bindings.");
            }

            BakeToSkeleton(rigBuilder, constraint, clip, defaultPoseClip, bindings, filterOptions);
        }

        private static void BakeToSkeleton(RigBuilder rigBuilder, IRigConstraint constraint, AnimationClip clip, AnimationClip defaultPoseClip, IEnumerable<EditorCurveBinding> bindings, CurveFilterOptions filterOptions)
        {
            // Make sure the base constraint is valid
            if (constraint == null || !constraint.IsValid())
            {
                throw new InvalidOperationException(
                    string.Format("The rig constraint {0} is not a valid constraint.",
                    constraint != null ? constraint.ToString() : ""));
            }

            var overrides = new Dictionary<IRigConstraint, IRigConstraint>();

            using(var graph = new EvaluationGraph(rigBuilder, clip, defaultPoseClip, overrides, constraint))
            {
                BakeCurvesToClip(clip, bindings, rigBuilder, graph, filterOptions);
            }
        }

        /// <summary>
        /// Bakes motion from the skeleton to the constraints in the RigBuilder.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder whose RigConstraints are to be baked.</param>
        public static void TransferMotionToConstraint(RigBuilder rigBuilder)
        {
            List<RigLayer> layers = rigBuilder.layers;
            List<Rig> rigs = new List<Rig>(layers.Count);
            foreach(var layer in layers)
            {
                if (layer.rig != null && layer.active)
                {
                    rigs.Add(layer.rig);
                }
            }

            TransferMotionToConstraint(rigBuilder, rigs);
        }

        /// <summary>
        /// Bakes motion from the skeleton to the constraints in the Rig.
        /// </summary>
        /// <param name="rig">The Rig whose RigConstraints are to be baked.</param>
        public static void TransferMotionToConstraint(Rig rig)
        {
            var rigBuilder = rig.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                throw new InvalidOperationException("No rigbuilder was found in the hierarchy.");

            TransferMotionToConstraint(rigBuilder, new Rig[]{rig});
        }

        private static void TransferMotionToConstraint(RigBuilder rigBuilder, IEnumerable<Rig> rigs)
        {
            var constraints = new List<IRigConstraint>();
            foreach(var rig in rigs)
            {
                constraints.AddRange(RigUtils.GetConstraints(rig));
            }

            var clip = AnimationWindowUtils.activeAnimationClip;

            // Make sure we have a clip selected
            if (clip == null)
            {
                throw new InvalidOperationException(
                    "There is no clip to work on." +
                    " The animation window must be open with an active clip!");
            }

            AnimationClip editableClip = clip;
            if (!GetEditableClip(ref editableClip))
                return;

            AnimationClip defaultPoseClip = CreateDefaultPose(rigBuilder);

            Undo.RegisterCompleteObjectUndo(editableClip, kBakeToConstraintUndoLabel);

            var animator = rigBuilder.GetComponent<Animator>();
            if (editableClip != clip)
                AddClipToAnimatorController(animator, editableClip);

            var bindingsToRemove = new HashSet<EditorCurveBinding>();

            // Remove weight curve & force constraint to be active
            if (Preferences.forceConstraintWeightOnBake)
            {
                AnimationCurve oneWeightCurve = AnimationCurve.Constant(0f, editableClip.length, 1f);

                foreach(var rig in rigs)
                {
                    AnimationUtility.SetEditorCurve(editableClip, GetWeightCurveBinding(rigBuilder, rig), oneWeightCurve);
                }
            }

            foreach(IRigConstraint constraint in constraints)
            {
                var bakeParameters = FindBakeParameters(constraint);
                if (bakeParameters == null || !bakeParameters.canBakeToConstraint)
                    continue;

                // Flush out animation mode modifications
                AnimationMode.BeginSampling();
                AnimationMode.EndSampling();

                var bindings = bakeParameters.GetSourceCurveBindings(rigBuilder, constraint);
                BakeToConstraint(rigBuilder, constraint, editableClip, defaultPoseClip, bindings, Preferences.bakeToConstraintCurveFilterOptions);

                bindingsToRemove.UnionWith(bakeParameters.GetConstrainedCurveBindings(rigBuilder, constraint));
            }

            if (Preferences.bakeToConstraintAndRemoveCurves)
                RemoveCurves(editableClip, bindingsToRemove);
        }

        /// <summary>
        /// Bakes motion from the skeleton to the RigConstraint
        /// </summary>
        /// <typeparam name="T">Type of RigConstraint that is to be baked.</typeparam>
        /// <param name="constraint">The RigConstraint that will be baked to.</param>
        public static void TransferMotionToConstraint<T>(T constraint)
            where T : MonoBehaviour, IRigConstraint
        {
            var rigBuilder = constraint.GetComponentInParent<RigBuilder>();
            if (rigBuilder == null)
                throw new InvalidOperationException("No rigbuilder was found in the hierarchy.");

            var bakeParameters = FindBakeParameters(constraint);
            if (bakeParameters == null)
                throw new InvalidOperationException(string.Format("Could not find BakeParameters class for constraint {0}.", constraint != null ? constraint.ToString() : "no-name"));

            if (!bakeParameters.canBakeToSkeleton)
                throw new InvalidOperationException("Constraint disallows transfering motion to constraint.");

            var bindings = bakeParameters.GetSourceCurveBindings(rigBuilder, constraint);
            var clip = AnimationWindowUtils.activeAnimationClip;

            // Make sure we have a clip selected
            if (clip == null)
            {
                throw new InvalidOperationException(
                    "There is no clip to work on." +
                    " The animation window must be open with an active clip!");
            }

            AnimationClip editableClip = clip;
            if (!GetEditableClip(ref editableClip))
                return;

            AnimationClip defaultPoseClip = CreateDefaultPose(rigBuilder);

            Undo.RegisterCompleteObjectUndo(editableClip, kBakeToConstraintUndoLabel);

            var animator = rigBuilder.GetComponent<Animator>();
            if (editableClip != clip)
                AddClipToAnimatorController(animator, editableClip);

            // Remove weight curve & force constraint to be active
            if (Preferences.forceConstraintWeightOnBake)
            {
                AnimationCurve oneWeightCurve = AnimationCurve.Constant(0f, editableClip.length, 1f);
                AnimationUtility.SetEditorCurve(editableClip, GetWeightCurveBinding(rigBuilder, constraint), oneWeightCurve);
            }

            BakeToConstraint(constraint, editableClip, defaultPoseClip, bindings, Preferences.bakeToConstraintCurveFilterOptions);

            if (Preferences.bakeToConstraintAndRemoveCurves)
                RemoveCurves(editableClip, bakeParameters.GetConstrainedCurveBindings(rigBuilder, constraint));
        }

        internal static void BakeToConstraint<T>(T constraint, AnimationClip clip, AnimationClip defaultPoseClip, IEnumerable<EditorCurveBinding> bindings, CurveFilterOptions filterOptions)
            where T : MonoBehaviour, IRigConstraint
        {
            // Make sure we have a rigbuilder (which guarantees an animator).
            var rigBuilder = constraint.GetComponentInParent<RigBuilder>();
            if(rigBuilder == null)
            {
                throw new InvalidOperationException(
                    "No rigbuilder was found in the hierarchy. " +
                    "A RigBuilder and Animator are required to construct valid bindings.");
            }

            BakeToConstraint(rigBuilder, constraint, clip, defaultPoseClip, bindings, filterOptions);
        }

        private static void BakeToConstraint(RigBuilder rigBuilder, IRigConstraint constraint, AnimationClip clip, AnimationClip defaultPoseClip, IEnumerable<EditorCurveBinding> bindings, CurveFilterOptions filterOptions)
        {
            // Make sure the base constraint is valid
            if (constraint == null || !constraint.IsValid())
            {
                throw new InvalidOperationException(
                    string.Format("The rig constraint {0} is not a valid constraint.",
                    constraint != null ? constraint.ToString() : ""));
            }

            // Check if the constraint is inverse solvable
            var inverseConstraint = FindInverseRigConstraint(constraint);
            if (inverseConstraint == null)
            {
                throw new InvalidOperationException(
                    string.Format("No inverse rig constraint could be found for {0}.",
                    constraint.ToString()));
            }
            else if (!inverseConstraint.IsValid())
            {
                throw new InvalidOperationException(
                    string.Format("The inverse rig constrain {1} for {0} is not a valid constraint.",
                    constraint.ToString(),
                    inverseConstraint.ToString()));
            }

            var overrides = new Dictionary<IRigConstraint, IRigConstraint>();
            overrides.Add(constraint, inverseConstraint);

            using(var graph = new EvaluationGraph(rigBuilder, clip, defaultPoseClip, overrides, constraint))
            {
                BakeCurvesToClip(clip, bindings, rigBuilder, graph, filterOptions);
            }
        }

        private static AnimationClip CreateDefaultPose(RigBuilder rigBuilder)
        {
            if(rigBuilder == null)
                throw new ArgumentNullException("It is not possible to bake curves without an RigBuilder.");

            var defaultPoseClip = new AnimationClip() { name = "DefaultPose" };

            if (!AnimationMode.InAnimationMode())
                return defaultPoseClip;

            var bindings = new List<EditorCurveBinding>();

            var gameObjects = new Queue<GameObject>();
            gameObjects.Enqueue(rigBuilder.gameObject);

            while (gameObjects.Count > 0)
            {
                var gameObject = gameObjects.Dequeue();

                EditorCurveBinding[] allBindings = AnimationUtility.GetAnimatableBindings(gameObject, rigBuilder.gameObject);
                foreach (var binding in allBindings)
                {
                    if (binding.isPPtrCurve)
                        continue;

                    if (binding.type == typeof(GameObject))
                        continue;

                    UnityEngine.Object target = gameObject.GetComponent(binding.type);
                    if (!AnimationMode.IsPropertyAnimated(target, binding.propertyName))
                        continue;

                    bindings.Add(binding);
                }

                // Iterate over all child GOs
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    gameObjects.Enqueue(childTransform.gameObject);
                }
            }

            // Flush out animation mode modifications
            AnimationMode.BeginSampling();
            AnimationMode.EndSampling();

            foreach(var binding in bindings)
            {
                float floatValue;
                AnimationUtility.GetFloatValue(rigBuilder.gameObject, binding, out floatValue);

                var key = new Keyframe(0f, floatValue);
                var curve = new AnimationCurve(new Keyframe[] {key});
                defaultPoseClip.SetCurve(binding.path, binding.type, binding.propertyName, curve);
            }

            return defaultPoseClip;
        }

        private static void BakeCurvesToClip(AnimationClip clip, IEnumerable<EditorCurveBinding> bindings, RigBuilder rigBuilder, IEvaluationGraph graph, CurveFilterOptions filterOptions)
        {
            if(rigBuilder == null)
                throw new ArgumentNullException("It is not possible to bake curves without an RigBuilder.");

            if (clip == null)
                throw new ArgumentNullException("It is not possible to bake curves to a clip that is null.");

            if (!AnimationMode.InAnimationMode())
                throw new ArgumentException("AnimationMode must be active during bake operation.");

            var animator = rigBuilder.GetComponent<Animator>();

            var recorder = new GameObjectRecorder(animator.gameObject);
            foreach (var binding in bindings)
                recorder.Bind(binding);

            var frameCount = (int)(clip.length * clip.frameRate);
            float dt = 1f / clip.frameRate;
            float time = 0f;

            graph?.Evaluate(0f);
            recorder.TakeSnapshot(0f);

            for (int frame = 1; frame <= frameCount; ++frame)
            {
                time = frame / clip.frameRate;
                graph?.Evaluate(time);
                recorder.TakeSnapshot(dt);
            }

            var tempClip = new AnimationClip();
            recorder.SaveToClip(tempClip, clip.frameRate, filterOptions);
            CopyCurvesToClip(tempClip, clip);
        }

        private static void RemoveCurves(AnimationClip clip, IEnumerable<EditorCurveBinding> bindings)
        {
            if (clip == null)
                throw new ArgumentNullException("The destination animation clip cannot be null.");

            var rotationBinding = new EditorCurveBinding();
            foreach(var binding in bindings)
            {
                // Remove the correct editor curve binding for a rotation curves
                if (EditorCurveBindingUtils.RemapRotationBinding(clip, binding, ref rotationBinding))
                    AnimationUtility.SetEditorCurve(clip, rotationBinding, null);
                else
                    AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static void CopyCurvesToClip(AnimationClip fromClip, AnimationClip toClip)
        {
            var rotationBinding = new EditorCurveBinding();
            var bindings = AnimationUtility.GetCurveBindings(fromClip);
            foreach(var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(fromClip, binding);

                if (EditorCurveBindingUtils.RemapRotationBinding(toClip, binding, ref rotationBinding))
                    AnimationUtility.SetEditorCurve(toClip, rotationBinding, curve);
                else
                    AnimationUtility.SetEditorCurve(toClip, binding, curve);
            }
        }

        private static IRigConstraint FindInverseRigConstraint(IRigConstraint constraint)
        {
            if (constraint == null)
                return null;

            var inverseConstraintTypes = TypeCache.GetTypesWithAttribute<InverseRigConstraintAttribute>();
            foreach (var inverseConstraintType in inverseConstraintTypes)
            {
                var attribute = inverseConstraintType.GetCustomAttribute<InverseRigConstraintAttribute>();

                if (attribute.baseConstraint == constraint.GetType())
                    return (IRigConstraint)Activator.CreateInstance(inverseConstraintType, new object[] { constraint });
            }

            return null;
        }

        internal static IBakeParameters FindBakeParameters(IRigConstraint constraint)
        {
            var constraintType = constraint.GetType();
            var bakeParametersTypes = TypeCache.GetTypesWithAttribute<BakeParametersAttribute>();
            foreach (var bakeParametersType in bakeParametersTypes)
            {
                if (!typeof(IBakeParameters).IsAssignableFrom(bakeParametersType))
                    continue;

                var attribute = bakeParametersType.GetCustomAttribute<BakeParametersAttribute>();
                if (attribute.constraintType == constraintType)
                    return (IBakeParameters)Activator.CreateInstance(bakeParametersType);
            }

            return null;
        }

        private static EditorCurveBinding GetWeightCurveBinding(RigBuilder rigBuilder, Rig rig)
        {
            var path = AnimationUtility.CalculateTransformPath(rig.transform, rigBuilder.transform);
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Rig), ConstraintProperties.s_Weight);
            return binding;
        }

        private static EditorCurveBinding GetWeightCurveBinding<T>(RigBuilder rigBuilder, T constraint)
            where T : MonoBehaviour, IRigConstraint
        {
            var path = AnimationUtility.CalculateTransformPath(constraint.transform, rigBuilder.transform);
            var binding = EditorCurveBinding.FloatCurve(path, typeof(T), ConstraintProperties.s_Weight);
            return binding;
        }

        private static bool GetEditableClip(ref AnimationClip clip)
        {
            if (clip == null)
                return false;

            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
            {
                var path = EditorUtility.SaveFilePanelInProject(
                    "Save new clip",
                    clip.name + "(Clone)",
                    "anim",
                    string.Format("Create an editable clone of the readonly clip {0}.", clip.name));

                if (path == "")
                    return false;

                clip = UnityEngine.Object.Instantiate(clip);
                AssetDatabase.CreateAsset(clip, path);
            }

            return true;
        }

        private static void AddClipToAnimatorController(Animator animator, AnimationClip clip)
        {
            RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;

            AnimatorController effectiveController = runtimeController as AnimatorController;
            if (effectiveController == null)
            {
                AnimatorOverrideController overrideController = runtimeController as AnimatorOverrideController;
                if (overrideController != null)
                {
                    effectiveController = overrideController.runtimeAnimatorController as AnimatorController;
                }
            }

            if (effectiveController != null)
            {
                string title = "Add clip to controller";
                string message = String.Format("Do you want to add clip '{0}' to controller '{1}'?", clip.name, effectiveController.name);
                if (EditorUtility.DisplayDialog(title, message, "yes", "no", DialogOptOutDecisionType.ForThisSession, "com.unity.animation.rigging-add-clip-to-controller"))
                {
                    effectiveController.AddMotion(clip);
                    AnimationWindowUtils.activeAnimationClip = clip;
                    AnimationWindowUtils.StartPreview();
                }
            }
        }
    }
}
