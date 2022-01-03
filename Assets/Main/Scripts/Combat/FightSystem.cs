using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Combat
{

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
    //TODO: Move to control
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class CombatTargettingSystem : SystemBase
    {


        protected override void OnUpdate()
        {
            //FIXME: Should be in another system
            UnTargetNoHittableTarget();
            var hittables = GetComponentDataFromEntity<Hittable>(true);
            Entities
            .WithReadOnly(hittables)
            .ForEach((Entity e, ref Fighter fighter, in DynamicBuffer<HittedByRaycast> rayHits, in MouseClick mouseClick) =>
            {
                fighter.TargetFoundThisFrame = 0;
                foreach (var rayHit in rayHits)
                {
                    if (hittables.HasComponent(rayHit.Hitted))
                    {
                        if (rayHit.Hitted != e)
                        {
                            if (mouseClick.CapturedThisFrame)
                            {
                                fighter.Target = rayHit.Hitted;
                            }
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
    [UpdateAfter(typeof(HitSystem))]
    public class WasHittedSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;

        EntityQuery hitQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            if (hitQuery.CalculateEntityCount() > 0)
            {
                var wasHittedByEntities = GetBufferFromEntity<WasHitted>(false);
                // Create hit event
                Entities
                .WithReadOnly(wasHittedByEntities)
                .WithStoreEntityQueryInField(ref hitQuery)
                .ForEach((Entity e, int entityInQueryIndex, in Hit hit) =>
                {
                    if (wasHittedByEntities.HasComponent(hit.Hitted))
                    {
                        var buffer = cbp.SetBuffer<WasHitted>(entityInQueryIndex, hit.Hitted);
                        buffer.Capacity = 10;
                        buffer.Add(new WasHitted { Hitter = hit.Hitter });
                        if (buffer.Length == 10)
                        {
                            buffer.RemoveAt(0);
                        }
                    }

                }).ScheduleParallel();

            }
            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(FightSystem))]
    public class HitSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        EntityQuery hittedEntity;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            hittedEntity = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]{
                    typeof(Hit)
                }
            });
        }
        protected override void OnUpdate()
        {
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            cb.DestroyEntitiesForEntityQuery(hittedEntity);
            // Create hit event
            Entities
            .WithAny<IsFighting>()
            .ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<HitEvent> hitEvents, in Fighter fighter, in DeltaTime time) =>
            {
                if (fighter.CurrentAttack.TimeElapsedSinceAttack >= 0 && fighter.Attacking)
                {
                    for (int i = 0; i < hitEvents.Length; i++)
                    {
                        var hitEvent = hitEvents[i];
                        var distance = math.distance(fighter.CurrentAttack.TimeElapsedSinceAttack, hitEvent.Time);
                        var shouldFire = distance <= time.Value && distance >= 0;
                        var upperBound = hitEvent.Time + 0.1;
                        var lowerBound = hitEvent.Time - 0.1;
                        if (fighter.CurrentAttack.TimeElapsedSinceAttack - time.Value <= hitEvent.Time && fighter.CurrentAttack.TimeElapsedSinceAttack > hitEvent.Time)
                        {
                            // hitEvent.Fired = true;
                            // hitEvents[i] = hitEvent;
                            var eventEntity = cbp.CreateEntity(entityInQueryIndex);
                            cbp.AddComponent(entityInQueryIndex, eventEntity, new Hit { Hitted = fighter.Target, Hitter = e });
                            break;

                        }
                    }
                }
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(Dependency);

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
