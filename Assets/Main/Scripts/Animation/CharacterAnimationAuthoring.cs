using RPG.Core;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Entities;
using UnityEngine;
namespace RPG.Animation
{
#if UNITY_EDITOR

    class CharacterAnimationAuthoring : MonoBehaviour
    {
        public AnimationClip IDLE;

        public AnimationClip Walk;

        public AnimationClip Run;
    }

    public class CharacterAnimationConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((CharacterAnimationAuthoring characterAnimation) =>
            {
                var entity = GetPrimaryEntity(characterAnimation);
                var setup = new CharacterAnimationSetup { };
                if (TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.IDLE, out var idleClip))
                {
                    setup.IDLE = idleClip;
                }
                if (TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Walk, out var walkClip))
                {
                    setup.Walk = walkClip;
                }
                if (TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Run, out var runClip))
                {
                    setup.Run = runClip;
                }
                DstEntityManager.AddComponent<CharacterAnimation>(entity);
                DstEntityManager.AddComponentData(entity, setup);
                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }

        private bool TryGetClipAssetRef(GameObject obj, AnimationClip clip, out BlobAssetReference<Clip> blobAsset)
        {
            blobAsset = default;
            if (clip != null)
            {
                DeclareAssetDependency(obj, clip);
                blobAsset = BlobAssetStore.GetClip(clip);
                return true;
            }
            return false;
        }
    }
#endif
}
