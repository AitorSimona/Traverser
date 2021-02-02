using UnityEngine;

using Unity.SnapshotDebugger;

/// <summary>
/// The movement controller is an alternative implementation
/// of Unity's character controller that allows to simulate
/// and rewind movement. This functionality enables predictive
/// collision detection.
/// </summary>
/// <remarks>
/// The core concept behind a character controller is that it provides
/// collision detection and resolution without relying on the physics system.
/// It allows to move a character (or object) similar to a `Transform`
/// but it prevents moving through colliders.
/// <para>
/// The movement controller takes this concept a step further by adding
/// a snapshot and rewind feature. Whenever a snapshot is being taken,
/// all subsequent movements (including their collision resolution) can
/// be rewound to the state when the snapshot had been taken. This allows
/// for movements to be made without actually having to commit to them.
/// This functionality enables the concept of predictive collision detection
/// and is used to generate a desired future trajectory that respects
/// collisions that happen in the (near) future.
/// </para>
/// <example>
/// <code>
/// var controller = GetComponent&lt;MovementController&gt;();
/// 
/// controller.Snapshot();
/// 
/// float timeHorizon = 1.0f;
/// float sampleRate = 30.0f;
/// float deltaTime = timeHorizon / sampleRate;
/// 
/// float remainingTime = timeHorizon;
/// while(remainingTime > 0.0f)
/// {
///     controller.Move(desiredVelocity * deltaTime);
///     controller.Tick(deltaTime);
///     
///     remainingTime -= deltaTime;
/// }
/// 
/// controller.Rewind();
/// </code>
/// </example>
/// </remarks>
[AddComponentMenu("Kinematica/Movement Controller")]
public partial class MovementController : SnapshotProvider
{
    /// <summary>
    /// Determines whether or not the movement controller is enabled.
    /// </summary>
    [SerializeField]
    protected bool isEnabled = true;

    /// <summary>
    /// Determines whether or not gravity is enabled.
    /// </summary>
    public bool gravityEnabled = true;

    /// <summary>
    /// Layer mask to be used when performing collision detection.
    /// The default value is the 'Default' layer (Layer 1).
    /// </summary>
    public int layerMask = 1;

    /// <summary>
    /// Determines whether or not the controller is supposed
    /// to automatically resolve ground penetration.
    /// </summary>
    public bool resolveGroundPenetration = true;

    /// <summary>
    /// Determines whether or not the controller is supposed
    /// to automatically snap to ground surfaces.
    /// The tolerance value is defined by <see cref="groundSnapDistance"/>.
    /// </summary>
    public bool groundSnap = true;

    /// <summary>
    /// Determines whether or not the controller is supposed
    /// to perform collision detection and resolution.
    /// </summary>
    public bool collisionEnabled = true;

    /// <summary>
    /// Mass of the character represented by the movement
    /// controller specified in kilograms.
    /// </summary>
    public float mass = 1.0f;

    /// <summary>
    /// Tolerance value in meters used to determine
    /// whether or not the controller is grounded.
    /// </summary>
    public float groundTolerance = 0.01f;

    /// <summary>
    /// Defines the distance between the controller's origin
    /// and the starting point of a ray used to determine
    /// whether or not the controller is grounded.
    /// See <see cref="groundProbeLength"/> for the corresponding
    /// length of the ray.
    /// </summary>
    public float groundProbeOffset = 1.0f;

    /// <summary>
    /// Defines the length of a ray used to determine
    /// whether or not the controller is grounded.
    /// See <see cref="groundProbeOffset"/> for the corresponding
    /// start offset of the ray.
    /// </summary>
    public float groundProbeLength = 3.0f;

    /// <summary>
    /// Defines the "area of support" of the controller, i.e.
    /// a circular shape centered at the controller's origin
    /// that determines whether or not the controller is grounded.
    /// </summary>
    public float groundSupport = 0.1f;

    /// <summary>
    /// This value is used if <see cref="groundSnap"/> is enabled.
    /// It maximum determines the distance between the controller
    /// and the ground for which the controller gets snapped to the ground.
    /// </summary>
    public float groundSnapDistance = 0.3f;
}
