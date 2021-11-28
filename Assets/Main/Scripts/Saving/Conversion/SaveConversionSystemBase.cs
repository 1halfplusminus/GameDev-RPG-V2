using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;


namespace RPG.Saving
{
    /** 
    FIXME: Each class that extends this system iterate over all the saveable & call convert on them
    That class should take a list of system in constructor that implement a IConvertSave interface & call convert on these system
   **/
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public abstract class SaveConversionSystemBase<T> : SystemBase, ISavingConversionSystem where T : struct, IComponentData
    {

        public EntityManager DstEntityManager { get; }
        MapIdentifierSystem conversionSystem;

        EntityQuery query;


        public SaveConversionSystemBase(EntityManager entityManager)
        {
            DstEntityManager = entityManager;

        }
        protected override void OnCreate()
        {
            base.OnCreate();
            conversionSystem = EntityManager.World.GetOrCreateSystem<MapIdentifierSystem>();
            var description = new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() },
            };
            query = EntityManager.CreateEntityQuery(description);
        }
        protected override void OnUpdate()
        {
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (EntityManager.HasComponent<T>(entities[i]))
                {
                    var target = conversionSystem.GetTarget(entities[i]);
                    if (target != Entity.Null)
                    {
                        Convert(target, entities[i]);
                    }

                }
            }
            /* sceneSections = new List<SceneSection>();
            DstEntityManager.GetAllUniqueSharedComponentData(sceneSections);

            foreach (var sceneSection in sceneSections)
            {
               
                dstQuery.ResetFilter();
                query.ResetFilter();
            } */


        }
        protected Entity GetPrimaryEntity(Entity e)
        {
            return conversionSystem.GetTarget(e);
        }
        protected T GetComponent(Entity entity)
        {
            return EntityManager.GetComponentData<T>(entity);
        }
        protected bool HasComponent(Entity entity)
        {
            return EntityManager.HasComponent<T>(entity);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        abstract protected void Convert(Entity dstEntity, Entity entity);
    }
}
