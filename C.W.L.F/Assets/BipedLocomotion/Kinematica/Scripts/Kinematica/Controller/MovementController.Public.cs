using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.SnapshotDebugger;

public partial class MovementController : SnapshotProvider
{
    /// <summary>
    /// Determines whether or not the movement controller is enabled. 
    /// </summary>
    /// <value>Whether the movement controller is enabled.</value>
    /// <remarks>
    /// All movement requests will be ignored if the movement controller is disabled.
    /// </remarks>
    /// <seealso cref="Tick"/>
    public bool IsEnabled
    {
        get => isEnabled;

        set
        {
            if (value != isEnabled)
            {
                if (isEnabled = value)
                {
                    state.previous.ground = state.current.ground;
                    state.previous.position = state.current.position;

                    InitializeSupport(Position);
                }
            }
        }
    }

    /// <summary>
    /// Determines the current world space position of the movement controller. 
    /// </summary>
    /// <value>Current world space position of the movement controller.</value>
    /// <remarks>
    /// Writing to this property essentially instantaneously changes the world
    /// space position of the movement controller without checking for
    /// collisions, i.e. it teleports the movement controller to a new location.
    /// </remarks>
    public float3 Position
    {
        get => state.current.position;
        set => state.current.position = value;
    }

    /// <summary>
    /// Indicates whether the movement controller is currently considered to be grounded.
    /// </summary>
    /// <value>Returns whether the movement controller is currently considered to be grounded.</value>
    /// <remarks>
    /// The movement controller is considered to be grounded if its collider is currently resting
    /// or sliding across a supporting surface.
    /// </remarks>
    /// <seealso cref="Closure.Ground"/>
    public bool IsGrounded
    {
        get => state.current.isGrounded;
    }

    /// <summary>
    /// Gives access to the current <see cref="Closure"/>.
    /// </summary>
    /// <value>Reference to the current closure.</value>
    /// <remarks>
    /// The movement controller captures information about the current and previous state
    /// of the controller itself and the environment in closures, <see cref="Closure"/>.
    /// The 'current' closure refers to the current state after <see cref="Tick"/>
    /// has been called in any given frame. The 'previous' closure always refers
    /// to the current state of the previous frame.
    /// </remarks>
    /// <seealso cref="previous"/>
    public ref Closure current
    {
        get => ref state.current;
    }

    /// <summary>
    /// Gives access to the previous <see cref="Closure"/>.
    /// </summary>
    /// <value>Reference to the previous closure.</value>
    /// <remarks>
    /// The movement controller captures information about the current and previous state
    /// of the controller itself and the environment in closures, <see cref="Closure"/>.
    /// The 'previous' closure refers to the current state before <see cref="Tick"/>
    /// had been called in any given frame.
    /// </remarks>
    /// <seealso cref="current"/>
    public ref Closure previous
    {
        get => ref state.previous;
    }

    /// <summary>
    /// This function is called when the movement controller becomes enabled and active.
    /// </summary>
    /// <remarks>
    /// This function is called from Unity when the movement controller becomes
    /// enabled and active. It initializes its internal state here based on the
    /// current transform of the game object.
    /// </remarks>
    /// <seealso cref="OnDisable"/>
    public override void OnEnable()
    {
        base.OnEnable();

        state = State.Create();
        snapshotState = State.Create();

        float3 position = transform.position;

        InitializeSupport(position);

        state.current.position = position;

        state.previous.ground = state.current.ground;
        state.previous.position = position;
    }

    /// <summary>
    /// This function is called when the movement controller becomes disabled.
    /// </summary>
    /// <seealso cref="OnEnable"/>
    public override void OnDisable()
    {
        base.OnDisable();

        state.Dispose();
        snapshotState.Dispose();
    }

    /// <summary>
    /// Captures the current and previous state of the movement controller.
    /// </summary>
    /// <remarks>
    /// This function captures the current and previous state of the movement controller.
    /// Calling <see cref="Rewind"/> will restore the internal state of the
    /// movement controller to the state that was previously captured.
    /// Any modification to the state of the controller that was made between
    /// calls to <see cref="Snapshot"/> and <see cref="Rewind"/> will be reverted.
    /// <para>
    /// It should be noted that the internal state of the movement controller
    /// will only be modified when calling <see cref="Tick"/>. Methods like
    /// <see cref="Move"/> for example only express the intended movement to be
    /// executed during the next call to <see cref="Tick"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="Rewind"/>
    /// <seealso cref="Tick"/>
    /// <seealso cref="Move"/>
    public void Snapshot()
    {
        snapshotState.CopyFrom(state);
    }

    /// <summary>
    /// Restores the current and previous state of the movement controller.
    /// </summary>
    /// <remarks>
    /// This function restores the current and previous state of the
    /// movement controller that was previously captured, see <see cref="Snapshot"/>.
    /// <para>
    /// The movement controller automatically captures its internal state
    /// when it becomes enabled and active. Calling <see cref="Rewind"/> without
    /// calling <see cref="Snapshot"/> will restore the initial state of the
    /// movement controller.
    /// </para>
    /// <para>
    /// It should be noted that the internal state of the movement controller
    /// will only be modified when calling <see cref="Tick"/>. Methods like
    /// <see cref="Move"/> for example only express the intended movement to be
    /// executed during the next call to <see cref="Tick"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="Snapshot"/>
    /// <seealso cref="Tick"/>
    /// <seealso cref="Move"/>
    public void Rewind()
    {
        state.CopyFrom(snapshotState);
    }

    /// <summary>
    /// Displays a debug visualization of the movement controller's collision shape.
    /// </summary>
    /// <remarks>
    /// This function is intended to be called from client code to
    /// visualize the controller's collision shape. It can be called at
    /// any time during a given frame, i.e. it doesn't have to called
    /// during OnGUI(), it can be called during Update().
    /// </remarks>
    /// <param name="rotation">Assumed rotation of the movement controller.</param>
    /// <param name="color">Color to be used for debug visualization.</param>
    public void DebugDraw(quaternion rotation, Color color)
    {
        collisionShape.DebugDraw(Position, rotation, color);
    }

    /// <summary>
    /// This function can be called to indicate a relative world space displacement in meters.
    /// </summary>
    /// <remarks>
    /// This function can be called to define a relative world space displacement in meters.
    /// The position of the movement controller won't be affected until <see cref="Tick"/>
    /// has been called. Calling this function multiple times before calling <see cref="Tick"/>
    /// will simply override the previously defined displacement.
    /// <para>
    /// The final position of the movement controller after <see cref="Tick"/> has been
    /// called might deviate from the intended displacement since the actual movement
    /// performed will be subject to collision detection and resolution.
    /// </para>
    /// </remarks>
    /// <param name="displacement">Intended relative world space displacement in meters.</param>
    /// <seealso cref="MoveTo"/>
    /// <seealso cref="SetVelocity"/>
    /// <seealso cref="Tick"/>
    public void Move(float3 displacement)
    {
        state.desiredDisplacement += displacement;
    }

    /// <summary>
    /// This function can be called to indicate an absolute target world space position.
    /// </summary>
    /// <remarks>
    /// This function can be called to define an intended absolute world space position.
    /// The position of the movement controller won't be affected until <see cref="Tick"/>
    /// has been called. Calling this function multiple times before calling <see cref="Tick"/>
    /// will simply override the previously set target position.
    /// <para>
    /// The final position of the movement controller after <see cref="Tick"/> has been
    /// called might deviate from the intended target position since the actual movement
    /// performed will be subject to collision detection and resolution.
    /// </para>
    /// </remarks>
    /// <param name="position">Intended target world space position.</param>
    /// <seealso cref="Move"/>
    /// <seealso cref="SetVelocity"/>
    /// <seealso cref="Tick"/>
    public void MoveTo(float3 position)
    {
        Move(position - Position);
    }

    /// <summary>
    /// This function can be called to indicate a desired velocity in meters per second.
    /// </summary>
    /// <remarks>
    /// This function can be called to define a desired velocity in meters per second.
    /// The position of the movement controller won't be affected until <see cref="Tick"/>
    /// has been called. Calling this function multiple times before calling <see cref="Tick"/>
    /// will simply override the previously set velocity.
    /// <para>
    /// The final position of the movement controller after <see cref="Tick"/> has been
    /// called might deviate from the intended velocity since the actual movement
    /// performed will be subject to collision detection and resolution.
    /// </para>
    /// </remarks>
    /// <param name="velocity">Desired velocity in meters per second.</param>
    /// <seealso cref="Move"/>
    /// <seealso cref="MoveTo"/>
    /// <seealso cref="Tick"/>
    public void SetVelocity(float3 velocity)
    {
        state.desiredVelocity = velocity;
    }

    /// <summary>
    /// Calculates the closest contact point on the movement controller's collision shape.
    /// </summary>
    /// <remarks>
    /// Calculate the closest contact point on the associated collision shape.
    /// The closest point will be determined based on a relative position
    /// passed as argument, i.e. relative to the movement controller's transform.
    /// </remarks>
    /// <param name="origin">Position relative to the movement controller's transform.</param>
    /// <returns>Closest contact point on the collision shape.</returns>
    public float3 FromPosition(float3 origin)
    {
        return
            collisionShape.FromPosition(
                Position, origin);
    }

    /// <summary>
    /// Encapsulates a force that gets applied to the movement controller's transform.
    /// </summary>
    /// <remarks>
    /// Encapsulates a force in world space in newtons that act on the movement controller's transform.
    /// Forces can either be impulses (instantaneous changes in velocity) or accelerations
    /// over time.
    /// <para>
    /// Forces can be used to add an additional displacement each frame, which can
    /// be used for jumps for example. Gravity is another example of a force that
    /// can influence the overall displacement of the movement controller.
    /// </para>
    /// </remarks>
    public struct Force
    {
        /// <summary>
        /// Absolute force in world space expressed in newtons
        /// that acts on the movement controller each frame.
        /// </summary>
        public float3 value;

        /// <summary>
        /// Remaining time in seconds that this force will act on the movement controller.
        /// </summary>
        /// <remarks>
        /// A remaining duration of 0.0f will result in the force, see <see cref="value"/>,
        /// to be applied during the current frame only.
        /// </remarks>
        public float remainingTimeInSeconds;

        /// <summary>
        /// Create a new force based on the force itself and an optional duration.
        /// </summary>
        /// <remarks>
        /// This is a shorthand for `new Force()` and allows the creation of a new
        /// force based on a force in newtons and an optional duration in seconds.
        /// </remarks>
        /// <param name="force">Force in world space expressed in newstons.</param>
        /// <param name="duration">Duration in seconds, default value is 0.0f.</param>
        /// <returns>The newly created force.</returns>
        public static Force Create(float3 force, float duration = 0.0f)
        {
            return new Force
            {
                value = force,
                remainingTimeInSeconds = duration
            };
        }

        /// <summary>
        /// Returns the magnitude of the force to be applied.
        /// </summary>
        /// <value>Magnitude of the force in newtons.</value>
        public float Magnitude
        {
            get => math.length(value);
        }

        /// <summary>
        /// Returns the normalized direction of the force to be applied.
        /// </summary>
        /// <value>World space direction of the force.</value>
        public float3 Direction
        {
            get => math.normalize(value);
        }
    }

    /// <summary>
    /// Returns the accumulated velocity that acts on the movement controller.
    /// </summary>
    /// <remarks>
    /// The accumulated velocity is a result of all forces acting on the
    /// movement controller each frame.
    /// </remarks>
    /// <value>Velocity in world space in meters per second.</value>
    public float3 accumulatedVelocity
    {
        get => state.accumulatedVelocity;
    }

    /// <summary>
    /// Returns the list of forces that are applied to the movement controller.
    /// </summary>
    /// <value>List of forces that will be applied during the next <see cref="Tick"/>.</value>
    /// <seealso cref="Force"/>
    /// <seealso cref="Tick"/>
    public NativeList<Force> appliedForces
    {
        get => state.appliedForces;
    }

    /// <summary>
    /// Creates an impulse in world space that is to be applied to the movement controller.
    /// </summary>
    /// <remarks>
    /// Creates an impulse that is to be applied to the movement controller
    /// during the next <see cref="Tick"/>. The impulse is expected to be specified
    /// in world space in newtons.
    /// </remarks>
    /// <param name="impulse">World space impulse in newtons.</param>
    /// <seealso cref="Force"/>
    /// <seealso cref="Tick"/>
    public void AddImpulse(float3 impulse)
    {
        state.appliedForces.Add(Force.Create(impulse));
    }

    /// <summary>
    /// Creates a force in world space that is to be applied to the movement controller.
    /// </summary>
    /// <remarks>
    /// Creates a force that is to be applied to the movement controller. The force
    /// will be applied each frame during <see cref="Tick"/> over the specified
    /// duration in seconds. The force is expected to be specified in world space
    /// in newtons.
    /// </remarks>
    /// <param name="force">Force in world space in newtons.</param>
    /// <param name="duration">Duration in seconds.</param>
    /// <seealso cref="Force"/>
    /// <seealso cref="Tick"/>
    public void AddForce(float3 force, float duration)
    {
        state.appliedForces.Add(Force.Create(force, duration));
    }

    /// <summary>
    /// Main update function of the movement controller.
    /// </summary>
    /// <remarks>
    /// The movement controller can be used in a predictive scenario as well
    /// as on a frame-by-frame basis. To accommodate for both use cases it does
    /// not use the traditional `Update()` method but instead relies on a
    /// controlling component to manually call <see cref="Tick"/>.
    /// <para>
    /// This method applies all previously set displacements and/or forces.
    /// </para>
    /// <para>
    /// See <see cref="MovementController"/> for an example that calls
    /// <see cref="Tick"/> in a loop to perform predictive collision detection.
    /// </para>
    /// </remarks>
    /// <param name="deltaTime">Time advance for this frame in seconds.</param>
    /// <seealso cref="Move"/>
    /// <seealso cref="MoveTo"/>
    /// <seealso cref="SetVelocity"/>
    /// <seealso cref="AddImpulse"/>
    /// <seealso cref="AddForce"/>
    public void Tick(float deltaTime)
    {
        if (!isEnabled)
        {
            return;
        }

        UpdateVelocity(deltaTime);

        state.previous = state.current;
        state.current = Closure.Create();

        state.current.position = state.previous.position;

        UpdateMovement(deltaTime);
    }
}
