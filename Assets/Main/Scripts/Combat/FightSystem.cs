using Unity.Entities;
using RPG.Core;
using UnityEngine;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Mathematics;
namespace RPG.Combat
{
    [UpdateAfter(typeof(MouvementSystemGroup))]
    public class CombatSystemGroup : ComponentSystemGroup
    {

    }
    public struct Attack
    {
        Entity Fighter;
        Entity Target;
    }

    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class MoveTowardTargetSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var positionInWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            Entities
            .WithReadOnly(positionInWorlds)
            .ForEach((ref MoveTo moveTo, in Fighter fighter, in LocalToWorld localToWorld) =>
            {
                if (fighter.Target != Entity.Null)
                {
                    if (positionInWorlds.HasComponent(fighter.Target))
                    {
                        var targetPosition = positionInWorlds[fighter.Target].Position;
                        moveTo.Position = positionInWorlds[fighter.Target].Position;
                    }
                }

            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class CombatTargettingSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var hittables = GetComponentDataFromEntity<Hittable>(true);
            Entities
            .WithReadOnly(hittables)
            .ForEach((ref Fighter fighter, in DynamicBuffer<HittedByRaycast> rayHits) =>
            {
                var findTarget = false;
                foreach (var rayHit in rayHits)
                {
                    if (hittables.HasComponent(rayHit.Hitted))
                    {
                        fighter.Target = rayHit.Hitted;
                        findTarget = true;
                    }
                }
                if (rayHits.Length > 0 && !findTarget)
                {
                    fighter.Target = Entity.Null;
                }
            }).ScheduleParallel();
        }
    }
    [UpdateAfter(typeof(CombatTargettingSystem))]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class FightSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Fighter>().ForEach((Entity e, in Target target) =>
            {
                Debug.Log("Entity  e:" + e.Index + " target : " + target.Entity.Index);
            }).ScheduleParallel();
        }
    }
}
