using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;

using UnityEditor.Experimental.GraphView;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Kinematica.UIElements;
using Unity.Mathematics;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica.Editor
{
    public class GraphNode : Node
    {
        internal List<GraphNodePort> inputPorts = new List<GraphNodePort>();
        internal List<GraphNodePort> outputPorts = new List<GraphNodePort>();

        internal GraphNodePort readPort;
        internal GraphNodePort writePort;

        protected VisualElement controlsContainer;

        VisualElement stateElement;

        public DebugReference reference { get; private set; }

        public Type runtimeType { get; private set; }

        List<GraphNodePort> ports = new List<GraphNodePort>();

        internal GraphNodeView owner { private set; get; }

        public virtual string Title => $"{DataAttribute.GetDescription(runtimeType)}";

        readonly string styleSheet = "GraphNode.uss";


        void Initialize(GraphNodeView owner, Type type, DebugReference reference)
        {
            this.owner = owner;
            this.runtimeType = type;
            this.reference = reference;

            RegisterCallback<GeometryChangedEvent>(GeometryChangededCallback);

            UIElementsUtils.ApplyStyleSheet(styleSheet, this);

            InitializePorts();
            InitializeView();

            DrawDefaultInspector();

            RefreshExpandedState();

            RefreshPorts();
        }

        public void Reinitialize(DebugReference reference)
        {
            this.reference = reference;

            title = Title;

            UpdatePortValues();

            RefreshPorts();
        }

        public T GetDebugObject<T>() where T : struct, IDebugObject
        {
            return owner.GetDebugMemory().ReadObject<T>(reference);
        }

        public T GetDebugObjectField<T>(DebugIdentifier fieldIdentifier) where T : struct, IDebugObject
        {
            DebugReference fieldReference = owner.GetDebugMemory().FindObjectReference(fieldIdentifier);
            Assert.IsTrue(fieldReference.IsValid);
            return owner.GetDebugMemory().ReadObject<T>(fieldReference);
        }

        public void SetReadOnly(bool flag)
        {
            VisualElement selectionBorder =
                this.Q<VisualElement>("selection-border");

            VisualElement collapseButton =
                this.Q<VisualElement>("collapse-button");

            if (flag)
            {
                selectionBorder.style.display = DisplayStyle.None;
                collapseButton.style.display = DisplayStyle.None;
            }
            else
            {
                selectionBorder.style.display = DisplayStyle.Flex;
                collapseButton.style.display = DisplayStyle.Flex;
            }
        }

        public virtual void UpdateState()
        {
        }

        internal static GraphNode Create(GraphNodeView owner, Type type, DebugReference reference)
        {
            Type nodeType = GraphNodeAttribute.GetNodeType(reference.identifier.typeHashCode);

            if (nodeType != null)
            {
                Type runtimeType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item1;

                var graphNode =
                    Activator.CreateInstance(nodeType) as GraphNode;

                if (graphNode == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create node type {nodeType.FullName} for type {type.FullName}.");
                }

                graphNode.Initialize(owner, runtimeType, reference);

                return graphNode;
            }

            return null;
        }

        void GeometryChangededCallback(GeometryChangedEvent e)
        {
            if (math.abs(e.oldRect.width - e.newRect.width) <= 10)
                return;

            if (math.abs(e.oldRect.height - e.newRect.height) <= 10)
                return;

            owner.GeometryChangededCallback();
        }

        protected void AddInputPort(DebugIdentifier identifier, string name, bool selfNodeOnly)
        {
            GraphNodePort port = GraphNodePort.Create(Direction.Input, typeof(DebugIdentifier), identifier, selfNodeOnly);
            inputPorts.Add(port);
            inputContainer.Add(port);

            port.Initialize(this, name);
        }

        protected void AddOutputPort(DebugIdentifier identifier, string name, bool selfNodeOnly)
        {
            GraphNodePort port = GraphNodePort.Create(Direction.Output, typeof(DebugIdentifier), identifier, selfNodeOnly);
            outputPorts.Add(port);
            outputContainer.Add(port);

            port.Initialize(this, name);
        }

        object GetDebugObjectGeneric()
        {
            MethodInfo method = GetType().GetMethod(nameof(GetDebugObject), BindingFlags.Instance | BindingFlags.Public);
            MethodInfo genericMethod = method.MakeGenericMethod(new Type[] { runtimeType });

            return genericMethod.Invoke(this, null);
        }

        protected virtual void InitializePorts()
        {
            object obj = GetDebugObjectGeneric();

            DataType dataType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item2;

            var inputFields = dataType.inputFields;

            foreach (var field in inputFields)
            {
                var name = field.name;
                var info = field.info;

                AddInputPort((DebugIdentifier)info.GetValue(obj), name, field.selfNodeOnly);
            }

            var outputFields = dataType.outputFields;

            foreach (var field in outputFields)
            {
                var name = field.name;
                var info = field.info;

                AddOutputPort((DebugIdentifier)info.GetValue(obj), name, field.selfNodeOnly);
            }

            if (DataAttribute.IsInputOutput(runtimeType))
            {
                AddInputPort(reference, DataAttribute.GetDescription(runtimeType), false);
                AddOutputPort(reference, DataAttribute.GetDescription(runtimeType), false);
            }

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        protected virtual void UpdatePortValues()
        {
            object obj = GetDebugObjectGeneric();

            DataType dataType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item2;

            var inputFields = dataType.inputFields;

            for (int i = 0; i < inputFields.Length; ++i)
            {
                inputPorts[i].identifier = (DebugIdentifier)inputFields[i].info.GetValue(obj);
            }

            var outputFields = dataType.outputFields;

            for (int i = 0; i < outputFields.Length; ++i)
            {
                outputPorts[i].identifier = (DebugIdentifier)outputFields[i].info.GetValue(obj);
            }

            if (DataAttribute.IsInputOutput(runtimeType))
            {
                inputPorts[inputFields.Length].identifier = reference.identifier;
                outputPorts[outputFields.Length].identifier = reference.identifier;
            }

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        void InitializeView()
        {
            controlsContainer = new VisualElement
            {
                name = "controls"
            };

            mainContainer.Add(controlsContainer);

#if OLD_KINEMATICA_GRAPH
            debugContainer = new VisualElement
            {
                name = "debug"
            };

            if (nodeTarget.debug)
                mainContainer.Add(debugContainer);
#endif

            Type type = GetValueType();

            title = Title;

            var color = DataAttribute.GetColor(runtimeType);
            if (!color.Equals(Color.clear))
            {
                titleContainer.style.backgroundColor = color;
            }

            stateElement = new VisualElement { name = "state" };
            stateElement.Add(new VisualElement { name = "icon" });
            stateElement.style.flexDirection = FlexDirection.Row;

            titleContainer.Add(stateElement);
        }

        unsafe Type GetValueType()
        {
            return runtimeType;
        }

        public virtual unsafe void DrawDefaultInspector()
        {
#if OLD_KINEMATICA_GRAPH
            object obj = GetDebugObjectGeneric();

            DataType dataType = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item2;

            foreach (var field in dataType.propertyFields)
            {
                var fieldInfo = field.info;

                name = field.name;

                var element = FieldFactory.CreateField(
                    field.type, fieldInfo.GetValue(obj), null, name);

                if (element != null)
                {
                    element.SetEnabled(false);

                    controlsContainer.Add(element);
                }
            }
#endif
        }

        public virtual void OnCreate()
        {
        }

        public override bool IsMovable()
        {
            return false;
        }

        public virtual void OnSelected(ref MotionSynthesizer synthesizer)
        {
            for (int i = 0; i < Debugger.frameDebugger.NumSelected; ++i)
            {
                SelectedFrameDebugProvider selected = Debugger.frameDebugger.GetSelected(i);
                if (selected.providerInfo.provider != null && selected.providerInfo.provider == owner.FrameDebugProvider)
                {
                    Debugger.frameDebugger.TrySelect(selected.providerInfo.provider, reference.identifier);
                }
            }
        }

        public virtual void OnPreLateUpdate(ref MotionSynthesizer synthesizer)
        {
        }
    }
}
