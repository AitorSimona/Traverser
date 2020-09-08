using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.SnapshotDebugger
{
    internal sealed class Registry
    {
        Dictionary<GameObject, Aggregate> _aggregates = new Dictionary<GameObject, Aggregate>();

        public struct PrefabReference
        {
            public GameObject gameObject;
            public Identifier<SnapshotProvider> identifier;
        }

        Dictionary<Identifier<Aggregate>, PrefabReference> _prefabReferences = new Dictionary<Identifier<Aggregate>, PrefabReference>();

        Snapshot currentSnapshot;

        public Aggregate this[GameObject gameObject]
        {
            get
            {
                Aggregate result;

                if (_aggregates.TryGetValue(gameObject, out result))
                {
                    return result;
                }

                return null;
            }

            set { _aggregates[gameObject] = value; }
        }

        public Aggregate this[Identifier<Aggregate> identifier]
        {
            get
            {
                foreach (var iterator in _aggregates)
                {
                    if (iterator.Value.identifier == identifier)
                    {
                        return iterator.Value;
                    }
                }

                return null;
            }
        }

        public IEnumerable<Aggregate> aggregates
        {
            get
            {
                foreach (var iterator in _aggregates)
                {
                    yield return iterator.Value;
                }
            }
        }

        public IEnumerable<SnapshotProvider> providers
        {
            get
            {
                foreach (var aggregate in aggregates)
                {
                    foreach (var provider in aggregate.providers)
                    {
                        yield return provider;
                    }
                }
            }
        }

        public Identifier<Aggregate>[] ToArray()
        {
            int writeIndex = _aggregates.Count;

            var result = new Identifier<Aggregate>[writeIndex];

            foreach (var aggregate in aggregates)
            {
                result[--writeIndex] = aggregate.identifier;
            }

            Assert.IsTrue(writeIndex == 0);

            return result;
        }

        public void OnEnable(SnapshotProvider provider)
        {
            Assert.IsTrue(!provider.identifier.IsValid);
            Assert.IsTrue(!provider.aggregate.IsValid);

            if (Application.isPlaying)
            {
                provider.identifier = Identifier<SnapshotProvider>.Create();

                if (_aggregates.ContainsKey(provider.gameObject))
                {
                    var aggregate = this[provider.gameObject];

                    Assert.IsTrue(aggregate.identifier.IsValid);

                    provider.aggregate = aggregate.identifier;

                    aggregate._providers.Add(provider);
                }
                else
                {
                    var aggregate = Aggregate.Create(provider);

                    this[provider.gameObject] = aggregate;

                    provider.aggregate = aggregate.identifier;

                    if (!Debugger.instance.rewind)
                    {
                        var prefab = provider.GetComponent<Prefab>();

                        if (prefab != null)
                        {
                            var prefabReference = prefab.prefab.gameObject;

                            _prefabReferences[provider.aggregate] = new PrefabReference
                            {
                                gameObject = prefabReference,
                                identifier = provider.identifier
                            };
                        }
                    }
                }

                Assert.IsTrue(provider.aggregate.IsValid);
                Assert.IsTrue(provider.identifier.IsValid);

                Assert.IsTrue(this[provider.gameObject].identifier == provider.aggregate);
                Assert.IsTrue(this[provider.gameObject].Find(provider.identifier) == provider);
            }
        }

        public void OnDisable(SnapshotProvider provider)
        {
            Assert.IsTrue(provider.identifier.IsValid);
            Assert.IsTrue(provider.aggregate.IsValid);

            Assert.IsTrue(this[provider.gameObject].identifier == provider.aggregate);
            Assert.IsTrue(this[provider.gameObject].Find(provider.identifier) == provider);

            if (Application.isPlaying)
            {
                if (Debugger.instance.IsState(Debugger.State.Record))
                {
                    var prefab = provider.GetComponent<Prefab>();
                    if (prefab == null)
                    {
                        throw new MissingComponentException($"Missing '{nameof(Prefab)}' component on object '{provider.gameObject.name}' destroyed while recording. Recorder won't be able to spawn it back.");
                    }
                }

                var aggregate = _aggregates[provider.gameObject];

                aggregate._providers.Remove(provider);

                if (aggregate._providers.Count <= 0)
                {
                    _aggregates.Remove(provider.gameObject);
                }
            }
        }

        public void OnEarlyUpdate(bool rewind)
        {
            foreach (var aggregate in aggregates)
            {
                aggregate.OnEarlyUpdate(rewind);
            }
        }

        public Snapshot RecordSnapshot(float timeStamp, float deltaTime)
        {
            var snapshot = Snapshot.Create(timeStamp, deltaTime);

            snapshot.aggregateIdentifier = Identifier<Aggregate>.nextIdentifier;
            snapshot.providerIdentifier = Identifier<SnapshotProvider>.nextIdentifier;

            currentSnapshot = snapshot;

            return snapshot;
        }

        public void RestoreSnapshot(Snapshot snapshot)
        {
            SynchronizeSnapshot(snapshot);

            foreach (var aggregate in snapshot.aggregates)
            {
                var targetAggregate = this[aggregate.identifier];

                if (targetAggregate == null)
                {
                    Assert.IsTrue(true);
                }

                Assert.IsTrue(targetAggregate != null);

                foreach (var provider in aggregate.providers)
                {
                    var targetProvider = targetAggregate[provider.identifier];

                    Assert.IsTrue(targetProvider != null);

                    provider.payload.PrepareForRead();

                    targetProvider.ReadFromStream(provider.payload);

                    if (provider.customPayload != null)
                    {
                        provider.customPayload.Value.PrepareForRead();

                        Assert.IsTrue(targetProvider.RequirePostProcess);

                        targetProvider.OnReadPostProcess(provider.customPayload.Value);
                    }
                }
            }

            Identifier<Aggregate>.nextIdentifier = snapshot.aggregateIdentifier;
            Identifier<SnapshotProvider>.nextIdentifier = snapshot.providerIdentifier;
        }

        void SynchronizeSnapshot(Snapshot targetSnapshot)
        {
            var snapshotAggregates = targetSnapshot.ToArray();

            var registryAggregates = ToArray();

            foreach (var identifier in snapshotAggregates.Except(registryAggregates))
            {
                Assert.IsTrue(_prefabReferences.ContainsKey(identifier));

                var prefab = _prefabReferences[identifier];

                Identifier<Aggregate>.nextIdentifier = identifier - 1;
                Identifier<SnapshotProvider>.nextIdentifier = prefab.identifier - 1;

                GameObject.Instantiate(prefab.gameObject);
            }

            foreach (var identifier in registryAggregates.Except(snapshotAggregates))
            {
                var aggregate = this[identifier];

                Assert.IsTrue(aggregate != null);

                GameObject.Destroy(aggregate.gameObject);
            }
        }
    }
}
