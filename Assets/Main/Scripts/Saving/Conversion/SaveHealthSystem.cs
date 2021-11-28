using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace RPG.Saving
{
    // FIXME: Refactor like in scene
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public class SaveHealthSystem : SaveConversionSystemBase<Health>
    {
        public SaveHealthSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity)
        {

            var savingState = GetSingleton<SavingState>();
            Debug.Log($"{savingState.Direction} health for {dstEntity}");
            DstEntityManager.AddComponentData(dstEntity, GetComponent(entity));
        }
    }
}
