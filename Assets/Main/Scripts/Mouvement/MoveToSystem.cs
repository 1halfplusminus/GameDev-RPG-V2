
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.AI;
using Unity.Mathematics;
using RPG.Core;
using Unity.AI.Navigation;
using UnityEngine;

namespace RPG.Mouvement
{
    public class MouvementSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(MouvementSystemGroup))]

    public class MoveToSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Calcule distance 
            Entities
            .WithNone<IsDeadTag>().WithChangeFilter<MoveTo, LocalToWorld>().ForEach((ref MoveTo moveTo, in LocalToWorld localToWorld) =>
            {
                moveTo.Distance = math.distance(moveTo.Position, localToWorld.Position);

            }).ScheduleParallel();

            // Stop if arrived a destination
            Entities
            .WithNone<IsDeadTag>()
            .WithChangeFilter<MoveTo>().ForEach((Entity e, ref MoveTo moveTo, in LocalToWorld localToWorld) =>
            {
                if (moveTo.Distance <= moveTo.StoppingDistance)
                {
                    moveTo.Stopped = true;
                    moveTo.Position = localToWorld.Position;
                }
            }).ScheduleParallel();

            // Put velocity at zero if stopped
            Entities
            .WithNone<IsDeadTag>()
            .WithChangeFilter<MoveTo>()
            .ForEach((Entity e, ref Mouvement mouvement, in MoveTo moveTo) =>
            {
                if (moveTo.Stopped)
                {
                    mouvement.Velocity = new Velocity { Linear = float3.zero, Angular = float3.zero };
                }
            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(MouvementSystemGroup))]
    [UpdateAfter(typeof(MoveToSystem))]
    public class MoveToNavMeshAgentSystem : SystemBase
    {

        EntityQuery navMeshAgentQueries;
        protected override void OnCreate()
        {
            base.OnCreate();
        }
        protected override void OnUpdate()
        {

            // TODO : Refractor
            var lookAts = GetComponentDataFromEntity<LookAt>(true);
            Entities
            .WithReadOnly(lookAts)
            .WithChangeFilter<MoveTo>()
            .WithStoreEntityQueryInField(ref navMeshAgentQueries)
            .WithoutBurst()
            .WithAll<Mouvement>()
            .WithNone<IsDeadTag>()
            .ForEach((Entity e, NavMeshAgent agent, ref Translation position, ref Mouvement mouvement, ref MoveTo moveTo, ref Rotation rotation) =>
            {

                if (agent.isOnNavMesh && !moveTo.Stopped)
                {
                    agent.SetDestination(moveTo.Position);
                    position.Value = agent.transform.position;
                    mouvement.Velocity = new Velocity
                    {
                        Linear = agent.transform.InverseTransformDirection(agent.velocity),
                        Angular = agent.angularSpeed

                    };
                    mouvement.Speed = agent.speed;
                    if (!lookAts.HasComponent(e) || lookAts[e].Entity == Entity.Null)
                    {
                        rotation.Value = agent.transform.rotation;
                    }

                }
                else
                {
                    agent.Warp(position.Value);
                }
            }).Run();

        }
    }

}
