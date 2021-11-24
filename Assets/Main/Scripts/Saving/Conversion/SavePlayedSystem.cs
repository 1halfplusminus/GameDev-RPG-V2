using Unity.Entities;
using RPG.Gameplay;
using Unity.Jobs;
using RPG.Core;
using Unity.Animation;
using Unity.Transforms;
using Unity.Deformations;

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
                    None = new ComponentType[] {
                        ComponentType.ReadOnly<HasSpawn>(),
                        ComponentType.ReadOnly<Parent>()
                    }
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
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class SavePlayedSystem : SystemBase, ISavingConversionSystem
    {

        public EntityManager DstEntityManager { get; }
        IdentifiableSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery savePlayedQuery;
        public SavePlayedSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = DstEntityManager.World.GetOrCreateSystem<IdentifiableSystem>();
            commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();
            savePlayedQuery = DstEntityManager.CreateEntityQuery(
                new EntityQueryDesc()
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() },
                    None = new ComponentType[] {
                        ComponentType.ReadOnly<HasSpawn>(),
                        ComponentType.ReadOnly<SkinMatrix>()
                    }
                }
            );
        }
        protected override void OnUpdate()
        {
            var em = DstEntityManager;
            var identifiableEntities = IdentifiableSystem.IndexQuery(savePlayedQuery);
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Played played, in Identifier identifier) =>
            {
                var entity = IdentifiableSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
                Debug.Log($"Add played to {entity}");
                pWriter.AddComponent(entityInQueryIndex, entity, played);

            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


    }
}
