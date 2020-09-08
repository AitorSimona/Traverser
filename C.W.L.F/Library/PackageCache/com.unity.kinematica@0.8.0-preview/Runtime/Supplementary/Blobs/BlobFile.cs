using System.IO;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    internal static class BlobFile
    {
        public static string ConvertFileName(string fileName)
        {
            return Application.dataPath + '/' + fileName;
        }

        unsafe public static void WriteBlobAsset<T>(BlobAllocator blobAllocator, ref T asset, string fileName) where T : struct
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                long dataSize = blobAllocator.DataSize;
                var data = new byte[dataSize];
                fixed(byte* ptr = &data[0])
                {
                    UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf<T>(ref asset), data.Length);
                }
                writer.Write(data);
                writer.Close();
            }
        }

        public static bool Exists(string fileName)
        {
            return File.Exists(fileName);
        }

        unsafe public static int ReadBlobAssetVersion(string filename)
        {
            if (Exists(filename))
            {
                FileStream fileStream = File.OpenRead(filename);
                if (fileStream != null)
                {
                    int fileVersionSize = sizeof(int);
                    byte[] readBuffer = new byte[fileVersionSize];
                    fileStream.Read(readBuffer, 0, fileVersionSize);

                    int fileVersion = System.BitConverter.ToInt32(readBuffer, 0);

                    fileStream.Close();

                    return fileVersion;
                }
            }

            return -1;
        }

        unsafe public static BlobAssetReference<T> ReadBlobAsset<T>(string fileName) where T : struct
        {
            if (Exists(fileName))
            {
                return BlobAssetReference<T>.Create(
                    File.ReadAllBytes(fileName));
            }

            return new BlobAssetReference<T>();
        }
    }
}
