using Unity.Kinematica;
using UnityEngine;

namespace HelloWorld
{
    [RequireComponent(typeof(Kinematica))]
    public class HelloWorld : MonoBehaviour
    {
        bool idle;

        void Update()
        {
            var kinematica = GetComponent<Kinematica>();

            ref var synthesizer = ref kinematica.Synthesizer.Ref;

            if (Input.anyKeyDown)
            {
                idle ^= true;

                if (idle)
                {
                    synthesizer.PlayFirstSequence(
                        synthesizer.Query.Where("Idle",
                            Locomotion.Default).And(Idle.Default));
                }
                else
                {
                    synthesizer.PlayFirstSequence(
                        synthesizer.Query.Where("Locomotion",
                            Locomotion.Default).Except(Idle.Default));
                }
            }

            if (idle)
            {
                synthesizer.LoopSegmentIfEndReached(synthesizer.Time);
            }
        }
    }
}
