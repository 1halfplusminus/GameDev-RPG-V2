using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;


namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public abstract class SaveConversionSystemBase<T> : SystemBase, ISavingConversionSystem where T : struct, IComponentData
    {

        public EntityManager DstEntityManager { get; }
        MapIdentifierSystem conversionSystem;

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
            conversionSystem = EntityManager.World.GetOrCreateSystem<MapIdentifierSystem>();
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
            DstEntityManager.GetAllUniqueSharedComponentData<SceneSection>(sceneSections);

            foreach (var sceneSection in sceneSections)
            {
                using var entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (EntityManager.HasComponent<T>(entities[i]))
                    {
                        var target = conversionSystem.GetTarget(entities[i]);
                        if (target != Entity.Null)
                        {
                            Convert(conversionSystem.GetTarget(entities[i]), entities[i], EntityManager.GetComponentData<T>(entities[i]), sceneSection);
                        }

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

        abstract protected void Convert(Entity dstEntity, Entity entity, T data, SceneSection sceneSection);
    }
}
