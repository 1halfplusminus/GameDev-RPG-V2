using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class ProjectileSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithNone<TargetLook>().ForEach((int entityInQueryIndex, Entity e, ref LookAt lookAt, ref MoveTo moveTo, in Projectile p) =>
            {
                Debug.Log($"Projectile {e} targetting {p.Target.Index}  ");
                cbp.AddComponent(entityInQueryIndex, p.Target, new TargetBy { Entity = e });
                moveTo.Stopped = false;
                lookAt.Entity = p.Target;
            }).ScheduleParallel();

            Entities.ForEach((int entityInQueryIndex, Entity e, in LocalToWorld localToWorld, in TargetBy targetBy) =>
            {
                Debug.Log($"Projectile {targetBy.Entity} look target position {localToWorld.Position}  ");
                cbp.AddComponent(entityInQueryIndex, targetBy.Entity, new TargetLook { Position = localToWorld.Position });
            }).ScheduleParallel();
            Entities
            .WithAny<IsMoving>()
            .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, ref Translation t, in TargetLook targetLook, in LocalToWorld localToWorld, in Projectile p, in DeltaTime dt) =>
            {
                Debug.Log($"Projectile {e} moving to position {targetLook.Position}  ");
                moveTo.Position = targetLook.Position;
                if (!moveTo.IsAtStoppingDistance)
                {
                    moveTo.Stopped = false;
                    var direction = targetLook.Position - localToWorld.Position;
                    t.Value += math.normalize(direction) * p.Speed * dt.Value;
                }
            }).ScheduleParallel();

        }
    }
    [UpdateAfter(typeof(CombatTargettingSystem))]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class MoveTowardTargetSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var positionInWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            Entities
            .WithAny<IsFighting>()
            .WithReadOnly(positionInWorlds)
            .ForEach((int entityInQueryIndex, Entity e, ref MoveTo moveTo, ref Fighter fighter, in LocalToWorld localToWorld) =>
            {
                if (positionInWorlds.HasComponent(fighter.Target))
                {
                    var targetPosition = positionInWorlds[fighter.Target].Position;
                    if (fighter.MoveTowardTarget)
                    {
                        moveTo.Stopped = false;
                        moveTo.Position = targetPosition;

                    }
                    // Range of the weapon
                    if (math.distance(localToWorld.Position, targetPosition) <= fighter.Range + moveTo.StoppingDistance)
                    {
                        moveTo.Stopped = true;
                        fighter.TargetInRange = true;
                        fighter.MoveTowardTarget = false;
                        commandBuffer.AddComponent(entityInQueryIndex, e, new LookAt { Entity = fighter.Target });
                    }
                    else
                    {
                        fighter.TargetInRange = false;
                    }
                }

            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class CombatTargettingSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }
        protected override void OnUpdate()
        {

            UnTargetNoHittableTarget();
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
        private void UnTargetNoHittableTarget()
        {
            Entities
            .WithNone<IsDeadTag>()
            .ForEach((ref Fighter f) =>
            {
                if (!HasComponent<Hittable>(f.Target))
                {
                    f.Target = Entity.Null;
                    f.Attacking = false;
                }
            }).ScheduleParallel();
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
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
            Entities
            .WithChangeFilter<HitEvent>()
            .ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<HitEvent> hitEvents, in Fighter fighter) =>
            {
                if (fighter.CurrentAttack.InCooldown)
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
            Entities
            .WithChangeFilter<Fighter>()
            .ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<HitEvent> hitEvents, in Fighter fighter) =>
            {
                if (fighter.CurrentAttack.TimeElapsedSinceAttack >= 0)
                {
                    for (int i = 0; i < hitEvents.Length; i++)
                    {
                        var hitEvent = hitEvents[i];
                        if (hitEvent.Fired == false && fighter.CurrentAttack.TimeElapsedSinceAttack >= hitEvent.Time)
                        {
                            hitEvent.Fired = true;
                            hitEvents[i] = hitEvent;
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new Hit { Hitted = fighter.Target, Hitter = e });
                        }
                    }
                }
            }).ScheduleParallel();

            beginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateAfter(typeof(MoveTowardTargetSystem))]
    [UpdateAfter(typeof(CombatTargettingSystem))]
    [UpdateInGroup(typeof(CombatSystemGroup))]

    public class FightSystem : SystemBase
    {
        EntityCommandBufferSystem beginPresentationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            beginPresentationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = beginPresentationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, Entity e, ref Fighter fighter) =>
            {
                if (fighter.Target != Entity.Null)
                {
                    commandBuffer.AddComponent<IsFighting>(entityInQueryIndex, e);
                }
                else
                {
                    fighter.TargetInRange = false;
                    commandBuffer.RemoveComponent<IsFighting>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            beginPresentationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            ThrottleAttack();

        }


        private void ThrottleAttack()
        {
            Entities
            .WithNone<IsDeadTag>()
            .ForEach((ref Fighter fighter, in DeltaTime deltaTime) =>
                 {

                     // It attack if time elapsed since last attack >= duration of the attack
                     if (fighter.CurrentAttack.TimeElapsedSinceAttack >= fighter.AttackDuration)
                     {
                         fighter.CurrentAttack.Cooldown = fighter.Cooldown;
                         fighter.Attacking = false;
                         fighter.CurrentAttack.TimeElapsedSinceAttack = 0.0f;

                     }
                     // It  increase the time elapsed since attack
                     if (fighter.Attacking)
                     {
                         fighter.CurrentAttack.TimeElapsedSinceAttack += deltaTime.Value;
                     }
                     // It cancel attack if fighter move || no target in range
                     if (fighter.MoveTowardTarget || !fighter.TargetInRange)
                     {
                         fighter.Attacking = false;
                     }

                     // It attack if target in range & the attack cooldown is at 0
                     if (fighter.TargetInRange && !fighter.Attacking && fighter.CurrentAttack.Cooldown <= 0 && !fighter.MoveTowardTarget)
                     {
                         fighter.Attacking = true;
                         fighter.CurrentAttack.TimeElapsedSinceAttack = 0.0f;
                     }
                     // deacrease attack cooldwon if fighter not attack & have a target
                     if (fighter.CurrentAttack.Cooldown >= 0)
                     {
                         fighter.CurrentAttack.InCooldown = true;
                         fighter.CurrentAttack.Cooldown -= deltaTime.Value;
                     }
                     else
                     {
                         fighter.CurrentAttack.InCooldown = false;
                     }

                 }).ScheduleParallel();
        }
    }

    [UpdateAfter(typeof(FightSystem))]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class FightAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Fighter>().ForEach((ref CharacterAnimation characterAnimation, in Fighter fighter) =>
            {
                if (fighter.CurrentAttack.InCooldown && characterAnimation.AttackCooldown <= 1)
                {
                    characterAnimation.AttackCooldown += 0.05f;
                    characterAnimation.AttackCooldown = math.min(characterAnimation.AttackCooldown, 1f);
                }

                if (fighter.Attacking && fighter.TargetInRange)
                {
                    characterAnimation.Move = 0.0f;
                    characterAnimation.Attack += 0.1f;
                    characterAnimation.Attack = math.min(characterAnimation.Attack, 1f);
                }

                if (!fighter.CurrentAttack.InCooldown)
                {
                    characterAnimation.AttackCooldown -= 0.05f;
                    characterAnimation.AttackCooldown = math.max(characterAnimation.AttackCooldown, 0f);
                }

                if (!fighter.Attacking && !fighter.CurrentAttack.InCooldown || fighter.MoveTowardTarget)
                {
                    characterAnimation.Attack = 0.0f;
                }
            }).ScheduleParallel();
        }
    }
}
