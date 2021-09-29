using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This class is used to create Animation C# jobs handles for WeightedTransformArray.
    /// </summary>
    public class WeightedTransformArrayBinder
    {
        /// <summary>
        /// Creates an array of ReadOnlyTransformHandles representing the new bindings between the Animator and the Transforms in a WeightedTransformArray.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The component owning the WeightedTransformArray property.</param>
        /// <param name="weightedTransformArray">The WeightedTransformArray property.</param>
        /// <param name="transforms">The resulting array of ReadOnlyTransformHandles.</param>
        public static void BindReadOnlyTransforms(Animator animator, Component component, WeightedTransformArray weightedTransformArray, out NativeArray<ReadOnlyTransformHandle> transforms)
        {
            transforms = new NativeArray<ReadOnlyTransformHandle>(weightedTransformArray.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int index = 0; index < weightedTransformArray.Count; ++index)
            {
                transforms[index] = ReadOnlyTransformHandle.Bind(animator, weightedTransformArray[index].transform);
            }
        }

        /// <summary>
        /// Creates an array of ReadWriteTransformHandles representing the new bindings between the Animator and the Transforms in a WeightedTransformArray.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The component owning the WeightedTransformArray property.</param>
        /// <param name="weightedTransformArray">The WeightedTransformArray property.</param>
        /// <param name="transforms">The resulting array of ReadWriteTransformHandles.</param>
        public static void BindReadWriteTransforms(Animator animator, Component component, WeightedTransformArray weightedTransformArray, out NativeArray<ReadWriteTransformHandle> transforms)
        {
            transforms = new NativeArray<ReadWriteTransformHandle>(weightedTransformArray.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int index = 0; index < weightedTransformArray.Count; ++index)
            {
                transforms[index] = ReadWriteTransformHandle.Bind(animator, weightedTransformArray[index].transform);
            }
        }

        /// <summary>
        /// Creates an array of PropertyStreamHandle representing the new bindings between the Animator and the weights in a WeightedTransformArray.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The component owning the WeightedTransformArray property.</param>
        /// <param name="weightedTransformArray">The WeightedTransformArray property.</param>
        /// <param name="name"></param>
        /// <param name="weights"></param>
        public static void BindWeights(Animator animator, Component component, WeightedTransformArray weightedTransformArray, string name, out NativeArray<PropertyStreamHandle> weights)
        {
            weights = new NativeArray<PropertyStreamHandle>(weightedTransformArray.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int index = 0; index < weightedTransformArray.Count; ++index)
            {
                weights[index] = animator.BindStreamProperty(component.transform, component.GetType(), name + ".m_Item" + index + ".weight");
            }
        }
    }
}
