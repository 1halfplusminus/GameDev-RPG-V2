using Unity.Entities;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
    public class SavePositionSystem : SaveConversionSystemBase<Translation>
    {
        public SavePositionSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity, ComponentDataFromEntity<Translation> components, EntityManager dstManager, EntityManager entityManager)
        {
            Debug.Log($"Save translation for {dstEntity}");
            DstEntityManager.AddComponentData<Translation>(dstEntity, components[entity]);
            DstEntityManager.AddComponentData<WarpTo>(dstEntity, new WarpTo { Destination = components[entity].Value });
        }
    }
    /*  [DisableAutoCreation]
     [UpdateInGroup(typeof(SavingConversionSystemGroup))]
     [UpdateAfter(typeof(SaveIdentifierSystem))]
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
                   }
             );

         }
         protected override void OnUpdate()
         {
             var identifiableEntities = saveablePosition.ToEntityArray(Allocator.TempJob);
             var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
             Entities
             .WithReadOnly(identifiableEntities)
             .WithDisposeOnCompletion(identifiableEntities)
             .WithDisposeOnCompletion(identifiableEntities)
             .ForEach((int entityInQueryIndex, in Translation translation, in Identifier identifier) =>
             {
                 var entity = identifiableEntities[entityInQueryIndex];
                 Debug.Log($"Save position for entity {entity.Index} {entity.Version}");
                 pWriter.AddComponent(entityInQueryIndex, entity, translation);
                 pWriter.AddComponent(entityInQueryIndex, entity, new WarpTo { Destination = translation.Value });
             }).ScheduleParallel();
             commandBufferSystem.AddJobHandleForProducer(Dependency);

         }

         protected override void OnDestroy()
         {
             base.OnDestroy();
         }


     } */
}
