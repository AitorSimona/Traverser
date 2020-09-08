using Unity.Collections;

namespace Unity.Kinematica
{
    public interface IMotionMatchingQuery
    {
        string DebugTitle { get; }

        NativeString64 DebugName { get; }
    }
}
