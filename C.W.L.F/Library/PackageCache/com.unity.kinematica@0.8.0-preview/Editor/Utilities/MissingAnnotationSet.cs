using System.Collections.Generic;

namespace Unity.Kinematica.Editor
{
    internal struct MissingAnnotationSet
    {
        internal struct Occurence
        {
            public string clipName;
            public float startTime;
            public float duration;
        }

        Dictionary<string, List<Occurence>> annotations;

        public static MissingAnnotationSet Create() => new MissingAnnotationSet()
        {
            annotations = new Dictionary<string, List<Occurence>>()
        };

        public bool Any()
        {
            return annotations.Count > 0;
        }

        public void Add(string annotationName, string clipName, float startTime, float duration)
        {
            Occurence occurence = new Occurence()
            {
                clipName = clipName,
                startTime = startTime,
                duration = duration
            };

            List<Occurence> occurencesList;
            if (!annotations.TryGetValue(annotationName, out occurencesList))
            {
                occurencesList = new List<Occurence>();
            }

            occurencesList.Add(occurence);

            annotations[annotationName] = occurencesList;
        }

        public IEnumerable<(string, List<Occurence>)> Annotations
        {
            get
            {
                foreach (var pair in annotations)
                {
                    yield return (pair.Key, pair.Value);
                }
            }
        }
    }
}
