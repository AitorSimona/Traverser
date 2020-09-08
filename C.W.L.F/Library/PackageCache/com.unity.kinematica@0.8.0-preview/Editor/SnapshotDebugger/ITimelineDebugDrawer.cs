using System;
using UnityEngine;

namespace Unity.SnapshotDebugger.Editor
{
    public interface ITimelineDebugDrawer
    {
        Type AggregateType { get; }

        float GetDrawHeight(IFrameAggregate aggregate);

        void Draw(FrameDebugProviderInfo providerInfo, IFrameAggregate aggregate, TimelineWidget.DrawInfo drawInfo);

        void OnPostDraw();
    }
}
