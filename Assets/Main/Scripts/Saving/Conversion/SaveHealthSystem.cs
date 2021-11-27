using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace RPG.Saving
{
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public class SaveHealthSystem : SaveConversionSystemBase<Health>
    {
        public SaveHealthSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity, Health component, SceneSection section)
        {
            Debug.Log($"Save health for {dstEntity}");
            DstEntityManager.AddComponentData<Health>(dstEntity, component);
        }
    }
}
