using Unity.Entities;
using Unity.Animation;
using RPG.Control;

namespace RPG.Saving
{

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public class SaveInSceneSystem : SaveConversionSystemBase<InScene>
    {
        public SaveInSceneSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity)
        {
            var component = GetComponent(entity);
            var savingState = GetSingleton<SavingState>();
            if (savingState.Type == SavingStateType.FILE)
            {
                Debug.Log($"{savingState.Direction} In Scene for {dstEntity}");
                DstEntityManager.AddComponentData(dstEntity, component);
                DstEntityManager.RemoveComponent<InSceneLoaded>(dstEntity);
                DstEntityManager.RemoveComponent<InSceneEntity>(dstEntity);
            }

        }
    }
}
