using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using RPG.Core;
using Unity.Collections;

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
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            identifiableSpawnerQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<HasSpawn>(),
                    ComponentType.ReadOnly<SpawnIdentifier>()
                },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<HasSpawnIdentified>()
                }
            });
            RequireForUpdate(identifiableSpawnerQuery);
        }
        protected override void OnUpdate()
        {
            using var hasSpawns = identifiableSpawnerQuery.ToComponentDataArray<HasSpawn>(Allocator.Temp);
            using var spawnIdentifiers = identifiableSpawnerQuery.ToComponentDataArray<SpawnIdentifier>(Allocator.Temp);
            for (int i = 0; i < hasSpawns.Length; i++)
            {
                var hasSpawn = hasSpawns[i];
                var identifier = spawnIdentifiers[i];
                EntityManager.AddComponentData(hasSpawn.Entity, new Identifier { Id = identifier.Id });
            }
            EntityManager.AddComponent<HasSpawnIdentified>(identifiableSpawnerQuery);
        }
    }
}
