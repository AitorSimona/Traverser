using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]
    [RequireComponent(typeof(TraverserLocomotionAbility))]

    public class TraverserParkourAbility : MonoBehaviour, TraverserAbility
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

        // --- Private Variables ---

        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        // -------------------------------------------------

        // --- Basic Methods ---

        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserInputLayer.capture.UpdateParkour();
            return animationController.transition.UpdateTransition() ? this : null;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                return this;

            return null;
        }

        public TraverserAbility OnPostUpdate(float deltaTime)
        {
            return null;
        }

        public bool OnContact(TraverserAffineTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            TraverserInputLayer.capture.UpdateParkour();

            if (TraverserInputLayer.capture.parkourButton && !animationController.transition.isON)
            {
     

                // --- Identify collider's object layer ---
                //ref MovementController.Closure closure = ref controller.current;
                //Assert.IsTrue(closure.isColliding);

                //Collider collider = closure.collider;

                ref Collider collider = ref controller.current.collider;

                TraverserParkourObject parkourObject = collider.GetComponent<TraverserParkourObject>();

                if(parkourObject != null)
                    ret = RequestTransition(ref parkourObject, false);


                //int layerMask = 1 << collider.gameObject.layer;
                //Assert.IsTrue((layerMask & 0x1F01) != 0);

                //Parkour type = Parkour.Create(collider.gameObject.layer);
                ////Speed speed = GetSpeedTag();

                //if (type.IsType(Parkour.Type.Wall) || type.IsType(Parkour.Type.Table))
                //{
                //    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                //        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                //    {
                //        ret = RequestTransition(ref synthesizer, contactTransform, type);
                //    }
                //}
                //else if (type.IsType(Parkour.Type.Platform))
                //{
                //    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                //        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                //    {
                //        ret = RequestTransition(ref synthesizer, contactTransform, type);
                //    }
                //}
                //else if (type.IsType(Parkour.Type.Ledge))
                //{
                //    if (TagExtensions.IsAxis(collider, contactTransform, Missing.forward) ||
                //        TagExtensions.IsAxis(collider, contactTransform, Missing.right))
                //    {
                //        ret = RequestTransition(ref synthesizer, contactTransform, type);
                //    }
                //}
            }

            return ret;
        }

        bool RequestTransition(ref TraverserParkourObject parkourObject, bool isDrop) 
        {
            bool ret = false;

            if (parkourObject)
            {
                Debug.Log("Transitioning");

                float speed = math.length(controller.targetVelocity);

                Vector3 target = controller.contactTransform.t;
                target += -controller.contactNormal * controller.contactSize;

                TraverserAffineTransform targetTransform = TraverserAffineTransform.Create(target, controller.contactTransform.q);

                Debug.Log(speed);

                switch (parkourObject.type)
                {
                    case TraverserParkourObject.TraverserParkourType.Wall:

                        Vector3 contactRight = Vector3.Cross(controller.contactNormal, parkourObject.transform.up);
                        bool right = Vector3.Dot(transform.forward, contactRight) > 0.0;

                        target = controller.contactTransform.t;

                        if (right)
                        {
                            target += contactRight * 1.0f;
                            targetTransform.q *= Quaternion.Euler(0.0f, 90.0f, 0.0f);
                        }
                        else
                        {
                            target -= contactRight * 1.0f;
                            targetTransform.q *= Quaternion.Euler(0.0f, -90.0f, 0.0f);
                        }

                        targetTransform.t = target;

                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= 1.5)// TODO: Create walk speed
                        {
                            if(right)
                                ret = animationController.transition.StartTransition("JogTransition", "WallRunRight", "JogTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("JogTransition", "WallRunLeft", "JogTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else
                        {
                            if (right)
                                ret = animationController.transition.StartTransition("RunTransition", "WallRunRight", "RunTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("RunTransition", "WallRunLeft", "RunTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Table:

                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= 1.5)// TODO: Create walk speed
                        {
                            ret = animationController.transition.StartTransition("JogTransition", "VaultTableJog", "JogTransitionTrigger", "TableTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else
                        {
                            ret = animationController.transition.StartTransition("RunTransition", "VaultTableRun", "RunTransitionTrigger", "TableTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Platform:

                        target = controller.current.position;

                        if (!isDrop)
                        {
                            target = controller.contactTransform.t;
                            target += -controller.contactNormal * 0.5f;
                        }
                        else
                            targetTransform.q = transform.rotation;

                        targetTransform.t = target;

                        if (speed <= 1.5)// TODO: Create walk speed
                        {
                            if(!isDrop)
                                ret = animationController.transition.StartTransition("WalkTransition", "ClimbPlatformWalk", "WalkTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("WalkTransition", "DropPlatformWalk", "WalkTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);

                        }
                        else if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= 1.5)// TODO: Create walk speed
                        {
                            if (!isDrop)
                                ret = animationController.transition.StartTransition("JogTransition", "ClimbPlatformJog", "JogTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("JogTransition", "DropPlatformJog", "JogTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);
                        }
                        else
                        {
                            if (!isDrop)
                                ret = animationController.transition.StartTransition("RunTransition", "ClimbPlatformRun", "RunTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("RunTransition", "DropPlatformRun", "RunTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Ledge:

                        if (speed <= 1.5)// TODO: Create walk speed
                        {
                            ret = animationController.transition.StartTransition("WalkTransition", "VaultLedgeWalk", "WalkTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= 1.5)// TODO: Create walk speed
                        {
                            ret = animationController.transition.StartTransition("JogTransition", "VaultLedgeJog", "JogTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else
                        {
                            ret = animationController.transition.StartTransition("RunTransition", "VaultLedgeRun", "RunTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Tunnel:

                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= 1.5) // TODO: Create walk speed
                        {
                            ret = animationController.transition.StartTransition("JogTransition", "SlideTunnelJog", "JogTransitionTrigger", "TunnelTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else
                        {
                            ret = animationController.transition.StartTransition("RunTransition", "SlideTunnelRun", "RunTransitionTrigger", "TunnelTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }

                        break;
                    default:
                        break;
                }


                //ret = true;
            }

            return ret;
        }

        public bool OnDrop(float deltaTime)
        {
            bool ret = false;

            TraverserInputLayer.capture.UpdateParkour();

            if (TraverserInputLayer.capture.parkourDropDownButton 
                && controller.previous.isGrounded 
                && controller.previous.ground != null
                && !animationController.transition.isON)
            {
                // --- Get the ground's collider ---
                //Transform ground = controller.previous.ground.transform;
                //BoxCollider collider = ground.GetComponent<BoxCollider>();

                ref Collider collider = ref controller.current.ground;

                if (collider != null)
                {
                    TraverserParkourObject parkourObject = controller.previous.ground.GetComponent<TraverserParkourObject>();

                    ret = RequestTransition(ref parkourObject, true);
                }

                // if (collider != null)
                // {
                //// --- Create all of the collider's vertices ---
                //NativeArray<float3> vertices = new NativeArray<float3>(4, Allocator.Persistent);

                //Vector3 center = collider.center;
                //Vector3 size = collider.size;

                //vertices[0] = ground.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
                //vertices[1] = ground.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
                //vertices[2] = ground.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
                //vertices[3] = ground.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);

                //float3 p = controller.previous.position;
                //TraverserAffineTransform contactTransform = TagExtensions.GetClosestTransform(vertices[0], vertices[1], p);
                //float minimumDistance = math.length(contactTransform.t - p);

                //// --- Find out where the character will make contact with the ground ---
                //for (int i = 1; i < 4; ++i)
                //{
                //    int j = (i + 1) % 4;
                //    TraverserAffineTransform candidateTransform = TagExtensions.GetClosestTransform(vertices[i], vertices[j], p);

                //    float distance = math.length(candidateTransform.t - p);
                //    if (distance < minimumDistance)
                //    {
                //        minimumDistance = distance;
                //        contactTransform = candidateTransform;
                //    }
                //}

                //vertices.Dispose();

                //// --- Activate a transition towards the contact point ---
                //ret = RequestTransition(contactTransform, 0/*Parkour.Create(Parkour.Type.DropDown)*/);
                //}
            }

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        //Speed GetSpeedTag()
        //{
        //    float desiredLinearSpeed = TraverserInputLayer.capture.run ? locomotion.desiredSpeedFast : locomotion.desiredSpeedSlow;

        //    Speed speed = Speed.Create(Speed.Type.Normal);

        //    //Debug.Log(desiredLinearSpeed * InputLayer.capture.moveIntensity);

        //    if (TraverserInputLayer.capture.moveIntensity * desiredLinearSpeed < locomotion.desiredSpeedSlow)
        //    {
        //        // slow speed
        //        speed = Speed.Create(Speed.Type.Slow);
        //        //Debug.Log("Slow");
        //    }
        //    else if (TraverserInputLayer.capture.moveIntensity * desiredLinearSpeed >= locomotion.desiredSpeedSlow &&
        //        TraverserInputLayer.capture.moveIntensity * desiredLinearSpeed < locomotion.desiredSpeedFast*0.75)
        //    {
        //        // normal speed
        //        //Debug.Log("Normal");
        //    }
        //    else
        //    {
        //        // fast speed
        //        speed = Speed.Create(Speed.Type.Fast);
        //        //Debug.Log("Fast");

        //    }

        //    return speed;
        //}

        // -------------------------------------------------
    }
}

