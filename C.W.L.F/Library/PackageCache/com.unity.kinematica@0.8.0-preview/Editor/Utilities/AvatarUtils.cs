using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal static class AvatarExtension
    {
        public static List<AnimationRig.Joint> GetAvatarJoints(this Avatar avatar)
        {
            if (avatar == null)
            {
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(avatar);
            GameObject avatarRootObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
            if (avatarRootObject == null)
            {
                return null;
            }

            List<AnimationRig.Joint> jointsList = new List<AnimationRig.Joint>();
            jointsList.Add(new AnimationRig.Joint()
            {
                name = avatarRootObject.transform.name,
                parentIndex = -1,
                localTransform = AffineTransform.identity,
            });

            foreach (Transform child in avatarRootObject.transform)
            {
                AnimationRig.CollectJointsRecursive(jointsList, child, 0);
            }

            return jointsList;
        }

        public static List<string> GetAvatarJointNames(this Avatar avatar)
        {
            var jointsList = avatar.GetAvatarJoints();
            if (jointsList == null)
            {
                return null;
            }

            List<string> names = jointsList.Select(j => j.name).ToList();
            return names;
        }
    }
}
