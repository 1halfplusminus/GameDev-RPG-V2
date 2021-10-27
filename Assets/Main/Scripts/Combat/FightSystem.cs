using Unity.Entities;
using RPG.Core;
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
                if (fighter.Target == Entity.Null)
                {
                    fighter.TargetInRange = false;
                }
                if (fighter.Target != Entity.Null)
                {
                    if (positionInWorlds.HasComponent(fighter.Target) && fighter.MoveTowardTarget == true)
                    {
                        var targetPosition = positionInWorlds[fighter.Target].Position;
                        moveTo.Position = positionInWorlds[fighter.Target].Position;
                        // Range of the weapon
                        moveTo.StoppingDistance = fighter.WeaponRange;
                        if (moveTo.Distance <= fighter.WeaponRange)
                        {
                            fighter.TargetInRange = true;
                        }
                    }

                }
                // Check if fighter arrive at target
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
                fighter.TargetFoundThisFrame = 0;
                foreach (var rayHit in rayHits)
                {
                    if (hittables.HasComponent(rayHit.Hitted))
                    {
                        fighter.Target = rayHit.Hitted;
                        fighter.TargetFoundThisFrame += 1;
                    }

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
                    /*    var debug = "Entity  e:" + e.Index + " target : " + fighter.Target.Index;
                       Debug.Log(debug); */
                }
            }).ScheduleParallel();
        }
    }

    [UpdateAfter(typeof(CombatTargettingSystem))]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class FightAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Fighter>().ForEach((ref CharacterAnimation characterAnimation, in Fighter fighter) =>
            {
                if (fighter.TargetInRange)
                {
                    characterAnimation.Attack = 1.0f;
                }
                else
                {
                    characterAnimation.Attack = 0.0f;
                }
            }).ScheduleParallel();
        }
    }
}
