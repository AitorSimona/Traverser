using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public partial class TraverserCharacterController : MonoBehaviour
{
    public struct TraverserCollision
    {
        // The collider we just made contact with
        public Collider collider;

        // Sattes if we are currently colliding
        public bool isColliding;

        // The point at which we collided with the current collider
        public float3 colliderContactPoint;

        // The current collider's normal direction 
        public float3 colliderContactNormal;

        // Transform of the current ground, the object below the character
        public Transform ground;

        internal static TraverserCollision Create()
        {
            return new TraverserCollision()
            {
                collider = null,
                isColliding = false,
                colliderContactNormal = float3.zero,
                colliderContactPoint = float3.zero,
                ground = null
            };
        }
    }

    public TraverserCollision previousCollision;
    public TraverserCollision currentCollision;

    public CharacterController characterController;

    // Start is called before the first frame update
    void Start()
    {
        previousCollision = TraverserCollision.Create();
        currentCollision = TraverserCollision.Create();
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
