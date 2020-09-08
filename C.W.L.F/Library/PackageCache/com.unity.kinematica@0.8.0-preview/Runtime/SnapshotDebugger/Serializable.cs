namespace Unity.SnapshotDebugger
{
    public interface Serializable
    {
        // TODO: Instead of passing a read/write buffer we should
        //       pass an interface/class that only allows reading
        //       or writing (e.g. BinaryReader/BinaryWriter).
        void WriteToStream(Buffer buffer);

        void ReadFromStream(Buffer buffer);
    }
}
