using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Kinematica
{
    /// <summary>
    /// Reference to the runtime asset.
    /// </summary>
    [Serializable]
    public struct BinaryReference
    {
        // GUID of the Kinematica Asset used to build this binary
        [SerializeField]
        internal SerializableGuid assetGuid;

        internal BinaryReference(string assetGuidStr)
        {
            assetGuid = new SerializableGuid();
            assetGuid.SetGuidStr(assetGuidStr);
        }

        internal BinaryReference(int val0, int val1, int val2, int val3)
        {
            assetGuid = new SerializableGuid()
            {
                val0 = val0,
                val1 = val1,
                val2 = val2,
                val3 = val3
            };
        }

        public bool IsSet()
        {
            return assetGuid.IsSet();
        }

        internal string FilePath
        {
            get { return Binary.GetBinaryFilePathFromAssetGuid(assetGuid.GetGuidStr()); }
        }

        internal BlobAssetReference<Binary> LoadBinary()
        {
            if (!IsSet())
            {
                throw new ArgumentException($"Binary reference not set!");
            }

            string binaryFilePath = FilePath;
            if (BlobFile.Exists(binaryFilePath))
            {
                int fileVersion = BlobFile.ReadBlobAssetVersion(binaryFilePath);
                if (fileVersion != Binary.s_CodeVersion)
                {
                    throw new ArgumentException($"Binary '{binaryFilePath}' is outdated, its version is {fileVersion} whereas Kinematica version is {Binary.s_CodeVersion}. Rebuild the asset to update the binary.");
                }

                return BlobFile.ReadBlobAsset<Binary>(binaryFilePath);
            }
            else
            {
#if UNITY_EDITOR
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid.GetGuidStr());
                if (assetPath == null)
                {
                    throw new ArgumentException($"Invalid binary reference to an non-existing Kinematica Asset");
                }

                throw new ArgumentException($"Binary '{binaryFilePath}' does not exist. Did you forget to buid the corresponding Kinematica asset, {assetPath} ?");
#else
                throw new ArgumentException($"Binary '{binaryFilePath}' does not exist. Did you forget to build the corresponding Kinematica asset ?");
#endif
            }
        }
    }
}
