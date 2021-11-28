using Unity.Entities;
using Unity.Collections;
using static Unity.Entities.EntityRemapUtility;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class MapIdentifierSystem : SystemBase, ISavingConversionSystem
    {
        public EntityManager DstEntityManager { get; }

        EntityQuery saveIdentifiedQuery;

        EntityQuery dstIdentifiedQuery;

        NativeArray<EntityRemapInfo> remapInfos;
        public MapIdentifierSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var description = new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Identifier>() }
            };
            saveIdentifiedQuery = EntityManager.CreateEntityQuery(
               description
            );
            dstIdentifiedQuery = DstEntityManager.CreateEntityQuery(
              description
            );

        }
        protected override void OnUpdate()
        {


            using var indexIdentified = IdentifiableSystem.IndexQuery(saveIdentifiedQuery);
            using var indexedDstIdentified = IdentifiableSystem.IndexQuery(dstIdentifiedQuery);
            using var ids = indexIdentified.GetKeyArray(Allocator.Temp);
            remapInfos = EntityManager.CreateEntityRemapArray(Allocator.Persistent);
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (indexedDstIdentified.ContainsKey(id))
                {
                    AddEntityRemapping(ref remapInfos, indexIdentified[id], indexedDstIdentified[id]);
                }

            }
        }

        public Entity GetTarget(Entity entity)
        {
            return RemapEntity(ref remapInfos, entity);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (remapInfos.IsCreated)
            {
                remapInfos.Dispose();
            }
        }
    }



    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateBefore(typeof(MapIdentifierSystem))]
    public class CreateIdentifierSystem : SystemBase, ISavingConversionSystem
    {
        public EntityManager DstEntityManager { get; }

        EntityQuery saveIdentifiedQuery;

        EntityQuery dstIdentifiedQuery;


        public CreateIdentifierSystem(EntityManager entityManager)
        {
            DstEntityManager = entityManager;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var description = new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() }
            };
            saveIdentifiedQuery = EntityManager.CreateEntityQuery(
               description
            );
            dstIdentifiedQuery = DstEntityManager.CreateEntityQuery(
              description
            );

        }
        protected override void OnUpdate()
        {


            using var indexIdentified = IdentifiableSystem.IndexQuery(saveIdentifiedQuery);
            using var indexedDstIdentified = IdentifiableSystem.IndexQuery(dstIdentifiedQuery);
            using var ids = indexIdentified.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < ids.Length; i++)
            {

                var entity = Entity.Null;
                var id = ids[i];
                var sceneTag = EntityManager.GetSharedComponentData<SceneSection>(indexIdentified[id]);
                if (!indexedDstIdentified.ContainsKey(id))
                {
                    entity = DstEntityManager.CreateEntity(new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() });

                }
                else
                {
                    entity = indexedDstIdentified[id];
                }
                DstEntityManager.AddComponentData(entity, new Identifier { Id = id });
                DstEntityManager.AddSharedComponentData(entity, sceneTag);
            }
        }

    }

}
