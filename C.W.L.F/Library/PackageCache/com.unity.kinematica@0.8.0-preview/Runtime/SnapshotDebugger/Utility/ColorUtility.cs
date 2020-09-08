using UnityEngine;

namespace Unity.SnapshotDebugger
{
    internal static class ColorUtility
    {
        public static Color FromHtmlString(string html)
        {
            Color result;
            if (UnityEngine.ColorUtility.TryParseHtmlString(html, out result))
            {
                return result;
            }

            return Color.black;
        }
    }
}
