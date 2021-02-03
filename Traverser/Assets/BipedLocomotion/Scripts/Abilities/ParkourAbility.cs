using System.Collections.Generic;
using Unity.Collections;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]
    [RequireComponent(typeof(LocomotionAbility))]

    public class ParkourAbility : SnapshotProvider, Ability
    {
        // --- Attributes ---
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

        // -------------------------------------------------

        [Snapshot]
        AnchoredTransitionTask anchoredTransition; // Kinematica animation transition handler

        Kinematica kinematica;

        MovementController controller;

        LocomotionAbility locomotion;

        // --- Basic Methods ---

        public override void OnEnable()
        {
            base.OnEnable();
            anchoredTransition = AnchoredTransitionTask.Invalid;
            controller = GetComponent<MovementController>();
            kinematica = GetComponent<Kinematica>();
            locomotion = GetComponent<LocomotionAbility>();
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
                InputLayer.capture.UpdateParkour();
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            // --- If we are in a transition disable controller ---
            CollisionLayer.ConfigureController(anchoredTransition.isValid, ref controller);

            if(KinematicaLayer.UpdateAnchoredTransition(ref anchoredTransition, ref kinematica))           
                 return this;

            return null;
        }

        public Ability OnPostUpdate(float deltaTime)
        {

            return null;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            if (InputLayer.capture.parkourButton)
            {
                // --- Identify collider's object layer ---
                ref MovementController.Closure closure = ref controller.current;
                Assert.IsTrue(closure.isColliding);

                Collider collider = closure.collider;

                int layerMask = 1 << collider.gameObject.layer;
                Assert.IsTrue((layerMask & 0x1F01) != 0);

                Parkour type = Parkour.Create(collider.gameObject.layer);
                //Speed speed = GetSpeedTag();

                if (type.IsType(Parkour.Type.Wall) || type.IsType(Parkour.Type.Table))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                    {
                        ret = RequestTransition(ref synthesizer, contactTransform, type);
                    }
                }
                else if (type.IsType(Parkour.Type.Platform))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                    {
                        ret = RequestTransition(ref synthesizer, contactTransform, type);
                    }
                }
                else if (type.IsType(Parkour.Type.Ledge))
                {
                    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                    {
                        ret = RequestTransition(ref synthesizer, contactTransform, type);
                    }
                }
            }

            return ret;
        }

        bool RequestTransition(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, Parkour type)
        {
            // --- Require transition animation of the type given ---
            ref Binary binary = ref synthesizer.Binary;
            SegmentCollisionCheck collisionCheck = SegmentCollisionCheck.AboveGround | SegmentCollisionCheck.InsideGeometry;

            // --- Prevent collision checks ---
            //if (type.type == Parkour.Type.Wall)
            //{
            //    collisionCheck &= ~SegmentCollisionCheck.InsideGeometry;
            //    collisionCheck &= ~SegmentCollisionCheck.AboveGround;
            //}

            QueryResult sequence = TagExtensions.GetPoseSequence(ref binary, contactTransform,
                    type, GetSpeedTag(), contactThreshold, collisionCheck);

            if (sequence.length == 0)
            {
                Debug.Log("No sequences in parkour");
                return false;
            }

            anchoredTransition.Dispose();
            anchoredTransition = AnchoredTransitionTask.Create(ref synthesizer,
                    sequence, contactTransform, maximumLinearError,
                        maximumAngularError);

            return true;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            bool ret = false;

            if (InputLayer.capture.parkourDropDownButton && controller.previous.isGrounded && controller.previous.ground != null)
            {
                // --- Get the ground's collider ---
                Transform ground = controller.previous.ground;
                BoxCollider collider = ground.GetComponent<BoxCollider>();

                if (collider != null)
                {
                    // --- Create all of the collider's vertices ---
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

                    // --- Find out where the character will make contact with the ground ---
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

                    // --- Activate a transition towards the contact point ---
                    ret = RequestTransition(ref synthesizer, contactTransform, Parkour.Create(Parkour.Type.DropDown));
                }
            }

            return ret;
        }

        Speed GetSpeedTag()
        {
            float desiredLinearSpeed = InputLayer.capture.run ? locomotion.desiredSpeedFast : locomotion.desiredSpeedSlow;

            Speed speed = Speed.Create(Speed.Type.Normal);

            //Debug.Log(desiredLinearSpeed * InputLayer.capture.moveIntensity);

            if (InputLayer.capture.moveIntensity * desiredLinearSpeed < locomotion.desiredSpeedSlow)
            {
                // slow speed
                speed = Speed.Create(Speed.Type.Slow);
                //Debug.Log("Slow");
            }
            else if (InputLayer.capture.moveIntensity * desiredLinearSpeed >= locomotion.desiredSpeedSlow &&
                InputLayer.capture.moveIntensity * desiredLinearSpeed < locomotion.desiredSpeedFast*0.75)
            {
                // normal speed
                //Debug.Log("Normal");
            }
            else
            {
                // fast speed
                speed = Speed.Create(Speed.Type.Fast);
                //Debug.Log("Fast");

            }

            return speed;
        }

        // -------------------------------------------------
    }
}

