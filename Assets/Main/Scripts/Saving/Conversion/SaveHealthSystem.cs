using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace RPG.Saving
{
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
    public class SaveHealthSystem : SaveConversionSystemBase<Health>
    {
        public SaveHealthSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity, ComponentDataFromEntity<Health> components, EntityManager dstManager, EntityManager entityManager)
        {
            Debug.Log($"Save health for {dstEntity}");
            DstEntityManager.AddComponentData<Health>(dstEntity, components[entity]);
        }
    }
    /* [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
    public class SaveHealthSystem : SystemBase, ISavingConversionSystem
    {

        public EntityManager DstEntityManager { get; }
        IdentifiableSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery saveHealthQuery;
        public SaveHealthSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = DstEntityManager.World.GetOrCreateSystem<IdentifiableSystem>();
            commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();
            saveHealthQuery = DstEntityManager.CreateEntityQuery(
                new EntityQueryDesc()
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() },
                }
            );
        }
        protected override void OnUpdate()
        {
            var em = DstEntityManager;
            var identifiableEntities = saveHealthQuery.ToEntityArray(Allocator.TempJob);
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Health health, in Identifier identifier) =>
            {

                var entity = identifiableEntities[entityInQueryIndex];
                Debug.Log($"Save health to {entity.Index} {entity.Version}");
                pWriter.AddComponent(entityInQueryIndex, entity, health);

            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


    } */
}
