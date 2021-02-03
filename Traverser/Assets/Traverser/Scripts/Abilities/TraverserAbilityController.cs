using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]
    public class TraverserAbilityController : MonoBehaviour
    {
        // --- Attributes ---
        private TraverserAbility currentAbility;
        private CharacterController controller;
        private ControllerColliderHit lastHit;

        [Tooltip("How fast the character can move in m/s")]
        public float maxMovementSpeed = 1.0f;

        [Tooltip("How fast the character's speed will increase with given input in m/s^2")]
        public float maxMovementAcceleration = 0.1f;

        [Tooltip("How likely are we to deviate from current pose to idle, higher values make faster transitions to idle")]
        public float MovementLinearDrag = 1.0f;

        private float3 currentVelocity = Vector3.zero;

        // -------------------------------------------------

        // --- Basic methods ---

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<CharacterController>();

        }

        // Update is called once per frame
        void Update()
        {
            // --- Keep updating our current ability ---
            if (currentAbility != null)
            {
                currentAbility = currentAbility.OnUpdate(Time.deltaTime);
            }

            // --- If no ability is in control, look for one ---
            if (currentAbility == null)
            {

                // MYTODO: Order of update is important, it would be wise to add a priority to abilities,
                // instead of following the arbitrary order in which they were added as components

                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                {

                    // An ability can either return "null" or a reference to an ability.
                    // A "null" result signals that this ability doesn't require control.
                    // Otherwise the returned ability (which might be different from the
                    // one that we call "OnUpdate" on) will be the one that gains control.
                    TraverserAbility result = ability.OnUpdate(Time.deltaTime);

                    // --- If an ability asks to take control, break ---
                    if (result != null)
                    {
                        currentAbility = result;
                        break;
                    }
                }
            }

            // --- Apply drag ---
            currentVelocity -= currentVelocity*MovementLinearDrag*Time.deltaTime;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Debug.Log("Traverser: Character collided with the environment");

            lastHit = hit;
        }

        public void OnAnimatorMove()
        {
            // --- After all animations are evaluated, perform movement ---

            // --- Let abilities modify motion ---
            if (currentAbility is TraverserAbilityAnimatorMove abilityAnimatorMove)
            {
                abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            Assert.IsTrue(controller != null);

            // --- Move and update the controller ---
            float3 controllerPosition = controller.transform.position;
            float3 desiredPosition = controllerPosition + currentVelocity * Time.deltaTime;
            float3 desiredLinearDisplacement = desiredPosition - controllerPosition;
            controller.Move(desiredLinearDisplacement);

            // --- Let abilities apply final changes to motion, if needed ---
            if (currentAbility != null /*&& component.enabled*/)
                currentAbility.OnPostUpdate(Time.deltaTime);
        }

        // -------------------------------------------------

        // --- Movement ---

        public void SetMovementVelocity(float3 velocity)
        {
            currentVelocity = velocity;
        }

        public void AccelerateMovement(float3 acceleration)
        {
            if (math.length(acceleration) > maxMovementAcceleration)
                acceleration = math.normalize(acceleration) * maxMovementAcceleration;

            currentVelocity += acceleration;

            // --- Cap Velocity ---
            currentVelocity.x = math.clamp(currentVelocity.x, -maxMovementSpeed, maxMovementSpeed);
            currentVelocity.z = math.clamp(currentVelocity.z, -maxMovementSpeed, maxMovementSpeed);
            currentVelocity.y = math.clamp(currentVelocity.y, -maxMovementSpeed, maxMovementSpeed);
        }

        // -------------------------------------------------
    }
}
