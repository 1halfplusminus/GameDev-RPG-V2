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

            Entities.WithNone<Spawned>().ForEach((ref MoveTo moveTo, ref Fighter fighter, ref LookAt lookAt, in GuardLocation guardLocation) =>
            {
                if (fighter.Target == Entity.Null)
                {
                    moveTo.Position = guardLocation.Value;
                    moveTo.Stopped = false;
                    lookAt.Entity = Entity.Null;
                }
            }).ScheduleParallel();
        }
    }
    public class ChaseBehaviourSystem : SystemBase
    {
        EntityQuery playerControlledQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            playerControlledQuery = GetEntityQuery(typeof(PlayerControlled), ComponentType.ReadOnly<LocalToWorld>());
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


            Entities
            .WithReadOnly(playerPositions)
            .WithDisposeOnCompletion(playerPositions)
            .WithAny<AIControlled>()
            .ForEach((ref Fighter fighter, ref MoveTo moveTo, in ChasePlayer chasePlayer, in LocalToWorld localToWorld) =>
            {
                var localToWorlds = playerPositions.GetValueArray(Allocator.Temp);
                var entities = playerPositions.GetKeyArray(Allocator.Temp);
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
                    else
                    {
                        fighter.Target = Entity.Null;
                    }
                }
            }).ScheduleParallel();


        }
    }
}