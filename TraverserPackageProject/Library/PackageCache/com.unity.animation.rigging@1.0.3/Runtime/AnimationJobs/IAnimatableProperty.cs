namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Interface for animatable property handles used to read and write
    /// values in the AnimationStream.
    /// </summary>
    /// <typeparam name="T">The animatable value type</typeparam>
    public interface IAnimatableProperty<T>
    {
        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The property value.</returns>
        T Get(AnimationStream stream);
        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new property value.</param>
        void Set(AnimationStream stream, T value);
    }

    /// <summary>
    /// Boolean property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct BoolProperty : IAnimatableProperty<bool>
    {
        /// <summary>The PropertyStreamHandle used in the AnimationStream.</summary>
        public PropertyStreamHandle value;

        /// <summary>
        /// Creates a BoolProperty handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a BoolProperty handle that represents the new binding.</returns>
        public static BoolProperty Bind(Animator animator, Component component, string name)
        {
            return new BoolProperty()
            {
                value = animator.BindStreamProperty(component.transform, component.GetType(), name)
            };
        }

        /// <summary>
        /// Creates a BoolProperty handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="property">The name of the property.</param>
        /// <returns>Returns a BoolProperty handle that represents the new binding.</returns>
        public static BoolProperty BindCustom(Animator animator, string property)
        {
            return new BoolProperty
            {
                value = animator.BindCustomStreamProperty(property, CustomStreamPropertyType.Bool)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The boolean property value.</returns>
        public bool Get(AnimationStream stream) => value.GetBool(stream);
        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="v">The new boolean property value.</param>
        public void Set(AnimationStream stream, bool v) => value.SetBool(stream, v);
    }

    /// <summary>
    /// Integer property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct IntProperty : IAnimatableProperty<int>
    {
        /// <summary>The PropertyStreamHandle used in the AnimationStream.</summary>
        public PropertyStreamHandle value;

        /// <summary>
        /// Creates a IntProperty handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a IntProperty handle that represents the new binding.</returns>
        public static IntProperty Bind(Animator animator, Component component, string name)
        {
            return new IntProperty()
            {
                value = animator.BindStreamProperty(component.transform, component.GetType(), name)
            };
        }

        /// <summary>
        /// Creates a IntProperty handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="property">The name of the property.</param>
        /// <returns>Returns a IntProperty handle that represents the new binding.</returns>
        public static IntProperty BindCustom(Animator animator, string property)
        {
            return new IntProperty
            {
                value = animator.BindCustomStreamProperty(property, CustomStreamPropertyType.Int)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The integer property value.</returns>
        public int Get(AnimationStream stream) => value.GetInt(stream);
        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="v">The new integer property value.</param>
        public void Set(AnimationStream stream, int v) => value.SetInt(stream, v);
    }

    /// <summary>
    /// Float property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct FloatProperty : IAnimatableProperty<float>
    {
        /// <summary>The PropertyStreamHandle used in the AnimationStream.</summary>
        public PropertyStreamHandle value;

        /// <summary>
        /// Creates a FloatProperty handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a FloatProperty handle that represents the new binding.</returns>
        public static FloatProperty Bind(Animator animator, Component component, string name)
        {
            return new FloatProperty()
            {
                value = animator.BindStreamProperty(component.transform, component.GetType(), name)
            };
        }

        /// <summary>
        /// Creates a FloatProperty handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="property">The name of the property.</param>
        /// <returns>Returns a FloatProperty handle that represents the new binding.</returns>
        public static FloatProperty BindCustom(Animator animator, string property)
        {
            return new FloatProperty
            {
                value = animator.BindCustomStreamProperty(property, CustomStreamPropertyType.Float)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The float property value.</returns>
        public float Get(AnimationStream stream) => value.GetFloat(stream);
        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="v">The new float property value.</param>
        public void Set(AnimationStream stream, float v) => value.SetFloat(stream, v);
    }

    /// <summary>
    /// Vector2 property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct Vector2Property : IAnimatableProperty<Vector2>
    {
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle x;
        /// <summary>The PropertyStreamHandle used for the Y component in the AnimationStream.</summary>
        public PropertyStreamHandle y;

        /// <summary>
        /// Creates a Vector2Property handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a Vector2Property handle that represents the new binding.</returns>
        public static Vector2Property Bind(Animator animator, Component component, string name)
        {
            var type = component.GetType();
            return new Vector2Property
            {
                x = animator.BindStreamProperty(component.transform, type, name + ".x"),
                y = animator.BindStreamProperty(component.transform, type, name + ".y")
            };
        }

        /// <summary>
        /// Creates a Vector2Property handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>Returns a Vector2Property handle that represents the new binding.</returns>
        public static Vector2Property BindCustom(Animator animator, string name)
        {
            return new Vector2Property
            {
                x = animator.BindCustomStreamProperty(name + ".x", CustomStreamPropertyType.Float),
                y = animator.BindCustomStreamProperty(name + ".y", CustomStreamPropertyType.Float)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The Vector2 property value.</returns>
        public Vector2 Get(AnimationStream stream) =>
            new Vector2(x.GetFloat(stream), y.GetFloat(stream));

        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new Vector2 property value.</param>
        public void Set(AnimationStream stream, Vector2 value)
        {
            x.SetFloat(stream, value.x);
            y.SetFloat(stream, value.y);
        }
    }

    /// <summary>
    /// Vector3 property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct Vector3Property : IAnimatableProperty<Vector3>
    {
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle x;
        /// <summary>The PropertyStreamHandle used for the Y component in the AnimationStream.</summary>
        public PropertyStreamHandle y;
        /// <summary>The PropertyStreamHandle used for the Z component in the AnimationStream.</summary>
        public PropertyStreamHandle z;

        /// <summary>
        /// Creates a Vector3Property handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a Vector3Property handle that represents the new binding.</returns>
        public static Vector3Property Bind(Animator animator, Component component, string name)
        {
            var type = component.GetType();
            return new Vector3Property
            {
                x = animator.BindStreamProperty(component.transform, type, name + ".x"),
                y = animator.BindStreamProperty(component.transform, type, name + ".y"),
                z = animator.BindStreamProperty(component.transform, type, name + ".z")
            };
        }

        /// <summary>
        /// Creates a Vector3Property handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>Returns a Vector3Property handle that represents the new binding.</returns>
        public static Vector3Property BindCustom(Animator animator, string name)
        {
            return new Vector3Property
            {
                x = animator.BindCustomStreamProperty(name + ".x", CustomStreamPropertyType.Float),
                y = animator.BindCustomStreamProperty(name + ".y", CustomStreamPropertyType.Float),
                z = animator.BindCustomStreamProperty(name + ".z", CustomStreamPropertyType.Float)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The Vector3 property value.</returns>
        public Vector3 Get(AnimationStream stream) =>
            new Vector3(x.GetFloat(stream), y.GetFloat(stream), z.GetFloat(stream));

        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new Vector3 property value.</param>
        public void Set(AnimationStream stream, Vector3 value)
        {
            x.SetFloat(stream, value.x);
            y.SetFloat(stream, value.y);
            z.SetFloat(stream, value.z);
        }
    }

    /// <summary>
    /// Vector3Int property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct Vector3IntProperty : IAnimatableProperty<Vector3Int>
    {
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle x;
        /// <summary>The PropertyStreamHandle used for the Y component in the AnimationStream.</summary>
        public PropertyStreamHandle y;
        /// <summary>The PropertyStreamHandle used for the Z component in the AnimationStream.</summary>
        public PropertyStreamHandle z;

        /// <summary>
        /// Creates a Vector3IntProperty handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a Vector3IntProperty handle that represents the new binding.</returns>
        public static Vector3IntProperty Bind(Animator animator, Component component, string name)
        {
            var type = component.GetType();
            return new Vector3IntProperty
            {
                x = animator.BindStreamProperty(component.transform, type, name + ".x"),
                y = animator.BindStreamProperty(component.transform, type, name + ".y"),
                z = animator.BindStreamProperty(component.transform, type, name + ".z")
            };
        }

        /// <summary>
        /// Creates a Vector3IntProperty handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>Returns a Vector3IntProperty handle that represents the new binding.</returns>
        public static Vector3IntProperty BindCustom(Animator animator, string name)
        {
            return new Vector3IntProperty
            {
                x = animator.BindCustomStreamProperty(name + ".x", CustomStreamPropertyType.Int),
                y = animator.BindCustomStreamProperty(name + ".y", CustomStreamPropertyType.Int),
                z = animator.BindCustomStreamProperty(name + ".z", CustomStreamPropertyType.Int)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The Vector3Int property value.</returns>
        public Vector3Int Get(AnimationStream stream) =>
            new Vector3Int(x.GetInt(stream), y.GetInt(stream), z.GetInt(stream));

        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new Vector3Int property value.</param>
        public void Set(AnimationStream stream, Vector3Int value)
        {
            x.SetInt(stream, value.x);
            y.SetInt(stream, value.y);
            z.SetInt(stream, value.z);
        }
    }

    /// <summary>
    /// Vector3Bool property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct Vector3BoolProperty : IAnimatableProperty<Vector3Bool>
    {
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle x;
        /// <summary>The PropertyStreamHandle used for the Y component in the AnimationStream.</summary>
        public PropertyStreamHandle y;
        /// <summary>The PropertyStreamHandle used for the Z component in the AnimationStream.</summary>
        public PropertyStreamHandle z;

        /// <summary>
        /// Creates a Vector3BoolProperty handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a Vector3BoolProperty handle that represents the new binding.</returns>
        public static Vector3BoolProperty Bind(Animator animator, Component component, string name)
        {
            var type = component.GetType();
            return new Vector3BoolProperty
            {
                x = animator.BindStreamProperty(component.transform, type, name + ".x"),
                y = animator.BindStreamProperty(component.transform, type, name + ".y"),
                z = animator.BindStreamProperty(component.transform, type, name + ".z")
            };
        }

        /// <summary>
        /// Creates a Vector3BoolProperty handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>Returns a Vector3BoolProperty handle that represents the new binding.</returns>
        public static Vector3BoolProperty BindCustom(Animator animator, string name)
        {
            return new Vector3BoolProperty
            {
                x = animator.BindCustomStreamProperty(name + ".x", CustomStreamPropertyType.Bool),
                y = animator.BindCustomStreamProperty(name + ".y", CustomStreamPropertyType.Bool),
                z = animator.BindCustomStreamProperty(name + ".z", CustomStreamPropertyType.Bool)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The Vector3Bool property value.</returns>
        public Vector3Bool Get(AnimationStream stream) =>
            new Vector3Bool(x.GetBool(stream), y.GetBool(stream), z.GetBool(stream));

        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new Vector3Bool property value.</param>
        public void Set(AnimationStream stream, Vector3Bool value)
        {
            x.SetBool(stream, value.x);
            y.SetBool(stream, value.y);
            z.SetBool(stream, value.z);
        }
    }

    /// <summary>
    /// Vector4 property handle used to read and write values in the AnimationStream.
    /// </summary>
    public struct Vector4Property : IAnimatableProperty<Vector4>
    {
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle x;
        /// <summary>The PropertyStreamHandle used for the Y component in the AnimationStream.</summary>
        public PropertyStreamHandle y;
        /// <summary>The PropertyStreamHandle used for the Z component in the AnimationStream.</summary>
        public PropertyStreamHandle z;
        /// <summary>The PropertyStreamHandle used for the X component in the AnimationStream.</summary>
        public PropertyStreamHandle w;

        /// <summary>
        /// Creates a Vector4Property handle representing a property binding on a Component.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="component">The Component owning the parameter.</param>
        /// <param name="name">The property name</param>
        /// <returns>Returns a Vector4Property handle that represents the new binding.</returns>
        public static Vector4Property Bind(Animator animator, Component component, string name)
        {
            var type = component.GetType();
            return new Vector4Property
            {
                x = animator.BindStreamProperty(component.transform, type, name + ".x"),
                y = animator.BindStreamProperty(component.transform, type, name + ".y"),
                z = animator.BindStreamProperty(component.transform, type, name + ".z"),
                w = animator.BindStreamProperty(component.transform, type, name + ".w")
            };
        }

        /// <summary>
        /// Creates a Vector4Property handle for a custom property in the AnimationStream to pass extra data to downstream animation jobs in the graph.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>Returns a Vector4Property handle that represents the new binding.</returns>
        public static Vector4Property BindCustom(Animator animator, string name)
        {
            return new Vector4Property
            {
                x = animator.BindCustomStreamProperty(name + ".x", CustomStreamPropertyType.Float),
                y = animator.BindCustomStreamProperty(name + ".y", CustomStreamPropertyType.Float),
                z = animator.BindCustomStreamProperty(name + ".z", CustomStreamPropertyType.Float),
                w = animator.BindCustomStreamProperty(name + ".w", CustomStreamPropertyType.Float)
            };
        }

        /// <summary>
        /// Gets the property value from a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The Vector4 property value.</returns>
        public Vector4 Get(AnimationStream stream) =>
            new Vector4(x.GetFloat(stream), y.GetFloat(stream), z.GetFloat(stream), w.GetFloat(stream));

        /// <summary>
        /// Sets the property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new Vector4 property value.</param>
        public void Set(AnimationStream stream, Vector4 value)
        {
            x.SetFloat(stream, value.x);
            y.SetFloat(stream, value.y);
            z.SetFloat(stream, value.z);
            w.SetFloat(stream, value.w);
        }
    }
}
