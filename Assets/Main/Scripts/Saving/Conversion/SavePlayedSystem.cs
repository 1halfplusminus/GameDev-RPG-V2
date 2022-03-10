using Unity.Entities;
using RPG.Core;
using Unity.Animation;

namespace RPG.Saving
{
    // FIXME: Refactor like in scene
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public class SavePlayedSystem : SaveConversionSystemBase<Played>
    {
        public SavePlayedSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity e, in Played _) => Convert(GetPrimaryEntity(e), e)).WithStructuralChanges().Run();
        }
        protected override void Convert(Entity dstEntity, Entity entity)
        {
            if (dstEntity != Entity.Null)
            {
                var savingState = GetSingleton<SavingState>();
                Debug.Log($"{savingState.Direction} played for {dstEntity}");
                DstEntityManager.AddComponent<Played>(dstEntity);
            }
        }
    }
}
