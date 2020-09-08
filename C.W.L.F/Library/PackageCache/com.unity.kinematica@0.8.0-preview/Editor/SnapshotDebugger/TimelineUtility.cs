using System;

namespace Unity.SnapshotDebugger.Editor
{
    static class TimelineUtility
    {
        //TODO - duplicated in Unity.Kinematica.Editor.TimelineUtility.
        public static string TimeToTimeFrameStr(float time, int frameRate)
        {
            int frameDigits = frameRate != 0 ? (frameRate - 1).ToString().Length : 1;
            return time.ToString("0.00") + ":" + ((int)Math.Abs(time * frameRate)).ToString().PadLeft(frameDigits, '0');
        }
    }
}
