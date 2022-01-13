using RPG.Core;
using UnityEngine;
namespace RPG.Animation
{


    class CharacterAnimationAuthoring : MonoBehaviour
    {
        public ClipAsset IDLE;

        public ClipAsset Walk;

        public ClipAsset Run;

        public ClipAsset Attack;

        public ClipAsset Dead;
    }

    public class CharacterAnimationConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((CharacterAnimationAuthoring characterAnimation) =>
            {
                var entity = GetPrimaryEntity(characterAnimation);
                var setup = new CharacterAnimationSetup { };
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.IDLE, out var idleClip))
                {
                    setup.IDLE = idleClip;
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Walk, out var walkClip))
                {
                    setup.Walk = walkClip;
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Run, out var runClip))
                {
                    setup.Run = runClip;
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Attack, out var attackClip))
                {
                    setup.Attack = attackClip;
                }
                if (this.TryGetClipAssetRef(characterAnimation.gameObject, characterAnimation.Dead, out var deadClip))
                {
                    setup.Dead = deadClip;
                }
                DstEntityManager.AddComponent<CharacterAnimation>(entity);
                DstEntityManager.AddComponentData(entity, setup);
                DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }

    }
}
