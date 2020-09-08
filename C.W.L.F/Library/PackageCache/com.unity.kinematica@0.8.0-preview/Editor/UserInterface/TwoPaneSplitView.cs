using System.Collections.Generic;
using Unity.Kinematica.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * Taken from UIBuilder. If this makes it to trunk we should use the version there instead
 */
namespace Unity.Kinematica.Editor
{
    class TwoPaneSplitView : VisualElement
    {
        static readonly string k_UssPath = "TwoPaneSplitView.uss";

        static readonly string k_UssClassName = "unity-two-pane-split-view";
        static readonly string k_ContentContainerClassName = "unity-two-pane-split-view__content-container";
        static readonly string k_HandleDragLineClassName = "unity-two-pane-split-view__dragline";
        static readonly string k_HandleDragLineVerticalClassName = k_HandleDragLineClassName + "--vertical";
        static readonly string k_HandleDragLineHorizontalClassName = k_HandleDragLineClassName + "--horizontal";
        static readonly string k_HandleDragLineAnchorClassName = "unity-two-pane-split-view__dragline-anchor";
        static readonly string k_HandleDragLineAnchorVerticalClassName = k_HandleDragLineAnchorClassName + "--vertical";
        static readonly string k_HandleDragLineAnchorHorizontalClassName = k_HandleDragLineAnchorClassName + "--horizontal";
        static readonly string k_VerticalClassName = "unity-two-pane-split-view--vertical";
        static readonly string k_HorizontalClassName = "unity-two-pane-split-view--horizontal";

        public enum Orientation
        {
            Horizontal,
            Vertical
        }

        public new class UxmlFactory : UxmlFactory<TwoPaneSplitView, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlIntAttributeDescription m_FixedPaneIndex = new UxmlIntAttributeDescription { name = "fixed-pane-index", defaultValue = 0 };
            UxmlIntAttributeDescription m_FixedPaneInitialSize = new UxmlIntAttributeDescription { name = "fixed-pane-initial-size", defaultValue = 100 };
            UxmlStringAttributeDescription m_Orientation = new UxmlStringAttributeDescription { name = "orientation", defaultValue = "horizontal" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var fixedPaneIndex = m_FixedPaneIndex.GetValueFromBag(bag, cc);
                var fixedPaneInitialSize = m_FixedPaneInitialSize.GetValueFromBag(bag, cc);
                var orientationStr = m_Orientation.GetValueFromBag(bag, cc);
                var orientation = orientationStr == "horizontal"
                    ? Orientation.Horizontal
                    : Orientation.Vertical;

                ((TwoPaneSplitView)ve).Init(fixedPaneIndex, fixedPaneInitialSize, orientation);
            }
        }

        VisualElement m_LeftPane;
        VisualElement m_RightPane;

        VisualElement m_FixedPane;
        VisualElement m_FlexedPane;

        public VisualElement fixedPane => m_FixedPane;
        public VisualElement flexedPane => m_FlexedPane;

        VisualElement m_DragLine;
        VisualElement m_DragLineAnchor;

        VisualElement m_Content;

        Orientation m_Orientation;
        int m_FixedPaneIndex;
        float m_FixedPaneInitialDimension;

        public int fixedPaneIndex => m_FixedPaneIndex;

        TwoPaneSplitViewResizer m_Resizer;

        public TwoPaneSplitView()
        {
            AddToClassList(k_UssClassName);

            UIElementsUtils.ApplyStyleSheet(k_UssPath, this);

            m_Content = new VisualElement();
            m_Content.name = "unity-content-container";
            m_Content.AddToClassList(k_ContentContainerClassName);
            hierarchy.Add(m_Content);

            // Create drag anchor line.
            m_DragLineAnchor = new VisualElement();
            m_DragLineAnchor.name = "unity-dragline-anchor";
            m_DragLineAnchor.AddToClassList(k_HandleDragLineAnchorClassName);
            hierarchy.Add(m_DragLineAnchor);

            // Create drag
            m_DragLine = new VisualElement();
            m_DragLine.name = "unity-dragline";
            m_DragLine.AddToClassList(k_HandleDragLineClassName);
            m_DragLineAnchor.Add(m_DragLine);
        }

        public TwoPaneSplitView(
            int fixedPaneIndex,
            float fixedPaneStartDimension,
            Orientation orientation) : this()
        {
            Init(fixedPaneIndex, fixedPaneStartDimension, orientation);
        }

        public void Init(int fixedPaneIndex, float fixedPaneInitialDimension, Orientation orientation)
        {
            m_Orientation = orientation;
            m_FixedPaneIndex = fixedPaneIndex;
            m_FixedPaneInitialDimension = fixedPaneInitialDimension;

            m_Content.RemoveFromClassList(k_HorizontalClassName);
            m_Content.RemoveFromClassList(k_VerticalClassName);
            if (m_Orientation == Orientation.Horizontal)
                m_Content.AddToClassList(k_HorizontalClassName);
            else
                m_Content.AddToClassList(k_VerticalClassName);

            // Create drag anchor line.
            m_DragLineAnchor.RemoveFromClassList(k_HandleDragLineAnchorHorizontalClassName);
            m_DragLineAnchor.RemoveFromClassList(k_HandleDragLineAnchorVerticalClassName);
            if (m_Orientation == Orientation.Horizontal)
                m_DragLineAnchor.AddToClassList(k_HandleDragLineAnchorHorizontalClassName);
            else
                m_DragLineAnchor.AddToClassList(k_HandleDragLineAnchorVerticalClassName);

            // Create drag
            m_DragLine.RemoveFromClassList(k_HandleDragLineHorizontalClassName);
            m_DragLine.RemoveFromClassList(k_HandleDragLineVerticalClassName);
            if (m_Orientation == Orientation.Horizontal)
                m_DragLine.AddToClassList(k_HandleDragLineHorizontalClassName);
            else
                m_DragLine.AddToClassList(k_HandleDragLineVerticalClassName);

            if (m_Resizer != null)
            {
                m_DragLineAnchor.RemoveManipulator(m_Resizer);
                m_Resizer = null;
            }

            if (m_Content.childCount != 2)
                RegisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            else
                PostDisplaySetup();
        }

        void OnPostDisplaySetup(GeometryChangedEvent evt)
        {
            if (m_Content.childCount != 2)
            {
                Debug.LogError("TwoPaneSplitView needs exactly 2 chilren.");
                return;
            }

            PostDisplaySetup();

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        void PostDisplaySetup()
        {
            if (m_Content.childCount != 2)
            {
                Debug.LogError("TwoPaneSplitView needs exactly 2 children.");
                return;
            }

            m_LeftPane = m_Content[0];
            if (m_FixedPaneIndex == 0)
            {
                m_FixedPane = m_LeftPane;
                if (m_Orientation == Orientation.Horizontal)
                    m_LeftPane.style.width = m_FixedPaneInitialDimension;
                else
                    m_LeftPane.style.height = m_FixedPaneInitialDimension;
            }
            else
            {
                m_FlexedPane = m_LeftPane;
            }

            m_RightPane = m_Content[1];
            if (m_FixedPaneIndex == 1)
            {
                m_FixedPane = m_RightPane;
                if (m_Orientation == Orientation.Horizontal)
                    m_RightPane.style.width = m_FixedPaneInitialDimension;
                else
                    m_RightPane.style.height = m_FixedPaneInitialDimension;
            }
            else
            {
                m_FlexedPane = m_RightPane;
            }

            m_FixedPane.style.flexShrink = 0;
            m_FixedPane.style.flexGrow = 0;
            m_FlexedPane.style.flexGrow = 1;
            m_FlexedPane.style.flexShrink = 0;
            m_FlexedPane.style.flexBasis = 0;

            if (m_Orientation == Orientation.Horizontal)
            {
                if (m_FixedPaneIndex == 0)
                    m_DragLineAnchor.style.left = m_FixedPaneInitialDimension;
                else
                    m_DragLineAnchor.style.left = this.resolvedStyle.width - m_FixedPaneInitialDimension;
            }
            else
            {
                if (m_FixedPaneIndex == 0)
                    m_DragLineAnchor.style.top = m_FixedPaneInitialDimension;
                else
                    m_DragLineAnchor.style.top = this.resolvedStyle.height - m_FixedPaneInitialDimension;
            }

            int direction = 1;
            if (m_FixedPaneIndex == 0)
                direction = 1;
            else
                direction = -1;

            if (m_FixedPaneIndex == 0)
                m_Resizer = new TwoPaneSplitViewResizer(this, direction, m_Orientation);
            else
                m_Resizer = new TwoPaneSplitViewResizer(this, direction, m_Orientation);

            m_DragLineAnchor.AddManipulator(m_Resizer);

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        void OnSizeChange(GeometryChangedEvent evt)
        {
            var maxLength = this.resolvedStyle.width;
            var dragLinePos = m_DragLineAnchor.resolvedStyle.left;

            var activeElementPos = m_FixedPane.resolvedStyle.left;
            if (m_Orientation == Orientation.Vertical)
            {
                maxLength = this.resolvedStyle.height;
                dragLinePos = m_DragLineAnchor.resolvedStyle.top;
                activeElementPos = m_FixedPane.resolvedStyle.top;
            }

            if (m_FixedPaneIndex == 0 && dragLinePos > maxLength)
            {
                var delta = maxLength - dragLinePos;
                m_Resizer.ApplyDelta(delta);
            }
            else if (m_FixedPaneIndex == 1)
            {
                if (activeElementPos < 0)
                {
                    var delta = -dragLinePos;
                    m_Resizer.ApplyDelta(delta);
                }
                else
                {
                    if (m_Orientation == Orientation.Horizontal)
                        m_DragLineAnchor.style.left = activeElementPos;
                    else
                        m_DragLineAnchor.style.top = activeElementPos;
                }
            }
        }

        public override VisualElement contentContainer
        {
            get { return m_Content; }
        }
    }
}
