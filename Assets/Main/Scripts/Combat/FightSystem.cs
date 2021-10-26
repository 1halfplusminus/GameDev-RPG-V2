using Unity.Entities;
using RPG.Core;
using UnityEngine;
using Unity.Transforms;
using RPG.Mouvement;

namespace RPG.Combat
{
    [UpdateAfter(typeof(MouvementSystemGroup))]
    public class CombatSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class MoveTowardTargetSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var positionInWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            Entities
            .WithReadOnly(positionInWorlds)
            .ForEach((ref MoveTo moveTo, ref Fighter fighter, in LocalToWorld localToWorld) =>
            {
                if (fighter.Target != Entity.Null && fighter.MoveTowardTarget == true)
                {
                    if (positionInWorlds.HasComponent(fighter.Target))
                    {
                        var targetPosition = positionInWorlds[fighter.Target].Position;
                        moveTo.Position = positionInWorlds[fighter.Target].Position;
                        // Range of the weapon
                        moveTo.StoppingDistance = 2f;
                    }
                }
                if (fighter.MoveTowardTarget == true && moveTo.Distance <= moveTo.StoppingDistance)
                {
                    fighter.MoveTowardTarget = false;
                }
            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class CombatTargettingSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
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
            Entities.WithAll<Fighter>().ForEach((Entity e, in Fighter fighter) =>
            {
                if (fighter.Target != Entity.Null)
                {
                    Debug.Log("Entity  e:" + e.Index + " target : " + fighter.Target.Index);
                }
            }).ScheduleParallel();
        }
    }
}
