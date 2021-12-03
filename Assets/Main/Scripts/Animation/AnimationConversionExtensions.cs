using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Entities;
using UnityEngine;
namespace RPG.Animation
{
#if UNITY_EDITOR
    static class AnimationConversionExtensions
    {
        public static bool TryGetClipAssetRef(this GameObjectConversionSystem conversionSystem, GameObject obj, AnimationClip clip, out BlobAssetReference<Clip> blobAsset)
        {
            blobAsset = default;
            if (clip != null)
            {
                conversionSystem.DeclareAssetDependency(obj, clip);
                blobAsset = conversionSystem.BlobAssetStore.GetClip(clip);
                return true;
            }
            return false;
        }
    }
#endif
}
