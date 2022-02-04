using Unity.Animation;
using Unity.Entities;
using UnityEngine;

namespace RPG.Animation
{
#if UNITY_EDITOR
    using Unity.Animation.Hybrid;
    using Unity.Collections;
#endif
    static class AnimationConversionExtensions
    {
#if UNITY_EDITOR
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
#endif
        public static bool TryGetClipAssetRef(this GameObjectConversionSystem conversionSystem, GameObject obj, ClipAsset clip, out BlobAssetReference<Clip> blobAsset)
        {
            blobAsset = default;
            if (clip != null)
            {
                conversionSystem.DeclareAssetDependency(obj, clip);
                blobAsset = clip.GetClip();
                conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blobAsset);
                return true;
            }
            return false;
        }
    }

}
