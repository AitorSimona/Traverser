using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.SnapshotDebugger
{
    public class Aggregate
    {
        public GameObject gameObject;

        public Identifier<Aggregate> identifier;

        public List<SnapshotProvider> _providers = new List<SnapshotProvider>();

        public static Aggregate Create(SnapshotProvider provider)
        {
            return new Aggregate(provider);
        }

        public SnapshotProvider Find(Identifier<SnapshotProvider> identifier)
        {
            return _providers.Find(
                provider => provider.identifier == identifier);
        }

        public void OnEarlyUpdate(bool rewind)
        {
            foreach (var provider in providers)
            {
                provider.OnEarlyUpdate(rewind);
            }
        }

        public IEnumerable<SnapshotProvider> providers
        {
            get
            {
                foreach (var provider in _providers)
                {
                    yield return provider;
                }
            }
        }

        public SnapshotProvider this[Identifier<SnapshotProvider> identifier]
        {
            get { return Find(identifier); }
        }

        Aggregate(SnapshotProvider provider)
        {
            Assert.IsTrue(provider.identifier.IsValid);

            identifier = Identifier<Aggregate>.Create();

            gameObject = provider.gameObject;

            _providers.Add(provider);
        }
    }
}
