using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace UnityEngine.Animations.Rigging
{
    internal static class RigBuilderUtils
    {
        public struct PlayableChain
        {
            public string name;
            public Playable[] playables;

            public bool IsValid() => playables != null && playables.Length > 0;
        }

        private static readonly ushort k_AnimationOutputPriority = 1000;

        public static Playable[] BuildRigPlayables(PlayableGraph graph, IRigLayer layer)
        {
            if (layer == null || layer.jobs == null || layer.jobs.Length == 0)
                return null;

            var count = layer.jobs.Length;
            var playables = new Playable[count];
            for (int i = 0; i < count; ++i)
            {
                var binder = layer.constraints[i].binder;
                playables[i] = binder.CreatePlayable(graph, layer.jobs[i]);
            }

            // Connect rig playables serially
            for (int i = 1; i < count; ++i)
                playables[i].AddInput(playables[i - 1], 0, 1);

            return playables;
        }

        public static IEnumerable<PlayableChain> BuildPlayables(Animator animator, PlayableGraph graph, IList<IRigLayer> layers, SyncSceneToStreamLayer syncSceneToStreamLayer)
        {
            var playableChains = new PlayableChain[layers.Count + 1];

            // Create all rig layers
            int index = 1;
            foreach (var layer in layers)
            {
                var chain = new PlayableChain();
                chain.name = layer.name;

                if (layer.Initialize(animator))
                    chain.playables = BuildRigPlayables(graph, layer);

                playableChains[index++] = chain;
            }

            // Create sync to stream job with all rig references
            if (syncSceneToStreamLayer.Initialize(animator, layers) && syncSceneToStreamLayer.IsValid())
            {
                var chain = new PlayableChain();

                chain.name = "syncSceneToStream";
                chain.playables = new Playable[1] {RigUtils.syncSceneToStreamBinder.CreatePlayable(graph, syncSceneToStreamLayer.job)};

                playableChains[0] = chain;
            }

            return playableChains;
        }

        public static PlayableGraph BuildPlayableGraph(Animator animator, IList<IRigLayer> layers, SyncSceneToStreamLayer syncSceneToStreamLayer)
        {
            string graphName = animator.gameObject.transform.name + "_Rigs";
            PlayableGraph graph = PlayableGraph.Create(graphName);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            IEnumerable<PlayableChain> playableChains = BuildPlayables(animator, graph, layers, syncSceneToStreamLayer);

            foreach(var chain in playableChains)
            {
                if (!chain.IsValid())
                    continue;

                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, String.Format("%1-Output", chain.name), animator);
                output.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
                output.SetSortingOrder(k_AnimationOutputPriority);

                // Connect last rig playable to output
                output.SetSourcePlayable(chain.playables[chain.playables.Length - 1]);
            }

            return graph;
        }
    }
}

