using UnityEngine;
using Unity.Entities;
using Unity.Animation;

namespace RPG.Hybrid
{
#if UNITY_EDITOR
    using Unity.Animation.Hybrid;
    using UnityEngine.AddressableAssets;
    // public class AnimationConversionSystem : GameObjectConversionSystem
    // {
    //     protected override void OnUpdate()
    //     {

    //         Entities.ForEach((UnityEngine.Animation animation) =>
    //         {
    //             DeclareLinkedEntityGroup(animation.gameObject);
    //             DeclareLinkedEntityGroup(animation.transform.parent.gameObject);
    //             AddHybridComponent(animation);
    //         });
    //     }
    // }

    public struct AnimatorClip : IBufferElementData
    {
        public BlobAssetReference<Clip> Clip;
    }
    [DisableAutoCreation]
    public class AnimatorClipConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Animator animator) =>
            {
                var skinnedMeshs = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
                var animatorEntity = GetPrimaryEntity(animator);
                var clipsBuffer = DstEntityManager.AddBuffer<AnimatorClip>(animatorEntity);
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    DeclareAssetDependency(animator.gameObject, clip);
                    var convertedClip = BlobAssetStore.GetClip(clip);
                    clipsBuffer.Add(new AnimatorClip { Clip = convertedClip });
                }
            });
        }
    }
#endif


}
