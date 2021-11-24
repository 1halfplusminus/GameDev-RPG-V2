using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using RPG.Core;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SpawnableIdentifiableSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery identifiableSpawnerQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(identifiableSpawnerQuery);
        }
        protected override void OnUpdate()
        {

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithNone<HasSpawnIdentified>()
            .WithStoreEntityQueryInField(ref identifiableSpawnerQuery)
            .ForEach((int entityInQueryIndex, Entity e, in HasSpawn hasSpawn, in SpawnIdentifier identifier) =>
            {
                Debug.Log("Change identifier for spawner id");
                commandBuffer.AddComponent(entityInQueryIndex, hasSpawn.Entity, new Identifier { Id = identifier.Id });
                commandBuffer.AddComponent<HasSpawnIdentified>(entityInQueryIndex, e);
            })
            .ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
