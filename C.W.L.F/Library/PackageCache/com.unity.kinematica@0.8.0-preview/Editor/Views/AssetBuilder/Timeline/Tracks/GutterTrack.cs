using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    abstract class GutterTrack : Track
    {
        protected virtual DisplayStyle DefaultDisplay => DisplayStyle.Flex;

        protected GutterTrack(Timeline owner) : base(owner)
        {
            AddToClassList("gutterTrackElement");
            string key = GetType().Name + k_DisplayStringKey;
            string storedDisplay = EditorPrefs.GetString(key);
            if (!string.IsNullOrEmpty(storedDisplay) && int.TryParse(storedDisplay, out int intVal))
            {
                SetDisplay((DisplayStyle)intVal);
            }
            else
            {
                SetDisplay(DefaultDisplay);
            }
        }

        protected const string k_DisplayStringKey = "-displayed";

        public virtual void SetDisplay(DisplayStyle display)
        {
            EditorPrefs.SetString(GetType().Name + k_DisplayStringKey, ((int)display).ToString());
            style.display = display;
        }
    }
}
