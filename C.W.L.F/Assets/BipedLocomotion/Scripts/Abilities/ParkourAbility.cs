using Unity.Collections;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;
using UnityEngine.Assertions;

namespace CWLF
{
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]
    public class ParkourAbility : SnapshotProvider, Ability
    {
        // --- Inspector variables ---
        [Header("Transition settings")]
        [Tooltip("Distance in meters for performing movement validity checks.")]
        [Range(0.0f, 1.0f)]
        public float contactThreshold;

        [Tooltip("Maximum linear error for transition poses.")]
        [Range(0.0f, 1.0f)]
        public float maximumLinearError;

        [Tooltip("Maximum angular error for transition poses.")]
        [Range(0.0f, 180.0f)]
        public float maximumAngularError;

        [Header("Debug settings")]
        [Tooltip("Enables debug display for this ability.")]
        public bool enableDebugging;

        [Tooltip("Determines the movement to debug.")]
        public int debugIndex;

        [Tooltip("Controls the pose debug display.")]
        [Range(0, 100)]
        public int debugPoseIndex;

        // --- Input wrapper ---
        public struct FrameCapture
        {
            public bool jumpButton;
        }

        [Snapshot]
        FrameCapture capture;

        [Snapshot]
        AnchoredTransitionTask anchoredTransition;

        // --- Basic Methods ---

        public override void OnEnable()
        {
            base.OnEnable();
            anchoredTransition = AnchoredTransitionTask.Invalid;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            anchoredTransition.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind) // if we are not using snapshot debugger to rewind
            {
                capture.jumpButton = Input.GetButton("A Button");
            }
        }

        // --- Ability class methods ---

        public Ability OnUpdate(float deltaTime)
        {
            MovementController controller = GetComponent<MovementController>();
            bool active = anchoredTransition.isValid;

            controller.collisionEnabled = !active;
            controller.groundSnap = !active;
            controller.resolveGroundPenetration = !active;
            controller.gravityEnabled = !active;

            if (active)
            {
                Kinematica kinematica = GetComponent<Kinematica>();
                ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

                if (!anchoredTransition.IsState(AnchoredTransitionTask.State.Complete) && !anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
                {
                    anchoredTransition.synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);

                    // --- Finally tell kinematica to wait for this job to finish before executing other stuff ---
                    kinematica.AddJobDependency(AnchoredTransitionJob.Schedule(ref anchoredTransition));

                    return this;
                }

                anchoredTransition.Dispose();
                anchoredTransition = AnchoredTransitionTask.Invalid;
            }
            
            return null;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            if (capture.jumpButton)
            {             
                // --- Identify collider's object layer ---
                MovementController controller = GetComponent<MovementController>();
                ref MovementController.Closure closure = ref controller.current;
                Assert.IsTrue(closure.isColliding);

                Collider collider = closure.collider;

                int layerMask = 1 << collider.gameObject.layer;
                Assert.IsTrue((layerMask & 0x1F01) != 0);

                Parkour type = Parkour.Create(collider.gameObject.layer);

                if (type.IsType(Parkour.Type.Wall) || type.IsType(Parkour.Type.Table))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward))
                    {
                        ret = OnParkourContact(ref synthesizer, contactTransform, type);
                    }
                }
                else if (type.IsType(Parkour.Type.Platform))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                    {
                        ret = OnParkourContact(ref synthesizer, contactTransform, type);
                    }
                }
                else if (type.IsType(Parkour.Type.Ledge))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                    {
                        ret = OnParkourContact(ref synthesizer, contactTransform, type);
                    }
                }
            }

            return ret;
        }

        bool OnParkourContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, Parkour type)
        {
            if (enableDebugging)
            {
                DisplayTransition(ref synthesizer, contactTransform, type, contactThreshold);
            }

            ref Binary binary = ref synthesizer.Binary;

            var sequence = TagExtensions.GetPoseSequence(ref binary, contactTransform,
                    type, contactThreshold);

            anchoredTransition.Dispose();
            anchoredTransition = AnchoredTransitionTask.Create(ref synthesizer,
                    sequence, contactTransform, maximumLinearError,
                        maximumAngularError);

            return true;
        }

        void DisplayTransition<T>(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, T value, float contactThreshold) where T : struct
        {
            if (enableDebugging)
            {
                ref Binary binary = ref synthesizer.Binary;

                NativeArray<TagExtensions.OBB> obbs =
                    TagExtensions.GetBoundsFromContactPoints(ref binary,
                        contactTransform, value, contactThreshold);

                //
                // Display all relevant box colliders
                //

                int numObbs = obbs.Length;
                for (int i = 0; i < numObbs; ++i)
                {
                    TagExtensions.OBB obb = obbs[i];
                    obb.transform = contactTransform * obb.transform;
                    TagExtensions.DebugDraw(obb, Color.cyan);
                }

                var tagTraitIndex = binary.GetTraitIndex(value);

                int numTags = binary.numTags;

                int validIndex = 0;

                for (int i = 0; i < numTags; ++i)
                {
                    ref Binary.Tag tag = ref binary.GetTag(i);

                    if (tag.traitIndex == tagTraitIndex)
                    {
                        if (validIndex == debugIndex)
                        {
                            TagExtensions.DebugDrawContacts(ref binary, ref tag,
                                contactTransform, obbs, contactThreshold);

                            TagExtensions.DebugDrawPoseAndTrajectory(ref binary, ref tag,
                                contactTransform, debugPoseIndex);

                            return;
                        }

                        validIndex++;
                    }
                }

                obbs.Dispose();
            }
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            bool ret = false;
            MovementController controller = GetComponent<MovementController>();

            if (controller.previous.isGrounded && controller.previous.ground != null)
            {
                Transform ground = controller.previous.ground;
                BoxCollider collider = ground.GetComponent<BoxCollider>();

                if (collider != null)
                {
                    NativeArray<float3> vertices = new NativeArray<float3>(4, Allocator.Persistent);

                    Vector3 center = collider.center;
                    Vector3 size = collider.size;

                    vertices[0] = ground.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
                    vertices[1] = ground.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
                    vertices[2] = ground.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
                    vertices[3] = ground.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);

                    float3 p = controller.previous.position;
                    AffineTransform contactTransform = TagExtensions.GetClosestTransform(vertices[0], vertices[1], p);
                    float minimumDistance = math.length(contactTransform.t - p);

                    for (int i = 1; i < 4; ++i)
                    {
                        int j = (i + 1) % 4;
                        AffineTransform candidateTransform = TagExtensions.GetClosestTransform(vertices[i], vertices[j], p);
                       
                        float distance = math.length(candidateTransform.t - p);
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                            contactTransform = candidateTransform;
                        }
                    }

                    vertices.Dispose();

                    ret = OnParkourContact(ref synthesizer, contactTransform, Parkour.Create(Parkour.Type.DropDown));
                }
            }

            return ret;
        }
    }
}
