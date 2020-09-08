using System;
using System.Collections.Generic;

namespace Unity.SnapshotDebugger
{
    public interface IFrameDebugProvider
    {
        int GetUniqueIdentifier();

        string GetDisplayName();
    }
}
