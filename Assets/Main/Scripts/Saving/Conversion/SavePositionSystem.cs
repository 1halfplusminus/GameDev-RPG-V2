using Unity.Entities;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;
using RPG.Core;
using RPG.Control;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class SavePositionSystem : SystemBase, ISavingConversionSystem
    {

        public EntityManager DstEntityManager { get; }
        IdentifiableSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery saveablePosition;
        public SavePositionSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = DstEntityManager.World.GetOrCreateSystem<IdentifiableSystem>();
            commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();
            saveablePosition = DstEntityManager.CreateEntityQuery(
                  new EntityQueryDesc()
                  {
                      All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() },
                      None = new ComponentType[] {

                       }
                  }
            );

        }
        protected override void OnUpdate()
        {
            var identifiableEntities = IdentifiableSystem.IndexQuery(
                saveablePosition
            );
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .WithDisposeOnCompletion(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Translation translation, in Identifier identifier) =>
            {
                var entity = IdentifiableSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
                pWriter.AddComponent(entityInQueryIndex, entity, translation);
                pWriter.AddComponent(entityInQueryIndex, entity, new WarpTo { Destination = translation.Value });
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(Dependency);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


    }
}
