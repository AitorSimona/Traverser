using System;
using System.Collections.Generic;
using UnityEngine.Assertions.Comparers;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    internal class TagAnnotation : IDisposable
    {
        public float startTime;
        public float duration;

        public Payload payload;
        public string name;

        TagAnnotation(Type type, float startTime, float duration)
        {
            this.startTime = startTime;
            this.duration = duration;
            payload = Payload.Create(type);
        }

        public void Dispose()
        {
            payload.Dispose();
        }

        public static TagAnnotation Create(Type t, float startTime, float duration)
        {
            return new TagAnnotation(t, startTime, duration);
        }

        public static TagAnnotation Create<T>(T payload, float startTime, float duration) where T : struct
        {
            TagAnnotation tag = Create(typeof(T), startTime, duration);
            tag.payload.SetValue(payload);
            return tag;
        }

        public Type Type
        {
            get { return payload.Type; }
        }

        public string Name
        {
            get
            {
                if (payload.Type != null && payload.ScriptableObject != null)
                {
                    return payload.Type.Name;
                }

                return $"{TagAttribute.k_UnknownTagType} - {payload.SimplifiedTypeName} ";
            }
        }

        static Dictionary<Type, string> k_TagTypeFullNameCache;

        static TagAnnotation()
        {
            k_TagTypeFullNameCache = new Dictionary<Type, string>();
        }

        public static string GetTagTypeFullName(TagAnnotation t)
        {
            if (t.payload == null || t.payload.Type == null)
            {
                return null;
            }

            string name;
            if (k_TagTypeFullNameCache.TryGetValue(t.payload.Type, out name))
            {
                return name;
            }

            if (t.payload.Type != null && t.payload.ScriptableObject != null)
            {
                name = t.payload.Type.FullName;
                k_TagTypeFullNameCache[t.payload.Type] = name;
                return name;
            }

            return $"{TagAttribute.k_UnknownTagType} - {t.payload.SimplifiedTypeName} ";
        }

        public float EndTime => startTime + duration;

        public bool DoesCoverTime(float timeInSeconds)
        {
            return timeInSeconds >= startTime && (timeInSeconds - startTime) <= duration;
        }

        public bool Equals(TagAnnotation other)
        {
            return Type == other.Type &&
                FloatComparer.AreEqual(startTime, other.startTime, FloatComparer.kEpsilon) &&
                FloatComparer.AreEqual(duration, other.duration, FloatComparer.kEpsilon);
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public event Action Changed;
    }
}
