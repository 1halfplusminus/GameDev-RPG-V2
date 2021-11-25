using Unity.Entities;
using Unity.Collections;
using static Unity.Entities.EntityRemapUtility;

namespace RPG.Saving
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    public class SaveIdentifierSystem : SystemBase, ISavingConversionSystem
    {
        public EntityManager DstEntityManager { get; }
        IdentifiableSystem conversionSystem;

        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery saveIdentifiedQuery;

        EntityQuery dstIdentifiedQuery;

        NativeArray<EntityRemapInfo> remapInfos;
        public SaveIdentifierSystem(EntityManager entityManager)
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
            remapInfos = EntityManager.CreateEntityRemapArray(Allocator.Temp);
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (indexedDstIdentified.ContainsKey(id))
                {
                    EntityRemapUtility.AddEntityRemapping(ref remapInfos, indexIdentified[id], indexedDstIdentified[id]);
                }
                else
                {
                    var entity = DstEntityManager.CreateEntity(new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() });
                    var sceneTag = EntityManager.GetSharedComponentData<SceneSection>(indexIdentified[id]);
                    DstEntityManager.AddComponentData(entity, new Identifier { Id = id });
                    DstEntityManager.AddSharedComponentData(entity, sceneTag);
                    EntityRemapUtility.AddEntityRemapping(ref remapInfos, indexIdentified[id], entity);
                }
            }
            /* using var identified = saveIdentifiedQuery.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var identifiedEntity = saveIdentifiedQuery.ToEntityArray(Allocator.Temp);
            using var dstIdentified = dstIdentifiedQuery.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var dstEntities = dstIdentifiedQuery.ToEntityArray(Allocator.Temp);
            remapInfos = new NativeArray<EntityRemapInfo>(identified.Length, Allocator.Persistent);
            var archetype = DstEntityManager.CreateArchetype(new ComponentType[] { typeof(Identifier) });
            for (int i = 0; i < identified.Length; i++)
            {
                for (int j = 0; j < dstIdentified.Length; j++)
                {
                    if (dstIdentified[i].Id == identified[i].Id)
                    {
                        if (i < dstEntities.Length)
                        {
                            EntityRemapUtility.AddEntityRemapping(ref remapInfos, identifiedEntity[i], dstEntities[i]);
                        }
                        var target = EntityRemapUtility.RemapEntity(ref remapInfos, identifiedEntity[i]);
                        if (target == Entity.Null)
                        {

                            var entity = DstEntityManager.CreateEntity(new ComponentType[] { ComponentType.ReadOnly<Identifier>(), ComponentType.ReadOnly<SceneSection>() });
                            var sceneTag = EntityManager.GetSharedComponentData<SceneSection>(identifiedEntity[i]);
                            DstEntityManager.AddComponentData(entity, identified[i]);
                            DstEntityManager.AddSharedComponentData(entity, sceneTag);

                        }
                    }
                }
 */
        }

        public Entity GetTarget(Entity entity)
        {
            return EntityRemapUtility.RemapEntity(ref remapInfos, entity);
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

}
