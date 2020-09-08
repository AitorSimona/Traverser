using UnityEngine.Assertions;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Unity.SnapshotDebugger
{
    public class FrameDebugger
    {
        public IEnumerable<FrameDebugProviderInfo> ProviderInfos
        {
            get
            {
                foreach (FrameDebugProviderInfo providerInfo in m_ProviderInfos)
                {
                    yield return providerInfo;
                }
            }
        }

        public void AddFrameDebugProvider(IFrameDebugProvider provider)
        {
            if (!m_RecordingEnabled)
            {
                return;
            }

            if (!m_ProviderInfos.Any(info => info.uniqueIdentifier == provider.GetUniqueIdentifier()))
            {
                m_ProviderInfos.Add(new FrameDebugProviderInfo()
                {
                    uniqueIdentifier = provider.GetUniqueIdentifier(),
                    displayName = provider.GetDisplayName(),
                    provider = provider
                });
            }
        }

        public void RemoveFrameDebugProvider(IFrameDebugProvider provider)
        {
            int index = m_ProviderInfos.FindIndex(info => info.uniqueIdentifier == provider.GetUniqueIdentifier());
            if (index >= 0)
            {
                FrameDebugProviderInfo info = m_ProviderInfos[index];
                info.provider = null;
                m_ProviderInfos[index] = info;
            }

            m_Selection.RemoveAll(selected => selected.providerInfo.provider == provider);
        }

        public List<IFrameAggregate> GetFrameAggregates(int providerIdentifier)
        {
            if (m_FrameAggregates.TryGetValue(providerIdentifier, out FrameAggregateSet set))
            {
                return set.aggregates.Select(pair => pair.Value.aggregate).ToList();
            }

            return new List<IFrameAggregate>();
        }

        public T FindAggregate<T>(int providerIdentifier) where T : class, IFrameAggregate
        {
            if (m_FrameAggregates.TryGetValue(providerIdentifier, out FrameAggregateSet set))
            {
                if (set.aggregates.TryGetValue(typeof(T), out FrameAggregateInfo aggregateInfo))
                {
                    return aggregateInfo.aggregate as T;
                }
            }

            return null;
        }

        public void AddFrameRecord<FrameAggregate>(IFrameDebugProvider provider, IFrameRecord record) where FrameAggregate : IFrameAggregate, new()
        {
            List<IFrameRecord> records = new List<IFrameRecord>() { record };
            AddFrameRecords<FrameAggregate>(provider, records);
        }

        public void AddFrameRecords<FrameAggregate>(IFrameDebugProvider provider, IEnumerable<IFrameRecord> records) where FrameAggregate : IFrameAggregate, new()
        {
            if (!m_RecordingEnabled)
            {
                return;
            }

            AddFrameDebugProvider(provider);

            FrameAggregateSet aggregates;
            int providerIdentifier = provider.GetUniqueIdentifier();
            if (!m_FrameAggregates.TryGetValue(providerIdentifier, out aggregates))
            {
                aggregates = FrameAggregateSet.Create();
                m_FrameAggregates.Add(providerIdentifier, aggregates);
            }

            Type aggregateType = typeof(FrameAggregate);
            FrameAggregateInfo aggregateInfo;
            if (!aggregates.aggregates.TryGetValue(aggregateType, out aggregateInfo))
            {
                aggregateInfo = FrameAggregateInfo.Create<FrameAggregate>();
                aggregates.aggregates.Add(aggregateType, aggregateInfo);
            }

            aggregateInfo.pendingRecords.AddRange(records);
        }

        public int NumSelected => m_Selection.Count;

        public IEnumerable<SelectedFrameDebugProvider> Selection
        {
            get
            {
                foreach (SelectedFrameDebugProvider selected in m_Selection)
                {
                    yield return selected;
                }
            }
        }

        public SelectedFrameDebugProvider GetSelected(int index)
        {
            return m_Selection[index];
        }

        public bool TrySelect(IFrameDebugProvider provider, object metadata = null)
        {
            if (provider == null)
            {
                return false;
            }

            FrameDebugProviderInfo providerInfo = FrameDebugProviderInfo.CreateFromProvider(provider);
            if (!m_ProviderInfos.Any(p => p.uniqueIdentifier == providerInfo.uniqueIdentifier))
            {
                return false;
            }

            SelectedFrameDebugProvider selected = new SelectedFrameDebugProvider()
            {
                providerInfo = providerInfo,
                metadata = metadata
            };

            int index = m_Selection.FindIndex(s => s.providerInfo.uniqueIdentifier == providerInfo.uniqueIdentifier);
            if (index >= 0)
            {
                // already selected, update metadata
                m_Selection[index] = selected;
            }
            else
            {
                m_Selection.Add(selected);
            }

            return true;
        }

        public void ClearSelection() => m_Selection.Clear();

        internal void Update(float frameEndTime, float startTimeInSeconds)
        {
            if (frameEndTime <= m_FrameStartTime)
            {
                foreach (var pair in m_FrameAggregates.ToList())
                {
                    FrameAggregateSet set = pair.Value;
                    set.ClearPendingRecords();
                    m_FrameAggregates[pair.Key] = set;
                }
                return;
            }

            UpdateRecords(m_FrameStartTime, frameEndTime, startTimeInSeconds);
            m_FrameStartTime = frameEndTime;

            m_ProviderInfos.RemoveAll(providerInfo => providerInfo.provider == null && !m_FrameAggregates.ContainsKey(providerInfo.uniqueIdentifier));
        }

        internal void PruneRecordsStartingAfterTimeStamp(float startTimeInSeconds)
        {
            foreach (var pair in m_FrameAggregates.ToList())
            {
                FrameAggregateSet set = pair.Value;
                set.PruneRecordsStartingAfterTimeStamp(startTimeInSeconds);
                m_FrameAggregates[pair.Key] = set;
            }
        }

        internal void EnableRecording(float startTime)
        {
            PruneRecordsStartingAfterTimeStamp(startTime);
            m_FrameStartTime = startTime;
            m_RecordingEnabled = true;
        }

        internal void DisableRecording() => m_RecordingEnabled = false;

        internal void Clear()
        {
            m_FrameStartTime = 0.0f;
            m_ProviderInfos.Clear();
            m_FrameAggregates.Clear();
            m_Selection.Clear();
        }

        void UpdateRecords(float frameStartTime, float frameEndTime, float startTimeInSeconds)
        {
            List<int> obsoletes = new List<int>();

            foreach (var pair in m_FrameAggregates)
            {
                FrameAggregateSet set = pair.Value;
                set.UpdateRecords(frameStartTime, frameEndTime, startTimeInSeconds);

                if (set.aggregates.Count == 0)
                {
                    obsoletes.Add(pair.Key);
                }
            }

            foreach (int key in obsoletes)
            {
                m_FrameAggregates.Remove(key);
            }
        }

        class FrameAggregateInfo
        {
            public IFrameAggregate aggregate;
            public List<IFrameRecord> pendingRecords;

            public static FrameAggregateInfo Create<FrameAgreggate>() where FrameAgreggate : IFrameAggregate, new()
            {
                return new FrameAggregateInfo()
                {
                    aggregate = new FrameAgreggate(),
                    pendingRecords = new List<IFrameRecord>()
                };
            }

            public void UpdateRecords(float frameStartTime, float frameEndTime, float startTimeInSeconds)
            {
                aggregate.AddRecords(pendingRecords, frameStartTime, frameEndTime);
                ClearPendingRecords();

                aggregate.PruneFramesBeforeTimestamp(startTimeInSeconds);
            }

            public void PruneRecordsStartingAfterTimeStamp(float startTimeInSeconds)
            {
                aggregate.PruneFramesStartingAfterTimestamp(startTimeInSeconds);
                ClearPendingRecords();
            }

            public void ClearPendingRecords()
            {
                pendingRecords.Clear();
            }
        }

        class FrameAggregateSet
        {
            public Dictionary<Type, FrameAggregateInfo> aggregates;

            public static FrameAggregateSet Create() => new FrameAggregateSet()
            {
                aggregates = new Dictionary<Type, FrameAggregateInfo>()
            };

            public void UpdateRecords(float frameStartTime, float frameEndTime, float startTimeInSeconds)
            {
                List<Type> emptyAggregateTypes = new List<Type>(aggregates.Count);

                foreach (var pair in aggregates)
                {
                    FrameAggregateInfo aggregateInfo = pair.Value;

                    aggregateInfo.UpdateRecords(frameStartTime, frameEndTime, startTimeInSeconds);

                    if (aggregateInfo.aggregate.IsEmpty)
                    {
                        emptyAggregateTypes.Add(pair.Key);
                    }
                }

                foreach (Type type in emptyAggregateTypes)
                {
                    aggregates.Remove(type);
                }
            }

            public void PruneRecordsStartingAfterTimeStamp(float startTimeInSeconds)
            {
                List<Type> emptyAggregateTypes = new List<Type>(aggregates.Count);

                foreach (var pair in aggregates)
                {
                    FrameAggregateInfo aggregateInfo = pair.Value;

                    aggregateInfo.PruneRecordsStartingAfterTimeStamp(startTimeInSeconds);

                    if (aggregateInfo.aggregate.IsEmpty)
                    {
                        emptyAggregateTypes.Add(pair.Key);
                    }
                }

                foreach (Type type in emptyAggregateTypes)
                {
                    aggregates.Remove(type);
                }
            }

            public void ClearPendingRecords()
            {
                foreach (var pair in aggregates.ToList())
                {
                    FrameAggregateInfo aggregateInfo = pair.Value;
                    aggregateInfo.ClearPendingRecords();
                    aggregates[pair.Key] = aggregateInfo;
                }
            }
        }

        bool                                m_RecordingEnabled = false;
        float                               m_FrameStartTime = 0.0f;
        List<FrameDebugProviderInfo>        m_ProviderInfos = new List<FrameDebugProviderInfo>();
        Dictionary<int, FrameAggregateSet>  m_FrameAggregates = new Dictionary<int, FrameAggregateSet>();
        List<SelectedFrameDebugProvider>    m_Selection = new List<SelectedFrameDebugProvider>();
    }
}
