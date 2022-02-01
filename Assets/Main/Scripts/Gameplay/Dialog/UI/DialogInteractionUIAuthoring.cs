
using RPG.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace RPG.UI
{
    public class DialogInteractionUIConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DialogAuthoring dialogAuthoring) =>
            {
                var entity = GetPrimaryEntity(dialogAuthoring);
                var interactionUIPrefab = GetPrimaryEntity(dialogAuthoring.InteractionUIPrefab);
                DstEntityManager.AddComponentData(entity, new DialogInteractionUI { Prefab = interactionUIPrefab });
            });
        }
    }
}