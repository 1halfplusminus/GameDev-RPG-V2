using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

namespace RPG.Saving
{
    public struct Identifier : IComponentData
    {
        public Unity.Entities.Hash128 Id;
    }
    public struct Identified : IComponentData
    {

    }
    public interface ISavingConversionSystem
    {
        EntityManager DstEntityManager { get; }

    }
    // FIXME: Is this system used ?
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingSystemGroup))]
    [UpdateBefore(typeof(SaveSystem))]
    public class IdentifiableSystem : SystemBase
    {


        EntityQuery identiableQuery;

        EntityCommandBufferSystem entityCommandBufferSystem;

        private JobHandle outputDependency;

        public static NativeHashMap<Unity.Entities.Hash128, Entity> IndexQuery(EntityQuery query)
        {
            var ids = new NativeHashMap<Unity.Entities.Hash128, Entity>(query.CalculateEntityCount(), Allocator.TempJob);
            var datas = query.ToComponentDataArray<Identifier>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < datas.Length; i++)
            {
                if (ids.ContainsKey(datas[i].Id))
                {
                    Debug.LogWarning($"{entities[i]} and {ids[datas[i].Id]} as the same identifier : {datas[i].Id}");
                    ids.Remove(datas[i].Id);
                }
                ids.TryAdd(datas[i].Id, entities[i]);
            }
            entities.Dispose();
            datas.Dispose();
            return ids;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            RequireForUpdate(identiableQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref identiableQuery)
            .WithNone<Identified>()
            .ForEach((int entityInQueryIndex, Entity e, in Identifier identifier) =>
            {
                commandBufferP.AddComponent<Identified>(entityInQueryIndex, e);
            })
            .ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

            outputDependency = Dependency;
        }

        public JobHandle GetOutputDependency()
        {
            return outputDependency;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

    }
}
