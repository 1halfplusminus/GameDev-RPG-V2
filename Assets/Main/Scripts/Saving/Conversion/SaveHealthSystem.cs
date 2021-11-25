using Unity.Entities;
using Unity.Jobs;

namespace RPG.Saving
{
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
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
            var identifiableEntities = IdentifiableSystem.IndexQuery(saveHealthQuery);
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Health health, in Identifier identifier) =>
            {
                var entity = IdentifiableSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
                pWriter.AddComponent(entityInQueryIndex, entity, health);

            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


    }
}
