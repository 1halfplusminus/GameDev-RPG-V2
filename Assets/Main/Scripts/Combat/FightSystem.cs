using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;

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
            .ForEach((Entity e, ref Fighter fighter, in DynamicBuffer<HittedByRaycast> rayHits) =>
            {
                fighter.TargetFoundThisFrame = 0;
                foreach (var rayHit in rayHits)
                {
                    if (hittables.HasComponent(rayHit.Hitted))
                    {
                        if (rayHit.Hitted != e)
                        {
                            fighter.Target = rayHit.Hitted;
                            fighter.TargetFoundThisFrame += 1;
                        }
                   
                    }

                }

            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class HitSystem : SystemBase
    {
        EntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;

        EntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            FireEvent();
            CleanUp();
        }
        public void CleanUp()
        {

            var commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            // Pass hit event as not fired
            Entities.WithChangeFilter<Fighter>().ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<HitEvent> hitEvents, in Fighter fighter) =>
            {
                if (fighter.currentAttack.InCooldown)
                {
                    for (int i = 0; i < hitEvents.Length; i++)
                    {
                        var hitEvent = hitEvents[i];
                        hitEvent.Fired = false;
                        hitEvents[i] = hitEvent;
                    }
                }
            }).ScheduleParallel();
            // Clean up hit event 
            Entities.WithAll<Hit>().ForEach((Entity e, int entityInQueryIndex) =>
            {
                commandBuffer.DestroyEntity(entityInQueryIndex, e);
            }).ScheduleParallel();
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
        public void FireEvent()
        {
            var commandBuffer = beginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            // Create hit event
            Entities.ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<HitEvent> hitEvents, in Fighter fighter) =>
            {
                if (fighter.currentAttack.TimeElapsedSinceAttack >= 0)
                {
                    for (int i = 0; i < hitEvents.Length; i++)
                    {
                        var hitEvent = hitEvents[i];
                        if (hitEvent.Fired == false && fighter.currentAttack.TimeElapsedSinceAttack >= hitEvent.Time)
                        {
                            hitEvent.Fired = true;
                            hitEvents[i] = hitEvent;
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new Hit { Hitted = fighter.Target, Hitter = e });
                        }
                    }
                }
            }).ScheduleParallel();

            beginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
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

            ThrottleAttack();
        }


        private void ThrottleAttack()
        {
            Entities.ForEach((ref Fighter fighter, in DeltaTime deltaTime) =>
            {
                // It attack if time elapsed since last attack >= duration of the attack
                if (fighter.currentAttack.TimeElapsedSinceAttack >= fighter.AttackDuration)
                {
                    fighter.currentAttack.Cooldown = fighter.AttackCooldown;
                    fighter.Attacking = false;
                    fighter.currentAttack.TimeElapsedSinceAttack = 0.0f;

                }
                // It  increase the time elapsed since attack
                if (fighter.Attacking)
                {
                    fighter.currentAttack.TimeElapsedSinceAttack += deltaTime.Value;
                }
                // It cancel attack if fighter move || no target in range
                if (fighter.MoveTowardTarget || !fighter.TargetInRange)
                {
                    fighter.Attacking = false;
                }

                // It attack if target in range & the attack cooldown is at 0
                if (fighter.TargetInRange && !fighter.Attacking && fighter.currentAttack.Cooldown <= 0 && !fighter.MoveTowardTarget)
                {
                    fighter.Attacking = true;
                    fighter.currentAttack.TimeElapsedSinceAttack = 0.0f;
                }
                // deacrease attack cooldwon if fighter not attack & have a target
                if (fighter.currentAttack.Cooldown >= 0)
                {
                    fighter.currentAttack.InCooldown = true;
                    fighter.currentAttack.Cooldown -= deltaTime.Value;
                }
                else
                {
                    fighter.currentAttack.InCooldown = false;
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
                if (fighter.currentAttack.InCooldown && characterAnimation.AttackCooldown <= 1)
                {
                    characterAnimation.AttackCooldown += 0.1f;

                }

                if (fighter.Attacking && fighter.TargetInRange)
                {
                    characterAnimation.Attack = 1.0f;
                }

                if (!fighter.Attacking && !fighter.currentAttack.InCooldown)
                {
                    characterAnimation.Attack = 0.0f;
                }
                if (!fighter.currentAttack.InCooldown)
                {
                    characterAnimation.AttackCooldown = 0.0f;
                }
            }).ScheduleParallel();
        }
    }
}
