using Unity.Kinematica;
using UnityEngine;

namespace CWLF
{
    [RequireComponent(typeof(Kinematica))]
    public class Biped : MonoBehaviour
    {
        bool idle;

        private void OnEnable()
        {
            var kinematica = GetComponent<Kinematica>();
            ref var synthesizer = ref kinematica.Synthesizer.Ref;
            synthesizer.PlayFirstSequence(synthesizer.Query.Where(IdleTrait.Trait));
        }

        private void Update()
        {
            //ref var synthesizer = ref kinematica.Synthesizer.Ref;

            //if(Input.anyKeyDown)
            //{
            //    idle ^= true;

            //    if(idle)
            //    {
            //        synthesizer.PlayFirstSequence(synthesizer.Query.Where("IdleTag",Locomotio))
            //    }
            //}
        }
    }
}