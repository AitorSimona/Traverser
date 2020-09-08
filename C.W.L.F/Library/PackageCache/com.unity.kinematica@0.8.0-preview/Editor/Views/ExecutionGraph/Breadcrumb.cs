using System;
using JetBrains.Annotations;
using Unity.Kinematica.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class Breadcrumb : VisualElement
    {
        [UsedImplicitly]
        internal new class UxmlFactory : UxmlFactory<Breadcrumb> {}

        class BreadcrumbItem : VisualElement
        {
            public Label Label;
            Action m_ClickedEvent;

            public BreadcrumbItem(VisualTreeAsset template, Action clickedEvent)
            {
                m_ClickedEvent = clickedEvent;
                template.CloneTree(this);
                AddToClassList("breadcrumbItem");

                Label = this.Q<Label>("breadcrumbLabel");
                this.AddManipulator(new Clickable(OnClick));
                AddToClassList("breadcrumbButton");
            }

            void OnClick()
            {
                m_ClickedEvent?.Invoke();
            }

            public void Configure(string newLabel, Action clickedEvent)
            {
                if (Label.text != newLabel)
                    Label.text = newLabel;
                m_ClickedEvent = clickedEvent;
            }
        }

        VisualTreeAsset m_Asset;
        int m_ItemCount;

        readonly string styleSheet = "Breadcrumb.uss";

        public Breadcrumb()
        {
            m_Asset = UIElementsUtils.LoadTemplate("Breadcrumb.uxml");

            UIElementsUtils.ApplyStyleSheet(styleSheet, this);

            AddToClassList("breadcrumb");
            RegisterCallback<GeometryChangedEvent>(e => { UpdateElementPositions(); });
        }

        public void CreateOrUpdateItem(int index, string itemLabel, Action clickedEvent)
        {
            if (index >= m_ItemCount)
                PushItem(itemLabel, clickedEvent);
            else if (ElementAt(index) is BreadcrumbItem item)
                item.Configure(itemLabel, clickedEvent);
        }

        public void TrimItems(int countToKeep)
        {
            while (m_ItemCount > countToKeep)
                PopItem();
        }

        public void PushItem(string label, Action clickedEvent = null)
        {
            BreadcrumbItem breadcrumbItem = new BreadcrumbItem(m_Asset, clickedEvent);
            breadcrumbItem.Label.text = label;
            breadcrumbItem.EnableInClassList("first", m_ItemCount == 0);
            Insert(m_ItemCount, breadcrumbItem);
            m_ItemCount++;
        }

        public void PopItem()
        {
            m_ItemCount--;
            RemoveAt(m_ItemCount);
        }

        public new void Clear()
        {
            m_ItemCount = 0;
            base.Clear();
        }

        void UpdateElementPositions()
        {
            for (int i = 0; i < childCount; i++)
            {
                var element = ElementAt(i);
                element.style.left = -i * element.resolvedStyle.unitySliceRight;
            }
        }
    }
}
