namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Read/write handle on a Transform component used in Animation C# Jobs.
    /// </summary>
    public struct ReadWriteTransformHandle
    {
        TransformStreamHandle m_Handle;

        /// <summary>
        /// Gets the position of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The position of the transform relative to the parent.</returns>
        public Vector3 GetLocalPosition(AnimationStream stream) => m_Handle.GetLocalPosition(stream);
        /// <summary>
        /// Gets the rotation of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The rotation of the transform relative to the parent.</returns>
        public Quaternion GetLocalRotation(AnimationStream stream) => m_Handle.GetLocalRotation(stream);
        /// <summary>
        /// Gets the scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The scale of the transform relative to the parent.</returns>
        public Vector3 GetLocalScale(AnimationStream stream) => m_Handle.GetLocalScale(stream);
        /// <summary>
        /// Gets the position, rotation and scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform relative to the parent.</param>
        /// <param name="rotation">The rotation of the transform relative to the parent.</param>
        /// <param name="scale">The scale of the transform relative to the parent.</param>
        public void GetLocalTRS(AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale) =>
            m_Handle.GetLocalTRS(stream, out position, out rotation, out scale);

        /// <summary>
        /// Sets the position of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform relative to the parent.</param>
        public void SetLocalPosition(AnimationStream stream, Vector3 position) => m_Handle.SetLocalPosition(stream, position);
        /// <summary>
        /// Sets the rotation of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="rotation">The rotation of the transform relative to the parent.</param>
        public void SetLocalRotation(AnimationStream stream, Quaternion rotation) => m_Handle.SetLocalRotation(stream, rotation);
        /// <summary>
        /// Sets the scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="scale">The scale of the transform relative to the parent.</param>
        public void SetLocalScale(AnimationStream stream, Vector3 scale) => m_Handle.SetLocalScale(stream, scale);
        /// <summary>
        /// Sets the position, rotation and scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform relative to the parent.</param>
        /// <param name="rotation">The rotation of the transform relative to the parent.</param>
        /// <param name="scale">The scale of the transform relative to the parent.</param>
        /// <param name="useMask">Set to true to write the specified parameters if the matching stream parameters have not already been modified.</param>
        public void SetLocalTRS(AnimationStream stream, Vector3 position, Quaternion rotation, Vector3 scale, bool useMask = false) =>
            m_Handle.SetLocalTRS(stream, position, rotation, scale, useMask);

        /// <summary>
        /// Gets the position of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The position of the transform in world space.</returns>
        public Vector3 GetPosition(AnimationStream stream) => m_Handle.GetPosition(stream);
        /// <summary>
        /// Gets the rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The rotation of the transform in world space.</returns>
        public Quaternion GetRotation(AnimationStream stream) => m_Handle.GetRotation(stream);
        /// <summary>
        /// Gets the position and scaled rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform in world space.</param>
        /// <param name="rotation">The rotation of the transform in world space.</param>
        public void GetGlobalTR(AnimationStream stream, out Vector3 position, out Quaternion rotation) =>
            m_Handle.GetGlobalTR(stream, out position, out rotation);

        /// <summary>
        /// Sets the position of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform in world space.</param>
        public void SetPosition(AnimationStream stream, Vector3 position) => m_Handle.SetPosition(stream, position);
        /// <summary>
        /// Sets the rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="rotation">	The rotation of the transform in world space.</param>
        public void SetRotation(AnimationStream stream, Quaternion rotation) => m_Handle.SetRotation(stream, rotation);
        /// <summary>
        /// Sets the position and rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform in world space.</param>
        /// <param name="rotation">The rotation of the transform in world space.</param>
        /// <param name="useMask">Set to true to write the specified parameters if the matching stream parameters have not already been modified.</param>
        public void SetGlobalTR(AnimationStream stream, Vector3 position, Quaternion rotation, bool useMask = false) =>
            m_Handle.SetGlobalTR(stream, position, rotation, useMask);

        /// <summary>
        /// Returns whether this handle is resolved.
        /// A ReadWriteTransformHandle is resolved if it is valid, if it has the same bindings version than the one in the stream, and if it is bound to the transform in the stream.
        /// A ReadWriteTransformHandle can become unresolved if the animator bindings have changed or if the transform had been destroyed.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.Resolve"/>
        /// <seealso cref="ReadWriteTransformHandle.IsValid"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>Returns true if the handle is resolved, false otherwise.</returns>
        public bool IsResolved(AnimationStream stream) => m_Handle.IsResolved(stream);
        /// <summary>
        /// Returns whether this is a valid handle.
        /// A ReadWriteTransformHandle may be invalid if, for example, you didn't use the correct function to create it.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.Bind"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>Returns whether this is a valid handle.</returns>
        public bool IsValid(AnimationStream stream) => m_Handle.IsValid(stream);
        /// <summary>
        /// Bind this handle with an animated values from the AnimationStream.
        /// Handles are lazily resolved as they're accessed, but in order to prevent unwanted CPU spikes, this method allows to resolve handles in a deterministic way.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.IsResolved"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        public void Resolve(AnimationStream stream) => m_Handle.Resolve(stream);

        /// <summary>
        /// Create a ReadWriteTransformHandle representing the new binding between the Animator and a Transform already bound to the Animator.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="transform">The Transform to bind.</param>
        /// <returns>Returns the ReadWriteTransformHandle that represents the new binding.</returns>
        public static ReadWriteTransformHandle Bind(Animator animator, Transform transform)
        {
            ReadWriteTransformHandle handle = new ReadWriteTransformHandle();
            if (transform == null || !transform.IsChildOf(animator.transform))
                return handle;

            handle.m_Handle = animator.BindStreamTransform(transform);
            return handle;
        }
    }

    /// <summary>
    /// Read-only handle on a Transform component used in Animation C# Jobs.
    /// </summary>
    public struct ReadOnlyTransformHandle
    {
        TransformStreamHandle m_StreamHandle;
        TransformSceneHandle m_SceneHandle;
        byte m_InStream;

        /// <summary>
        /// Gets the position of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The position of the transform relative to the parent.</returns>
        public Vector3 GetLocalPosition(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalPosition(stream) : m_SceneHandle.GetLocalPosition(stream);

        /// <summary>
        /// Gets the rotation of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The rotation of the transform relative to the parent.</returns>
        public Quaternion GetLocalRotation(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalRotation(stream) : m_SceneHandle.GetLocalRotation(stream);

        /// <summary>
        /// Gets the scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The scale of the transform relative to the parent.</returns>
        public Vector3 GetLocalScale(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalScale(stream) : m_SceneHandle.GetLocalScale(stream);

        /// <summary>
        /// Gets the position, rotation and scale of the transform relative to the parent.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform relative to the parent.</param>
        /// <param name="rotation">The rotation of the transform relative to the parent.</param>
        /// <param name="scale">The scale of the transform relative to the parent.</param>
        public void GetLocalTRS(AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            if (m_InStream == 1)
                m_StreamHandle.GetLocalTRS(stream, out position, out rotation, out scale);
            else
                m_SceneHandle.GetLocalTRS(stream, out position, out rotation, out scale);
        }

        /// <summary>
        /// Gets the position of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The position of the transform in world space.</returns>
        public Vector3 GetPosition(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetPosition(stream) : m_SceneHandle.GetPosition(stream);

        /// <summary>
        /// Gets the rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>The rotation of the transform in world space.</returns>
        public Quaternion GetRotation(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetRotation(stream) : m_SceneHandle.GetRotation(stream);

        /// <summary>
        /// Gets the position and scaled rotation of the transform in world space.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="position">The position of the transform in world space.</param>
        /// <param name="rotation">The rotation of the transform in world space.</param>
        public void GetGlobalTR(AnimationStream stream, out Vector3 position, out Quaternion rotation)
        {
            if (m_InStream == 1)
                m_StreamHandle.GetGlobalTR(stream, out position, out rotation);
            else
                m_SceneHandle.GetGlobalTR(stream, out position, out rotation);
        }

        /// <summary>
        /// Returns whether this handle is resolved.
        /// A ReadOnlyTransformHandle is resolved if it is valid, if it has the same bindings version than the one in the stream, and if it is bound to the transform in the stream.
        /// A ReadOnlyTransformHandle can become unresolved if the animator bindings have changed or if the transform had been destroyed.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.Resolve"/>
        /// <seealso cref="ReadWriteTransformHandle.IsValid"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>Returns true if the handle is resolved, false otherwise.</returns>
        public bool IsResolved(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.IsResolved(stream) : true;

        /// <summary>
        /// Returns whether this is a valid handle.
        /// A ReadOnlyTransformHandle may be invalid if, for example, you didn't use the correct function to create it.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.Bind"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <returns>Returns whether this is a valid handle.</returns>
        public bool IsValid(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.IsValid(stream) : m_SceneHandle.IsValid(stream);

        /// <summary>
        /// Bind this handle with an animated values from the AnimationStream.
        /// Handles are lazily resolved as they're accessed, but in order to prevent unwanted CPU spikes, this method allows to resolve handles in a deterministic way.
        /// </summary>
        /// <seealso cref="ReadWriteTransformHandle.IsResolved"/>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        public void Resolve(AnimationStream stream)
        {
            if (m_InStream == 1)
                m_StreamHandle.Resolve(stream);
        }

        /// <summary>
        /// Create a ReadOnlyTransformHandle representing the new binding between the Animator and a Transform already bound to the Animator.
        /// </summary>
        /// <param name="animator">The Animator on which to bind the new handle.</param>
        /// <param name="transform">The Transform to bind.</param>
        /// <returns>Returns the ReadOnlyTransformHandle that represents the new binding.</returns>
        public static ReadOnlyTransformHandle Bind(Animator animator, Transform transform)
        {
            ReadOnlyTransformHandle handle = new ReadOnlyTransformHandle();
            if (transform == null)
                return handle;

            handle.m_InStream = (byte)(transform.IsChildOf(animator.transform) ? 1 : 0);
            if (handle.m_InStream == 1)
                handle.m_StreamHandle = animator.BindStreamTransform(transform);
            else
                handle.m_SceneHandle = animator.BindSceneTransform(transform);

            return handle;
        }
    }
}
