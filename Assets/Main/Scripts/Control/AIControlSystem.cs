using RPG.Combat;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Core;
namespace RPG.Control
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class SuspiciousSystem : SystemBase
    {
        EntityCommandBufferSystem ecs;
        protected override void OnCreate()
        {
            base.OnCreate();
            ecs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = ecs.CreateCommandBuffer().AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, Entity e, ref Suspicious suspicious, in DeltaTime time) =>
            {
                if (suspicious.StartedThisFrame)
                {
                    commandBuffer.AddComponent<IsSuspicious>(entityInQueryIndex, e);
                }
                if (suspicious.IsStarted)
                {
                    suspicious.Update(time.Value);
                }
                if (suspicious.IsFinish)
                {
                    suspicious.Reset();
                    commandBuffer.RemoveComponent<IsSuspicious>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            ecs.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(ChaseBehaviourSystem))]
    public class GuardBehaviorSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }
        protected override void OnUpdate()
        {
            Entities.WithAll<Spawned, GuardOriginalLocationTag>().ForEach((ref GuardLocation guardLocation, in Translation translation) =>
            {
                guardLocation.Value = translation.Value;
            }).ScheduleParallel();

            Entities.
            WithNone<Spawned, IsSuspicious>()
            .ForEach((Entity e, int entityInQueryIndex, ref MoveTo moveTo, ref Fighter fighter, in GuardLocation guardLocation, in DeltaTime time) =>
              {
                  if (fighter.Target == Entity.Null)
                  {
                      moveTo.Position = guardLocation.Value;
                      moveTo.Stopped = false;
                  }
              }).ScheduleParallel();

        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class ChaseBehaviourSystem : SystemBase
    {
        EntityQuery playerControlledQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            playerControlledQuery = GetEntityQuery(typeof(PlayerControlled), ComponentType.ReadOnly<LocalToWorld>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var playerPositions = new NativeHashMap<Entity, LocalToWorld>(playerControlledQuery.CalculateEntityCount(), Allocator.TempJob);
            var playerPositionsWriter = playerPositions.AsParallelWriter();
            Entities
            .WithDisposeOnCompletion(playerPositionsWriter)
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, in LocalToWorld position) =>
            {
                playerPositionsWriter.TryAdd(e, position);
            }).ScheduleParallel();
            // Todo: Refractor with a event system create a event when target lost & when target aquired
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithReadOnly(playerPositions)
            .WithDisposeOnCompletion(playerPositions)
            .WithAny<AIControlled>()
            .ForEach((int entityInQueryIndex, Entity e, ref Fighter fighter, ref MoveTo moveTo, in ChasePlayer chasePlayer, in LocalToWorld localToWorld) =>
            {
                var localToWorlds = playerPositions.GetValueArray(Allocator.Temp);
                var entities = playerPositions.GetKeyArray(Allocator.Temp);
                var currentTarget = fighter.Target;
                for (int i = 0; i < localToWorlds.Length; i++)
                {
                    var playerLocalToWorld = localToWorlds[i];
                    var entity = entities[i];
                    if (math.abs(math.distance(localToWorld.Position, playerLocalToWorld.Position)) <= chasePlayer.ChaseDistance)
                    {
                        fighter.Target = entity;
                        fighter.MoveTowardTarget = true;
                        moveTo.Stopped = false;

                    }
                    // Todo: Refractor with a event system create a event when target lost & when target aquired
                    // Loose Current Target
                    else if (entity == currentTarget)
                    {
                        fighter.Target = Entity.Null;
                        if (HasComponent<Suspicious>(e))
                        {
                            var suspicious = GetComponent<Suspicious>(e);
                            suspicious.Start();
                            commandBuffer.SetComponent(entityInQueryIndex, e, suspicious);
                        }
                        if (HasComponent<LookAt>(e))
                        {
                            var lookAt = GetComponent<LookAt>(e);
                            lookAt.Entity = Entity.Null;
                            commandBuffer.SetComponent(entityInQueryIndex, e, lookAt);
                        }
                    }
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}