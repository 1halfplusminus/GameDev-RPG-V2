using Unity.Entities;
using Unity.Collections;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class SaveIdentifierSystem : SystemBase, ISavingConversionSystem
    {
        public EntityManager DstEntityManager { get; }
        IdentifiableSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery saveIdentifiedQuery;

        EntityQuery dstIdentifiedQuery;
        public SaveIdentifierSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            saveIdentifiedQuery = EntityManager.CreateEntityQuery(
                new EntityQueryDesc()
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() },
                }
            );
            dstIdentifiedQuery = DstEntityManager.CreateEntityQuery(
              new EntityQueryDesc()
              {
                  All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() },
              }
          );

        }
        protected override void OnUpdate()
        {
            var identified = saveIdentifiedQuery.ToComponentDataArray<Identifier>(Allocator.Temp);
            var dstIdentified = dstIdentifiedQuery.ToComponentDataArray<Identifier>(Allocator.Temp);
            var archetype = DstEntityManager.CreateArchetype(new ComponentType[] { typeof(Identifier) });
            for (int i = 0; i < identified.Length; i++)
            {
                if (i >= dstIdentified.Length)
                {
                    var entity = DstEntityManager.CreateEntity(new ComponentType[] { typeof(Identifier) });
                    DstEntityManager.AddComponentData(entity, identified[i]);
                }
            }
        }
    }
}
