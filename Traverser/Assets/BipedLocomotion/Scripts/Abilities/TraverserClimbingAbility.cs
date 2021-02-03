using Unity.Kinematica;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(MovementController))]

    public class TraverserClimbingAbility : SnapshotProvider, TraverserAbility
    {
        // --- Attributes ---
        [Header("Transition settings")]
        [Tooltip("Distance in meters for performing movement validity checks")]
        [Range(0.0f, 1.0f)]
        public float contactThreshold;

        [Tooltip("Maximum linear error for transition poses")]
        [Range(0.0f, 1.0f)]
        public float maximumLinearError;

        [Tooltip("Maximum angular error for transition poses.")]
        [Range(0.0f, 180.0f)]
        public float maximumAngularError;

        [Header("Ledge prediction settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedLedge;

        [Tooltip("How fast or slow the target velocity is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float velocityPercentageLedge;

        // --------------------------------

        // --- Climbing movement/state ---
        public enum TraverserClimbingTriggerState
        {
            Suspended,
            Mounting,
            Climbing,
            FreeClimbing,
            Dismount,
            PullUp,
            DropDown
        }

        // --------------------------------

        // --- Climbing directions ---
        public enum TraverserClimbingState
        {
            Idle,
            Up,
            Down,
            Left,
            Right,
            UpRight,
            DownRight,
            UpLeft,
            DownLeft,
            CornerRight, 
            CornerLeft, 
            None
        }

        // --------------------------------

        Kinematica kinematica; // system
        MovementController controller;
        CapsuleCollider capsule;

        TraverserClimbingTriggerState state; // Actual Climbing movement/state
        TraverserClimbingTriggerState previousState; // Previous Climbing movement/state

        TraverserClimbingState climbingState; // Actual Climbing direction
        TraverserClimbingState previousClimbingState; // Previous Climbing direction
        TraverserClimbingState lastCollidingClimbingState;

        // --------------------------------

        // --- World interactable elements ---
        [Snapshot]
        TraverserLedgeObject.LedgeGeometry ledgeGeometry;

        TraverserLedgeObject.LedgeGeometry auxledgeGeometry;

        [Snapshot]
        TraverserWallObject.WallGeometry wallGeometry;

        [Snapshot]
        TraverserLedgeObject.LedgeAnchor ledgeAnchor;

        [Snapshot]
        TraverserWallObject.WallAnchor wallAnchor;

        [Snapshot]
        AnchoredTransitionTask anchoredTransition;

        // --------------------------------

        // --- Basic Methods ---
        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();
            controller = GetComponent<MovementController>();
            capsule = GetComponent<CapsuleCollider>();

            state = TraverserClimbingTriggerState.Suspended;
            previousState = TraverserClimbingTriggerState.Suspended;

            climbingState = TraverserClimbingState.Idle;
            previousClimbingState = TraverserClimbingState.Idle;
            lastCollidingClimbingState = TraverserClimbingState.None;

            ledgeGeometry = TraverserLedgeObject.LedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.LedgeGeometry.Create();
            wallGeometry = TraverserWallObject.WallGeometry.Create();

            ledgeAnchor = TraverserLedgeObject.LedgeAnchor.Create();
            wallAnchor = TraverserWallObject.WallAnchor.Create();

            anchoredTransition = AnchoredTransitionTask.Invalid;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            ledgeGeometry.Dispose();
            auxledgeGeometry.Dispose();
            anchoredTransition.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind) // if we are not using snapshot debugger to rewind
                TraverserInputLayer.capture.UpdateClimbing(); // Update climbing input
        }

        // --------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

            // --- Turn off/on controller ---
            TraverserCollisionLayer.ConfigureController(!IsState(TraverserClimbingTriggerState.Suspended), ref controller);

            // --- If character is not falling ---
            if (!IsState(TraverserClimbingTriggerState.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case TraverserClimbingTriggerState.Mounting:
                        HandleMountingState(ref synthesizer);
                        break;

                    case TraverserClimbingTriggerState.Dismount:
                        HandleDismountState(ref synthesizer);
                        break;

                    case TraverserClimbingTriggerState.PullUp:
                        HandlePullUpState(ref synthesizer);
                        break;

                    case TraverserClimbingTriggerState.Climbing:
                        HandleClimbingState(ref synthesizer, deltaTime);
                        break;

                    case TraverserClimbingTriggerState.FreeClimbing:
                        HandleFreeClimbingState(ref synthesizer, deltaTime);
                        break;

                    case TraverserClimbingTriggerState.DropDown:
                        HandleDropDownState(ref synthesizer);
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(TraverserClimbingTriggerState.Suspended))
                    return null;

                TraverserKinematicaLayer.UpdateAnchoredTransition(ref anchoredTransition, ref kinematica);

                return this;
            }

            return null;
        }

        // --- Climbing states wrappers ---
        void HandleMountingState(ref MotionSynthesizer synthesizer)
        {
            bool TransitionSuccess;

            if (TraverserKinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out TransitionSuccess))
            {
                if (!TransitionSuccess)
                {
                    SetState(TraverserClimbingTriggerState.Suspended);
                    return;
                }

                bool freeClimbing = false; // ledgeDistance >= 0.1f;

                // --- Get ledge anchor point from root motion transform ---
                float3 rootPosition = synthesizer.WorldRootTransform.t;
                ledgeAnchor = ledgeGeometry.GetAnchor(rootPosition);
                float ledgeDistance = math.length(rootPosition - ledgeGeometry.GetPosition(ledgeAnchor));

                if (ledgeDistance >= 0.3f)
                    freeClimbing = true;

                // --- Depending on how far the anchor is, decide if we are hanging onto a ledge or climbing a wall ---
                Climbing climbingTrait = freeClimbing ? Climbing.Create(Climbing.Type.Wall) : Climbing.Create(Climbing.Type.Ledge);

                if (freeClimbing)
                {
                    wallAnchor = wallGeometry.GetAnchor(synthesizer.WorldRootTransform.t); // rootposition
                    SetState(TraverserClimbingTriggerState.FreeClimbing);
                }
                else
                {
                    SetState(TraverserClimbingTriggerState.Climbing); // we are hanging onto a ledge 
                }

                SetClimbingState(TraverserClimbingState.Idle);
                PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
            }
        }

        void HandleClimbingState(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            bool bTransitionSucceeded;
            TraverserKinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out bTransitionSucceeded);

            // --- Handle special corner transitions ---
            if (climbingState == TraverserClimbingState.CornerRight
                || climbingState == TraverserClimbingState.CornerLeft
                )
            {
                if (bTransitionSucceeded)
                {
                    ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);
                    SetClimbingState(TraverserClimbingState.None);
                }

                return;
            }

            UpdateClimbing(ref synthesizer, deltaTime);

            TraverserClimbingState desiredState = GetDesiredClimbingState();

            if (desiredState == lastCollidingClimbingState)
                desiredState = TraverserClimbingState.Idle;

            // --- Handle ledge climbing/movement direction ---
            if (!IsClimbingState(desiredState) || bTransitionSucceeded)
            {
                Climbing climbingTrait = Climbing.Create(Climbing.Type.Ledge);

                if (desiredState == TraverserClimbingState.Idle)
                {
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                }
                else if (desiredState == TraverserClimbingState.Right)
                {
                    Direction direction = Direction.Create(Direction.Type.Right);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == TraverserClimbingState.Left)
                {
                    Direction direction = Direction.Create(Direction.Type.Left);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == TraverserClimbingState.CornerRight)
                {
                    Direction direction = Direction.Create(Direction.Type.CornerRight);

                    // --- We fake a play and check if at the segment's end there is a collision ---
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));

                    // --- If a collision is found play idle ---
                    if (!TraverserKinematicaLayer.IsCurrentAnimationEndValid(ref synthesizer))
                    {
                        SetClimbingState(TraverserClimbingState.Idle);
                        PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                        Debug.Log("No sequences for right corner transition in climbing, collision found");
                        return;
                    }
                }
                else if (desiredState == TraverserClimbingState.CornerLeft)
                {
                    Direction direction = Direction.Create(Direction.Type.CornerLeft);

                    // --- We fake a play and check if at the segment's end there is a collision ---
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));

                    // --- If a collision is found play idle ---
                    if (!TraverserKinematicaLayer.IsCurrentAnimationEndValid(ref synthesizer))
                    {                     
                        SetClimbingState(TraverserClimbingState.Idle);
                        PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                        Debug.Log("No sequences for left corner transition in climbing, collision found");
                        return;
                    }
                }

                SetClimbingState(desiredState);
            }

            AffineTransform rootTransform = synthesizer.WorldRootTransform;
            wallGeometry.Initialize(rootTransform);
            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);
            float height = wallGeometry.GetHeight(ref wallAnchor);
            bool closeToDrop = math.abs(height - 2.8f) <= 0.095f;

            RaycastHit ray_hit;
            // --- Check if the ray hits a collider, allow dismount if true ---
            if (Physics.Raycast(synthesizer.WorldRootTransform.t - synthesizer.WorldRootTransform.Forward, Vector3.down, out ray_hit, 2.8f, TraverserCollisionLayer.EnvironmentCollisionMask))
                closeToDrop = true;

            float2 stickInput = TraverserInputLayer.GetStickInput();

            // --- React to pull up/dismount ---
            if (TraverserInputLayer.capture.pullUpButton && !TraverserCollisionLayer.IsCharacterCapsuleColliding(transform.position, ref capsule))
            {
                AffineTransform contactTransform = ledgeGeometry.GetTransform(ledgeAnchor);
                RequestTransition(ref synthesizer, contactTransform, Ledge.Type.PullUp);
                SetState(TraverserClimbingTriggerState.PullUp);
            }
            else if (closeToDrop && TraverserInputLayer.capture.dismountButton)
            {
                Ledge trait = Ledge.Create(Ledge.Type.Dismount); // temporal
                PlayFirstSequence(synthesizer.Query.Where("Ledge", trait).Except(Idle.Default)); // temporal
                SetState(TraverserClimbingTriggerState.Dismount);
            }
            else if (!closeToDrop && -stickInput.y < -0.5)
            {
                SetState(TraverserClimbingTriggerState.FreeClimbing);
            }
        }

        void HandleFreeClimbingState(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            bool bTransitionSucceeded;
            TraverserKinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out bTransitionSucceeded);

            // --- Handle special corner transitions ---
            if (climbingState == TraverserClimbingState.CornerRight
                || climbingState == TraverserClimbingState.CornerLeft)
            {

                if (bTransitionSucceeded)
                {
                    ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);

                    RaycastHit ray_hit;

                    // --- Check if the ray hits a collider ---
                    if (Physics.Raycast(synthesizer.WorldRootTransform.t - synthesizer.WorldRootTransform.Forward*0.5f, synthesizer.WorldRootTransform.Forward, out ray_hit, 2, TraverserCollisionLayer.EnvironmentCollisionMask))
                    {
                        wallGeometry.Initialize(ray_hit.collider as BoxCollider, synthesizer.WorldRootTransform);
                    }

                    //Debug.DrawRay(synthesizer.WorldRootTransform.t - synthesizer.WorldRootTransform.Forward*0.5f, synthesizer.WorldRootTransform.Forward, Color.red,60);

                    SetClimbingState(TraverserClimbingState.None);
                }

                return;
            }

            // --- We are climbing a wall ---
            UpdateFreeClimbing(ref synthesizer, deltaTime);

            TraverserClimbingState desiredState = GetDesiredFreeClimbingState();

            if (!IsClimbingState(desiredState) || bTransitionSucceeded)
            {
                // --- Handle wall climbing state ---
                
                Climbing climbingTrait = Climbing.Create(Climbing.Type.Wall);

                if (desiredState == TraverserClimbingState.Right
                    || desiredState == TraverserClimbingState.Left
                    || desiredState == TraverserClimbingState.CornerRight
                    || desiredState == TraverserClimbingState.CornerLeft)
                    climbingTrait.type = Climbing.Type.Ledge;

                if (desiredState == TraverserClimbingState.Idle)
                {
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                }
                else
                {
                    // Tip: Since Direction and ClimbingState enums follow the same order, we can cast from one to the other, avoiding a switch
                    Direction direction = Direction.Create((Direction.Type)desiredState - 1);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));

                    if (desiredState == TraverserClimbingState.CornerRight
                    || desiredState == TraverserClimbingState.CornerLeft)
                    {
                        // --- We fake a play and check if at the segment's end there is a collision ---

                        // --- If a collision is found play idle ---
                        if (!TraverserKinematicaLayer.IsCurrentAnimationEndValid(ref synthesizer))
                        {
                            SetClimbingState(TraverserClimbingState.Idle);
                            PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                            Debug.Log("No sequences for corner transition in free climbing, collision found");
                            return;
                        }
                    }
                }

                SetClimbingState(desiredState);
            }

            float height = wallGeometry.GetHeight(ref wallAnchor);
            float totalHeight = wallGeometry.GetHeight();
            bool closeToLedge = math.abs(totalHeight - height) <= 0.095f;
            bool closeToDrop = math.abs(height - 2.8f) <= 0.095f;

            float2 stickInput = TraverserInputLayer.GetStickInput();

            // --- Check if we are close to hanging onto a ledge or almost on the ground ---
            if (closeToLedge && -stickInput.y > 0.5f)
            {
                ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t); // rootPosition
                SetState(TraverserClimbingTriggerState.Climbing);
            }
            if (closeToDrop && TraverserInputLayer.capture.dismountButton)
            {
                Ledge trait = Ledge.Create(Ledge.Type.Dismount); // temporal
                PlayFirstSequence(synthesizer.Query.Where("Ledge", trait).Except(Idle.Default)); // temporal
                SetState(TraverserClimbingTriggerState.Dismount);
            }
        }

        void HandleDismountState(ref MotionSynthesizer synthesizer)
        {
            bool bTransitionSucceeded;
            TraverserKinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out bTransitionSucceeded);

            if (bTransitionSucceeded)
            {
                SetState(TraverserClimbingTriggerState.Suspended);
                PlayFirstSequence(synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default));
            }
        }

        void HandlePullUpState(ref MotionSynthesizer synthesizer)
        {
            bool bTransitionSucceeded;

            if (TraverserKinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out bTransitionSucceeded))
                SetState(TraverserClimbingTriggerState.Suspended);
        }

        void HandleDropDownState(ref MotionSynthesizer synthesizer)
        {
            if (!anchoredTransition.isValid && previousState != TraverserClimbingTriggerState.DropDown)
            {
                BoxCollider collider = controller.current.ground.GetComponent<BoxCollider>();

                if (collider != null)
                {
                    if (collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    {
                        ledgeGeometry.Initialize(collider);
                        ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);
                        wallGeometry.Initialize(collider, ledgeGeometry.GetTransform(ledgeAnchor));

                        // --- Make sure we build the contact transform considering the wall's normal ---
                        AffineTransform contactTransform = ledgeGeometry.GetTransformGivenNormal(ledgeAnchor, wallGeometry.GetNormalWorldSpace());
                        RequestTransition(ref synthesizer, contactTransform, Ledge.Type.DropDown);
                    }
                }
            }

            bool TransitionSuccess;

            if (TraverserKinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out TransitionSuccess))
            {
                if(!TransitionSuccess)
                {
                    SetState(TraverserClimbingTriggerState.Suspended);
                    return;
                }

                // --- From the top of a wall, drop down onto the ledge ---
                ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);

                // --- Depending on how far the anchor is, decide if we are hanging onto a ledge or climbing a wall ---
                SetState(TraverserClimbingTriggerState.Climbing);
                SetClimbingState(TraverserClimbingState.Idle);
                Climbing climbingTrait = Climbing.Create(Climbing.Type.Ledge);
                PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
            }
        }

        bool UpdateCollidingClimbingState(float desiredMoveOnLedge, float3 desiredPosition, float3 desiredForward)
        {
            bool bCollision = TraverserCollisionLayer.IsCharacterCapsuleColliding(desiredPosition - math.normalize(desiredForward) * 0.5f - new float3(0.0f, 1.5f, 0.0f), ref capsule);

            if (climbingState == TraverserClimbingState.Idle)
            {
                lastCollidingClimbingState = TraverserClimbingState.None;
            }
            else if (bCollision)
            {
                float currentMoveDirection = climbingState == TraverserClimbingState.Left ? 1.0f : -1.0f;

                if (currentMoveDirection * desiredMoveOnLedge > 0.0f)
                    lastCollidingClimbingState = climbingState;
            }

            return bCollision;
        }

        void UpdateClimbing(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //
            AffineTransform deltaTransform = synthesizer.GetTrajectoryDeltaTransform(deltaTime);
            AffineTransform rootTransform = synthesizer.WorldRootTransform * deltaTransform;
            float linearDisplacement = -deltaTransform.t.x;

            TraverserLedgeObject.LedgeAnchor desiredLedgeAnchor = ledgeGeometry.UpdateAnchor(ledgeAnchor, linearDisplacement);
            float3 position = ledgeGeometry.GetPosition(desiredLedgeAnchor);
            float3 desiredForward = ledgeGeometry.GetNormal(desiredLedgeAnchor);

            // --- Update current anchor if it is still on the ledge ---
            if (!UpdateCollidingClimbingState(linearDisplacement, position, desiredForward))
                ledgeAnchor = desiredLedgeAnchor;

            float distance = math.length(rootTransform.t - position);

            if (distance >= 0.01f)
            {
                float3 normal = math.normalize(position - rootTransform.t);
                rootTransform.t += normal * 0.5f * deltaTime;
            }

            rootTransform.t = position;

            float angle;
            float3 currentForward = Missing.zaxis(rootTransform.q);
            quaternion q = Missing.forRotation(currentForward, desiredForward);
            float maximumAngle = math.radians(90.0f) * deltaTime;
            float3 axis = Missing.axisAngle(q, out angle);
            angle = math.min(angle, maximumAngle);
            rootTransform.q = math.mul(quaternion.AxisAngle(axis, angle), rootTransform.q);

            // --- Update root motion transform ---
            synthesizer.WorldRootTransform = rootTransform;

            ledgeGeometry.DebugDraw();
            ledgeGeometry.DebugDraw(ref ledgeAnchor);
        }

        void UpdateFreeClimbing(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //

            // --- Update wall climbing ---
            AffineTransform deltaTransform = synthesizer.GetTrajectoryDeltaTransform(deltaTime);
            AffineTransform rootTransform = synthesizer.WorldRootTransform * deltaTransform;

            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);
            float y = 1.0f - (2.8f / wallGeometry.GetHeight());
            wallAnchor.y = math.min(y, wallAnchor.y);

            float3 position = wallGeometry.GetPosition(wallAnchor);
            float distance = math.length(rootTransform.t - position);

            if (distance >= 0.01f)
            {
                float3 normal = math.normalize(position - rootTransform.t);
                rootTransform.t += normal * 0.5f * deltaTime;
            }

            rootTransform.t = position;

            float angle;
            float3 currentForward = Missing.zaxis(rootTransform.q);
            float3 desiredForward = -wallGeometry.GetNormalWorldSpace();

            quaternion q = Missing.forRotation(currentForward, desiredForward);
            float maximumAngle = math.radians(90.0f) * deltaTime;
            float3 axis = Missing.axisAngle(q, out angle);
            angle = math.min(angle, maximumAngle);
            rootTransform.q = math.mul(quaternion.AxisAngle(axis, angle), rootTransform.q);

            rootTransform *= deltaTransform.inverse();
            rootTransform.q = math.normalize(rootTransform.q);

            // --- Update root motion transform ---
            synthesizer.WorldRootTransform = rootTransform;

            wallGeometry.DebugDraw();
            wallGeometry.DebugDraw(ref wallAnchor);

            ledgeGeometry.DebugDraw();
            ledgeGeometry.DebugDraw(ref ledgeAnchor);
        }

        public TraverserAbility OnPostUpdate(float deltaTime)
        {
            return null;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            // --- If we make contact with a climbable surface and player issues climb order, mount ---
            if (TraverserInputLayer.capture.mountButton)
            {
                if (IsState(TraverserClimbingTriggerState.Suspended))
                {
                    BoxCollider collider = controller.current.collider as BoxCollider;

                    // --- Ensure we are not falling ---

                    if (collider != null && controller.IsGrounded)
                    {
                        if (collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        {
                            auxledgeGeometry.Initialize(collider);
                            TraverserLedgeObject.LedgeAnchor auxAnchor = auxledgeGeometry.GetAnchor(contactTransform.t);
                            bool left = false;
                            float distance = auxledgeGeometry.GetDistanceToClosestVertex(contactTransform.t, auxledgeGeometry.GetNormal(auxAnchor), ref left);

                            // --- Make sure we are not too close to the corner (We may trigger an unwanted transition afterwards) ---
                            if (!collider.Equals(controller.current.ground.GetComponent<BoxCollider>()) && distance > 0.25)
                            {
                                ledgeGeometry.Initialize(collider);
                                wallGeometry.Initialize(collider, contactTransform);
                          
                                RequestTransition(ref synthesizer, contactTransform, Ledge.Type.Mount);
                                SetState(TraverserClimbingTriggerState.Mounting);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            bool ret = false;


            if (TraverserInputLayer.capture.dropDownButton && !IsState(TraverserClimbingTriggerState.DropDown))
            {
                SetState(TraverserClimbingTriggerState.DropDown);
                ret = true;
            }

            return ret;
        }

        public void RequestTransition(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, Ledge.Type type)
        {
            // --- Require transition animation of the type given ---
            ref Binary binary = ref synthesizer.Binary; 
            Ledge trait = Ledge.Create(type);

            SegmentCollisionCheck collisionCheck = SegmentCollisionCheck.AboveGround | SegmentCollisionCheck.InsideGeometry;

            // --- Prevent collision checks ---
            if (type == Ledge.Type.DropDown || type == Ledge.Type.Mount)
            {
                collisionCheck &= ~SegmentCollisionCheck.InsideGeometry;
                collisionCheck &= ~SegmentCollisionCheck.AboveGround;
            }

            QueryResult sequence = TagExtensions.GetPoseSequence(ref binary, contactTransform, 
                trait, trait, contactThreshold, collisionCheck);

            if (sequence.length == 0)
            {
                Debug.Log("No sequences in climbing");
                return;
            }

            bool rootadjust = trait.type == Ledge.Type.PullUp ? false : true;

            anchoredTransition.Dispose();
            anchoredTransition = AnchoredTransitionTask.Create(ref synthesizer,
                    sequence, contactTransform, maximumLinearError,
                        maximumAngularError, rootadjust);
        }

        // --------------------------------

        // --- Utilities ---    

        public void SetState(TraverserClimbingTriggerState newState)
        {
            previousState = state;
            state = newState;
            lastCollidingClimbingState = TraverserClimbingState.None;
        }

        public bool IsState(TraverserClimbingTriggerState queryState)
        {
            return state == queryState;
        }

        public void SetClimbingState(TraverserClimbingState climbingState)
        {
            previousClimbingState = this.climbingState;
            this.climbingState = climbingState;
        }

        public bool IsClimbingState(TraverserClimbingState climbingState)
        {
            return this.climbingState == climbingState;
        }
        public void PlayFirstSequence(PoseSet poses) 
        {
            kinematica.Synthesizer.Ref.PlayFirstSequence(poses);
            poses.Dispose();
        }

        TraverserClimbingState GetDesiredFreeClimbingState()
        {
            float2 stickInput = TraverserInputLayer.GetStickInput();
            stickInput.y = -stickInput.y; // negate y, so positive y means going up

            // --- Depending on stick input, decide climbing direction ---
            if (math.length(stickInput) >= 0.1f)
            {
                if (stickInput.x > 0.3f && stickInput.y > 0.3f)
                {
                    return TraverserClimbingState.UpRight;
                }
                else if (stickInput.x < -0.3f && stickInput.y > 0.3f)
                {
                    return TraverserClimbingState.UpLeft;
                }
                else if (stickInput.x > 0.3f && stickInput.y < -0.3f)
                {
                    return TraverserClimbingState.DownRight;
                }
                else if (stickInput.x < -0.3f && stickInput.y < -0.3f)
                {
                    return TraverserClimbingState.DownLeft;
                }
                else if (stickInput.x > 0.5f)
                {
                    bool left = false;

                    // --- Use ledge definition to determine how close we are to the edges of the wall ---
                    float distance = ledgeGeometry.GetDistanceToClosestVertex(kinematica.Synthesizer.Ref.WorldRootTransform.t, ledgeGeometry.GetNormal(ledgeAnchor), ref left);

                    if (!left && distance < 0.1f)
                        return TraverserClimbingState.CornerRight;

                    return TraverserClimbingState.Right;
                }
                else if (stickInput.x < -0.5f)
                {
                    bool left = false;

                    // --- Use ledge definition to determine how close we are to the edges of the wall ---
                    float distance = ledgeGeometry.GetDistanceToClosestVertex(kinematica.Synthesizer.Ref.WorldRootTransform.t, ledgeGeometry.GetNormal(ledgeAnchor), ref left);

                    if (left && distance < 0.1f)
                        return TraverserClimbingState.CornerLeft;

                    return TraverserClimbingState.Left;
                }
                else if (stickInput.y < -0.5f)
                {
                    return TraverserClimbingState.Down;
                }
                else if (stickInput.y > 0.5f)
                {
                    return TraverserClimbingState.Up;
                }

            }

            return TraverserClimbingState.Idle;
        }

        TraverserClimbingState GetDesiredClimbingState() 
        {
            float2 stickInput = TraverserInputLayer.GetStickInput();

            // --- Use ledge definition to determine how close we are to the edges of the wall, also at which side the vertex is (bool left) ---
            bool left = false;
            float distance = ledgeGeometry.GetDistanceToClosestVertex(kinematica.Synthesizer.Ref.WorldRootTransform.t, ledgeGeometry.GetNormal(ledgeAnchor), ref left);

            if (stickInput.x > 0.5f)
            {
                if (!left && distance < 0.1f)
                    return TraverserClimbingState.CornerRight;

                return TraverserClimbingState.Right;
            }

            else if (stickInput.x < -0.5f)
            {
                if (left && distance < 0.1f)
                    return TraverserClimbingState.CornerLeft;

                return TraverserClimbingState.Left;
            }
            

            return TraverserClimbingState.Idle;
        }

        // --------------------------------
    }
}

