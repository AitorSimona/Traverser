using System.Collections.Generic;
using System.Linq;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Kinematica.Editor
{
    class PreviewSelector : BindableElement, INotifyValueChanged<GameObject>
    {
        public new class UxmlFactory : UxmlFactory<PreviewSelector, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits {}

        const string k_DefaultLabelText = "(GameObject)";
        Image m_SelectorDropdown;
        Label m_TargetLabel;

        GameObject m_PreviewTarget;
        VisualElement m_TargetContainer;

        public PreviewSelector()
        {
            UIElementsUtils.ApplyStyleSheet("PreviewSelector.uss", this);
            AddToClassList("previewSelector");

            // Target Selection
            {
                var selectorContainer = new VisualElement();
                selectorContainer.AddToClassList("selectorContainer");
                var selectorClick = new Clickable(OnSelectorClicked);
                selectorContainer.AddManipulator(selectorClick);

                Label label = new Label { text = "Target" };
                var labelClick = new Clickable(OnSelectorClicked);
                label.AddManipulator(labelClick);

                m_SelectorDropdown = new Image();
                m_SelectorDropdown.AddToClassList("selectorDropdown");
                m_SelectorDropdown.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
                m_SelectorDropdown.RegisterCallback<DragPerformEvent>(OnDragPerform);

                selectorContainer.Add(label);
                selectorContainer.Add(m_SelectorDropdown);
                Add(selectorContainer);
            }

            //Target label
            {
                m_TargetContainer = new VisualElement();
                m_TargetContainer.AddToClassList("targetContainer");
                var containerClick = new Clickable(OnTargetClicked);
                m_TargetContainer.AddManipulator(containerClick);

                Image gameObjectIcon = new Image();
                gameObjectIcon.AddToClassList("gameObjectIcon");
                gameObjectIcon.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
                gameObjectIcon.RegisterCallback<DragPerformEvent>(OnDragPerform);

                m_TargetLabel = new Label { text = k_DefaultLabelText };
                m_TargetLabel.AddToClassList("previewLabel");
                m_TargetLabel.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
                m_TargetLabel.RegisterCallback<DragPerformEvent>(OnDragPerform);

                m_TargetContainer.Add(gameObjectIcon);
                m_TargetContainer.Add(m_TargetLabel);
                Add(m_TargetContainer);
            }

            RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.Any(obj =>
            {
                if (obj is GameObject go)
                {
                    return go.GetComponent<Animator>() != null;
                }

                return false;
            }))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            var targets = DragAndDrop.objectReferences.OfType<GameObject>().ToList();
            var target = targets.FirstOrDefault(t => t.GetComponent<Animator>() != null);
            if (target != null && target != value)
            {
                OnPreviewTargetClicked(target);
            }
        }

        void OnSelectorClicked()
        {
            var menu = new GenericMenu();

            List<GameObject> previewTargets = Object.FindObjectsOfType<GameObject>().Where(go => go.GetComponent<Animator>() != null).ToList();

            menu.AddItem(new GUIContent("None"), value == null, () => OnPreviewTargetClicked(null));

            if (previewTargets.Any())
            {
                menu.AddSeparator("");

                foreach (var target in previewTargets)
                {
                    menu.AddItem(new GUIContent($"{target.name}"), target == value, () => OnPreviewTargetClicked(target));
                }
            }

            menu.DropDown(m_SelectorDropdown.worldBound);
        }

        void OnTargetClicked()
        {
            EditorGUIUtility.PingObject(m_PreviewTarget);
        }

        void OnPreviewTargetClicked(GameObject target)
        {
            if (value == target || target == null)
            {
                value = null;
                m_TargetLabel.text = k_DefaultLabelText;
            }
            else
            {
                value = target;
                m_TargetLabel.text = target.name;
            }
        }

        public void SetValueWithoutNotify(GameObject newValue)
        {
            m_PreviewTarget = newValue;
            m_TargetLabel.text = newValue == null ? k_DefaultLabelText : newValue.name;
            UpdateTargetStyleClass();
        }

        public GameObject value
        {
            get { return m_PreviewTarget; }
            set
            {
                if (m_PreviewTarget != value)
                {
                    var previous = m_PreviewTarget;
                    m_PreviewTarget = value;
                    if (panel != null)
                    {
                        using (ChangeEvent<GameObject> evt =
                                   ChangeEvent<GameObject>.GetPooled(previous, value))
                        {
                            evt.target = this;
                            SendEvent(evt);
                        }
                    }

                    m_TargetLabel.text = m_PreviewTarget == null ? k_DefaultLabelText : m_PreviewTarget.name;

                    UpdateTargetStyleClass();
                    SceneView.RepaintAll();
                }
            }
        }

        void UpdateTargetStyleClass()
        {
            if (m_PreviewTarget == null)
            {
                m_TargetContainer.RemoveFromClassList("targetContainerActive");
            }
            else
            {
                m_TargetContainer.AddToClassList("targetContainerActive");
            }
        }
    }
}
