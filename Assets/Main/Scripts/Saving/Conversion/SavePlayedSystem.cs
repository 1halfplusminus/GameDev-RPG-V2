using Unity.Entities;
using RPG.Gameplay;
using Unity.Jobs;
using RPG.Core;
using Unity.Animation;
using Unity.Transforms;
using Unity.Deformations;
using Unity.Collections;
using System.Collections.Generic;

namespace RPG.Saving
{


    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
    public abstract class SaveConversionSystemBase<T> : SystemBase, ISavingConversionSystem where T : struct, IComponentData
    {

        public EntityManager DstEntityManager { get; }
        SaveIdentifierSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery dstQuery;

        EntityQuery query;

        List<SceneSection> sceneSections;


        public SaveConversionSystemBase(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = EntityManager.World.GetOrCreateSystem<SaveIdentifierSystem>();
            commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();
            EntityQueryDesc description = new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() },
            };
            dstQuery = DstEntityManager.CreateEntityQuery(
                description
            );
            query = EntityManager.CreateEntityQuery(description);
        }
        protected override void OnUpdate()
        {
            sceneSections = new List<SceneSection>();
            EntityManager.GetAllUniqueSharedComponentData<SceneSection>(sceneSections);
            var components = GetComponentDataFromEntity<T>(true);
            foreach (var sceneSection in sceneSections)
            {
                query.SetSharedComponentFilter(sceneSection);
                using var entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (components.HasComponent(entities[i]))
                    {
                        Convert(conversionSystem.GetTarget(entities[i]), entities[i], components, DstEntityManager, EntityManager);
                    }
                }
                dstQuery.ResetFilter();
                query.ResetFilter();
            }


        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        abstract protected void Convert(Entity dstEntity, Entity entity, ComponentDataFromEntity<T> components, EntityManager dstManager, EntityManager entityManager);
    }
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(SaveIdentifierSystem))]
    public class SavePlayedSystem : SaveConversionSystemBase<Played>
    {
        public SavePlayedSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity, ComponentDataFromEntity<Played> components, EntityManager dstManager, EntityManager entityManager)
        {
            Debug.Log($"Save played for {dstEntity}");
            DstEntityManager.AddComponent<Played>(dstEntity);
        }
    }
}
