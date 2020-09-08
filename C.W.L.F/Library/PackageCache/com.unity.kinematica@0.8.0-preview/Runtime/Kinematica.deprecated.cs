using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    public partial class Kinematica : SnapshotProvider
    {
        [System.Obsolete("Push has been removed for clarity, please use PlayFirstSequence instead. (UnityUpgradable) -> Unity.Kinematica.Kinematica.PlayFirstSequence(Unity.Kinematica.QueryResult)")]
        public void Push(QueryResult queryResult)
        {
            PlayFirstSequence(queryResult);
        }
    }
}
