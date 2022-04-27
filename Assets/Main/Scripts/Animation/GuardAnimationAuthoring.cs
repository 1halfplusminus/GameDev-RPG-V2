using RPG.Core;
using UnityEngine;
namespace RPG.Animation
{

    public class GuardAnimationAuthoring : MonoBehaviour
    {
        public ClipAsset LookingAround;

    }
    public class GuardAnimationConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((GuardAnimationAuthoring guardAnimationAuthoring) =>
            {
                // var entity = GetPrimaryEntity(guardAnimationAuthoring);
                // var setup = new GuardAnimationSetup { };
                // if (this.TryGetClipAssetRef(guardAnimationAuthoring.gameObject, guardAnimationAuthoring.LookingAround, out var lookingAroundClip))
                // {
                //     setup.LookingAround = lookingAroundClip;
                // }
                // DstEntityManager.AddComponent<GuardAnimation>(entity);
                // DstEntityManager.AddComponentData(entity, setup);
                // DstEntityManager.AddComponent<DeltaTime>(entity);
            });
        }
    }
}
