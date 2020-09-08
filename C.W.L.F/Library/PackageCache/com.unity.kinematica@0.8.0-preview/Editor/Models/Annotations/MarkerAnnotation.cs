using System;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    internal class MarkerAnnotation : IDisposable
    {
        public float timeInSeconds;
        public Payload payload;

        public string Name
        {
            get
            {
                if (payload.Type != null)
                {
                    return payload.Type.Name;
                }

                return MarkerAttribute.k_UnknownMarkerType;
            }
        }

        public static MarkerAnnotation Create(Type type, float timeInSeconds)
        {
            return new MarkerAnnotation(type, timeInSeconds);
        }

        public static MarkerAnnotation Create<T>(T payload, float timeInSeconds) where T : struct
        {
            MarkerAnnotation marker = Create(typeof(T), timeInSeconds);
            marker.payload.SetValue(payload);
            return marker;
        }

        MarkerAnnotation(Type type, float timeInSeconds)
        {
            this.timeInSeconds = timeInSeconds;
            payload = Payload.Create(type);
        }

        public void Dispose()
        {
            payload.Dispose();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public event Action Changed;
    }
}
