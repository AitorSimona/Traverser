using System.Globalization;
using Unity.SnapshotDebugger.Editor;
using UnityEngine;
using UnityEditor;
using ColorUtility = Unity.SnapshotDebugger.ColorUtility;
using Unity.Mathematics;

public enum TimelineViewMode
{
    frames = 0,
    secondsFrames,
    seconds
}

public class TimelineWidget
{
    public static readonly Color k_MajorTickColor;
    public static readonly Color k_MinorTickColor;

    static TimelineWidget()
    {
        k_MajorTickColor = EditorGUIUtility.isProSkin ? ColorUtility.FromHtmlString("#B4B4B4") : ColorUtility.FromHtmlString("#525252");
        k_MinorTickColor = ColorUtility.FromHtmlString("#525252");
    }

    public struct DrawRangeInfo
    {
        public Rect drawRect;
        public float startTime;
        public float endTime;

        public float Duration => endTime - startTime;
    }

    public struct DrawInfo
    {
        public DrawRangeInfo layout;
        public DrawRangeInfo timeline;

        public static DrawInfo Create(Rect drawRect, float layoutStartTime, float layoutEndTime, float timelineStartTime, float timelineEndTime)
        {
            DrawInfo drawInfo = new DrawInfo()
            {
                layout = new DrawRangeInfo()
                {
                    drawRect = drawRect,
                    startTime = layoutStartTime,
                    endTime = layoutEndTime
                }
            };

            timelineStartTime = math.clamp(timelineStartTime, layoutStartTime, layoutEndTime);
            timelineEndTime = math.clamp(timelineEndTime, layoutStartTime, layoutEndTime);

            float timelineStartPos = drawInfo.GetPixelPosition(timelineStartTime);
            float timelineEndPos = drawInfo.GetPixelPosition(timelineEndTime);

            drawInfo.timeline = new DrawRangeInfo()
            {
                drawRect = new Rect(timelineStartPos, drawRect.y, timelineEndPos - timelineStartPos, drawRect.height),
                startTime = timelineStartTime,
                endTime = timelineEndTime
            };

            return drawInfo;
        }

        public float GetPixelPosition(float time)
        {
            float normalizedPosition = (time - layout.startTime) / layout.Duration;
            return layout.drawRect.x + normalizedPosition * layout.drawRect.width;
        }

        public float GetTime(float pixelPosition)
        {
            float normalizedPosition = (pixelPosition - layout.drawRect.x) / layout.drawRect.width;
            return layout.startTime + normalizedPosition * layout.Duration;
        }
    }

    public TimelineWidget()
    {
        RangeStart = 0.0f;
        RangeWidth = 10.0f;
        m_desiredRangeStart = 0.0f;
        m_desiredRangeWidth = 10.0f;
        m_selectedRangeStart = 0.0f;
        m_selectedRangeEnd = 0.0f;
    }

    public static void DrawNotations(
        DrawInfo drawInfo,
        int sampleRate = 60,
        float tickmarkHeight = 32f,
        bool above = false,
        TimelineViewMode labelFormat = TimelineViewMode.seconds)
    {
        // first figure out what step I should use
        float majorStepSize = 200.0f;

        float width = drawInfo.layout.drawRect.width;
        float height = drawInfo.layout.drawRect.height;

        float numSteps = Mathf.Ceil(width / majorStepSize);

        float scale = drawInfo.layout.Duration;
        float scaleStep = scale / numSteps;

        // Get the time step
        float modScale = scaleStep;
        float modScaleMult = 1.0f;
        while (modScale < 1.0f)
        {
            modScale *= 10.0f;
            modScaleMult *= 10.0f;
        }
        while (modScale > 10.0f)
        {
            modScale /= 10.0f;
            modScaleMult /= 10.0f;
        }

        // modify scale to be divisible by 1/2/4/5
        if (modScale > 5.0f)
        {
            modScale = 5.0f;
        }
        else if (modScale > 2.5f)
        {
            modScale = 2.5f;
        }
        else if (modScale > 2.0f)
        {
            modScale = 2.0f;
        }
        else
        {
            modScale = 1.0f;
        }

        modScale /= modScaleMult;

        // with fixed scale need to find our start point
        float startOffset = drawInfo.layout.startTime % modScale;
        float start = drawInfo.layout.startTime - startOffset;

        float rangeEnd = drawInfo.layout.startTime + scale;

        float textLineHeight = GUI.skin.font.lineHeight;

        float tickmarkHeightSmall = tickmarkHeight * 0.5f;
        float tickmarkHeightMedium = tickmarkHeight * 0.75f;
        float verticalPosition = above ? height :  height - textLineHeight;

        // draw
        for (int i = -1;; ++i)
        {
            float point = start + modScale * i;
            if (point > rangeEnd)
            {
                break;
            }

            // draw minor steps
            for (int j = 1; j < 5; ++j)
            {
                float subPoint = point + (modScale / 5) * j;

                float position = drawInfo.GetPixelPosition(subPoint);

                float heightThis = (j == 2 ? tickmarkHeightSmall : tickmarkHeightMedium);

                Rect r = new Rect(position, verticalPosition - heightThis, 1, heightThis);

                GUI.color = k_MinorTickColor;
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
            }

            // draw major step
            {
                float position = drawInfo.GetPixelPosition(point);

                Rect r = new Rect(position, verticalPosition - tickmarkHeight, 1, tickmarkHeight);

                GUI.color = k_MajorTickColor;
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);

                string label = string.Empty;
                int frame = (int)(point * sampleRate);

                switch (labelFormat)
                {
                    case TimelineViewMode.frames:
                        label = frame.ToString(CultureInfo.InvariantCulture);
                        break;
                    case TimelineViewMode.secondsFrames:
                        label = TimelineUtility.TimeToTimeFrameStr(point, sampleRate);
                        break;
                    default:
                        label = string.Format(scale < 0.1f ? "{0:0.000}" : "{0:0.00}", (float)point);
                        break;
                }

                var labelSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(label));
                var labelOffset = labelSize.y - textLineHeight;

                float x = position - labelSize.x * 0.5f;
                float y;
                if (above)
                {
                    y = 0;
                }
                else
                {
                    y = verticalPosition - labelOffset * 0.5f; // Centered position below guide line
                }

                var w = labelSize.x + 2;
                var h = labelSize.y;

                GUI.color = k_MajorTickColor;
                GUI.Label(new Rect(x, y, w, h), label);
            }
        }
    }

    public static void DrawRange(Rect r, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawLineAtTime(DrawInfo drawInfo, float timeInSeconds, Color color)
    {
        float position = drawInfo.GetPixelPosition(timeInSeconds);

        float textLineHeight = GUI.skin.font.lineHeight;

        Rect r = new Rect(position, 0.0f, 1, drawInfo.layout.drawRect.height - textLineHeight);

        GUI.color = color;
        GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawRectangle(Rect rectangle, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rectangle, EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawRectangleWithDetour(Rect rectangle, Color fillColor, Color detourColor, float detourThickness = 1.0f)
    {
        DrawRectangle(new Rect(rectangle.x - detourThickness, rectangle.y - detourThickness, rectangle.width + 2.0f * detourThickness, rectangle.height + 2.0f * detourThickness), detourColor);
        DrawRectangle(rectangle, fillColor);
    }

    public static void DrawRectangleDetour(Rect rectangle, Color detourColor, float thickness)
    {
        DrawRectangle(new Rect(rectangle.x, rectangle.y, rectangle.width, thickness), detourColor);
        DrawRectangle(new Rect(rectangle.x, rectangle.yMax - thickness, rectangle.width, thickness), detourColor);
        DrawRectangle(new Rect(rectangle.x, rectangle.y, thickness, rectangle.height), detourColor);
        DrawRectangle(new Rect(rectangle.xMax - thickness, rectangle.y, thickness, rectangle.height), detourColor);
    }

    public static void DrawLabel(Rect labelRect, string label, Color textColor)
    {
        GUI.color = textColor;
        GUI.Label(labelRect, label);
        GUI.color = Color.white;
    }

    public static void DrawLabelInsideRectangle(Rect rectangle, string label, Color color)
    {
        Vector2 labelSize = GetLabelSize(label);
        labelSize.x = Mathf.Min(rectangle.width, labelSize.x);
        labelSize.y = Mathf.Min(rectangle.height, labelSize.y);

        Rect labelRect = new Rect(rectangle.x + (rectangle.width - labelSize.x) * 0.5f, rectangle.y + (rectangle.height - labelSize.y) * 0.5f, labelSize.x, labelSize.y);

        DrawLabel(labelRect, label, color);
    }

    public static Vector2 GetLabelSize(string label)
    {
        float labelWidth = GUI.skin.GetStyle("label").CalcSize(new GUIContent(label)).x;

        int numLines = label.Split('\n').Length;
        float labelHeight = GUI.skin.font.lineHeight * numLines;

        return new Vector2(labelWidth, labelHeight);
    }

    public void Update(Rect rect, float maxEndTime)
    {
        willRepaint = false;

        var e = Event.current;

        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.ScrollWheel)
            {
                willRepaint = true;
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);

                var wheelDelta = e.alt ? -e.delta.x * 0.1f : e.delta.y;

                float currentPosition = GetDesiredPositionFromMouse(rect, e.mousePosition);
                float rangeStart = DesiredRangeStart;
                float scale = DesiredRangeWidth;

                float relativePosition = (currentPosition - rangeStart) / scale;

                float newScale = Mathf.Clamp(scale * Mathf.Pow(1.105f, wheelDelta), 0.05f, 100000.0f);

                m_desiredRangeStart = currentPosition - (relativePosition * newScale);
                m_desiredRangeWidth = currentPosition + ((1.0f - relativePosition) * newScale) - DesiredRangeStart;

                e.Use();
            }

            if ((e.button == 2 || e.button == 0 && e.alt) && e.type == EventType.MouseDown)
            {
                mouseButton2Down = true;
            }
            else if ((e.button == 2 || e.button == 0 && e.alt) && e.rawType == EventType.MouseUp)
            {
                mouseButton2Down = false;
            }

            if (mouseButton2Down)
            {
                willRepaint = true;
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                {
                    float displacement = e.delta.x * (RangeWidth / rect.width);

                    RangeStart -= displacement;
                    m_desiredRangeStart -= displacement;

                    e.Use();
                }
            }
        }

        if (maxEndTime >= 0.0f && maxEndTime > RangeEnd)
        {
            float scrollTime = maxEndTime - RangeEnd;
            RangeStart = RangeStart + scrollTime;
            m_desiredRangeStart += scrollTime;
        }

        RangeStart = Mathf.Lerp(RangeStart, m_desiredRangeStart, 0.06f);
        RangeWidth = Mathf.Lerp(RangeWidth, m_desiredRangeWidth, 0.06f);

        willRepaint |= !Mathf.Approximately(RangeStart, m_desiredRangeStart);
        willRepaint |= !Mathf.Approximately(RangeWidth, m_desiredRangeWidth);
    }

    public bool Repaint
    {
        get { return willRepaint; }
        set { willRepaint = value; }
    }

    public float GetPositionFromPoint(Rect rect, float relativePoint)
    {
        if (RangeWidth <= 0.0f)
        {
            return RangeStart;
        }

        float relativePosition = relativePoint / rect.width;
        return Mathf.Lerp(RangeStart, RangeEnd, relativePosition);
    }

    public float GetDesiredPositionFromPoint(Rect rect, float relativePoint)
    {
        if (DesiredRangeWidth <= 0.0f)
        {
            return DesiredRangeStart;
        }

        float relativePosition = relativePoint / rect.width;
        return Mathf.Lerp(DesiredRangeStart, DesiredRangeEnd, relativePosition);
    }

    public float GetCurrentPositionFromMouse(Rect rect, Vector2 mousePosition)
    {
        return GetPositionFromPoint(rect, mousePosition.x);
    }

    public float GetDesiredPositionFromMouse(Rect rect, Vector2 mousePosition)
    {
        return GetDesiredPositionFromPoint(rect, mousePosition.x);
    }

    public float RangeStart
    {
        get { return m_rangeStart; }
        set { m_rangeStart = value; }
    }

    public float RangeEnd
    {
        get { return RangeStart + RangeWidth; }
    }

    public float RangeWidth
    {
        get { return m_rangeWidth; }
        set { m_rangeWidth = value; }
    }

    public float DesiredRangeStart
    {
        get { return m_desiredRangeStart; }
        set { m_desiredRangeStart = value; }
    }

    public float DesiredRangeEnd
    {
        get { return m_desiredRangeStart + m_desiredRangeWidth; }
    }

    public float DesiredRangeWidth
    {
        get { return m_desiredRangeWidth; }
        set { m_desiredRangeWidth = value; }
    }

    public float SelectedRangeStart
    {
        get { return m_selectedRangeStart; }
        set { m_selectedRangeStart = value; }
    }

    public float SelectedRangeEnd
    {
        get { return m_selectedRangeEnd; }
        set { m_selectedRangeEnd = value; }
    }

    private float m_rangeStart;
    private float m_rangeWidth;

    private float m_desiredRangeStart;
    private float m_desiredRangeWidth;

    private float m_selectedRangeStart;
    private float m_selectedRangeEnd;

    private bool mouseButton2Down;

    private bool willRepaint;
}
