using Unity.Entities;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class SavePositionSystem : SystemBase, ISavingConversionSystem
    {

        public EntityManager DstEntityManager { get; }
        SavingConversionSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;
        public SavePositionSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = DstEntityManager.World.GetOrCreateSystem<SavingConversionSystem>();
            commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var identifiableEntities = conversionSystem.IdentifiableEntities;
            var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(identifiableEntities)
            .ForEach((int entityInQueryIndex, in Translation translation, in Identifier identifier) =>
            {
                var entity = SavingConversionSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
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
