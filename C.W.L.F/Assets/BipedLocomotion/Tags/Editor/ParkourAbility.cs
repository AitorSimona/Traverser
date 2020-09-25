using Unity.Collections;
using Unity.Jobs;
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
            return null;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {

            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {

            return false;
        }
    }
}
