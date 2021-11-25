using Unity.Entities;
using RPG.Gameplay;
using Unity.Jobs;
using RPG.Core;
using Unity.Animation;
using Unity.Transforms;
using Unity.Deformations;
using Unity.Collections;

namespace RPG.Saving
{
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
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
                }
            );
        }
        protected override void OnUpdate()
        {
            var em = DstEntityManager;
            var identifiableEntities = savePlayedQuery.ToEntityArray(Allocator.TempJob);
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Played played, in Identifier identifier) =>
            {
                var entity = identifiableEntities[entityInQueryIndex];
                Debug.Log($"Save played to {entity.Index} {entity.Version}");
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
