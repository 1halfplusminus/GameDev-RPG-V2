
using UnityEngine;
using Unity.Entities;
using Unity.Animation.Hybrid;


namespace RPG.Animation
{
    public class AnimatorConversionSystem : GameObjectConversionSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            // this.AddTypeToCompanionWhiteList(typeof(Animator));
            // this.AddTypeToCompanionWhiteList(typeof(SkinnedMeshRenderer));
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((RigComponent rigAuthoring) =>
            {
                var entity = GetPrimaryEntity(rigAuthoring);
                var animatedEntities = DstEntityManager.AddBuffer<AnimatedEntity>(entity);
                for (int i = 0; i < rigAuthoring.Bones.Length; i++)
                {
                    var bone = rigAuthoring.Bones[i];
                    var boneEntity = GetPrimaryEntity(bone);
                    animatedEntities.Add(new AnimatedEntity { Value = boneEntity });
                }
            });
            Entities.ForEach((Animator animator) =>
            {
                var gameObject = animator.gameObject;
                var entity = GetPrimaryEntity(animator);
                DeclareAssetDependency(animator.gameObject, animator.runtimeAnimatorController);
                DstEntityManager.AddBuffer<CurrentlyPlayingClip>(entity);
                var clipsBuffer = DstEntityManager.AddBuffer<AnimationClips>(entity);
#if UNITY_EDITOR
                var nbClip = animator.runtimeAnimatorController.animationClips.Length;
                for (int i = 0; i < nbClip; i++)
                {
                    var clip = animator.runtimeAnimatorController.animationClips[i];
                    var clipBlob = BlobAssetStore.GetClip(clip);
                    clipsBuffer.Add(new AnimationClips { Clip = clipBlob });
                }
#endif
            });
        }
    }


}
