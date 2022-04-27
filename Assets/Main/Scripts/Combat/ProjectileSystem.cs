using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using RPG.Core;
using UnityEngine.VFX;
// using RPG.Gameplay;
using Unity.Rendering;
using Unity.Physics;

namespace RPG.Combat
{
    public struct DestroyIfNoParticule : IComponentData
    {

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial class DestroyVisualEffect : SystemBase
    {

        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithAll<Projectile, ProjectileHitted, SetAttributeDestroyOnHit>()
            .WithNone<Playing>()
            .ForEach((int entityInQueryIndex, Entity e, VisualEffect effect) =>
            {
                if (effect.HasBool("Destroy"))
                {
                    effect.SetBool("Destroy", true);
                    cb.AddComponent<DestroyIfNoParticule>(e);
                    cb.AddComponent<Playing>(e);
                }
            }).WithoutBurst().Run();


            Entities
           .WithAll<Projectile, ProjectileHitted, OnDestroyOnHit>()
           .WithNone<Playing>()
           .ForEach((int entityInQueryIndex, Entity e, VisualEffect effect, DynamicBuffer<Child> children) =>
           {
               Debug.Log($"Send Destroy Event");
               effect.SendEvent("OnDestroy");
               cb.AddComponent<DisableRendering>(e);
               cb.AddComponent<Playing>(e);
               foreach (var child in children)
               {
                   cb.AddComponent<DisableRendering>(child.Value);
               }
           }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }


    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(HitSystem))]
    public partial class ProjectileSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery hitableQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var isDead = GetComponentDataFromEntity<IsDeadTag>(true);
            NativeHashMap<Entity, LocalToWorld> hitableLocalToWorld = QueryHitPoint();

            Entities
            .WithReadOnly(isDead)
            .WithReadOnly(hitableLocalToWorld)
            .WithDisposeOnCompletion(hitableLocalToWorld)
            .WithNone<ProjectileHitted>()
            .WithAny<IsHomingProjectile>()
            .ForEach((int entityInQueryIndex, Entity e, ref PhysicsVelocity v, ref Rotation r, in Projectile p, in LocalToWorld localToWorld) =>
            {
                var direction = LookTarget(e, p, localToWorld, isDead, hitableLocalToWorld);
                var lookRotation = quaternion.LookRotation(direction, math.up());
                v.Linear = math.normalize(direction) * p.Speed;

            }).ScheduleParallel();


            Entities
            .WithReadOnly(isDead)
            .WithNone<ProjectileHitted>()
            .ForEach((int entityInQueryIndex, Entity e, in Projectile p, in DynamicBuffer<StatefulTriggerEvent> ste) =>
            {
                var targetHit = 0;
                for (int i = 0; i < ste.Length; i++)
                {
                    var other = ste[i].GetOtherEntity(e);
                    if (other != p.ShootBy)
                    {
                        if (!isDead.HasComponent(other))
                        {
                            Debug.Log($"Projectile {e} collid with {other.Index}  ");
                            var hitEntity = cbp.CreateEntity(entityInQueryIndex);
                            cbp.RemoveComponent<PhysicsVelocity>(entityInQueryIndex, e);
                            cbp.AddComponent(entityInQueryIndex, hitEntity, new Hit() { Hitter = p.ShootBy, Hitted = other });
                            cbp.AddComponent<IsProjectile>(entityInQueryIndex, hitEntity);
                            Debug.Log($"Projectile {e} destroyed by {other.Index}  ");
                            cbp.AddComponent<ProjectileHitted>(entityInQueryIndex, e);
                            targetHit++;
                            if (targetHit >= p.MaxTargetHit)
                            {
                                break;
                            }
                        }
                    }

                }

            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private static float3 LookTarget(Entity e, in Projectile p, in LocalToWorld localToWorld, ComponentDataFromEntity<IsDeadTag> isDead, NativeHashMap<Entity, LocalToWorld> hitableLocalToWorld)
        {

            if (hitableLocalToWorld.ContainsKey(p.Target) && !isDead.HasComponent(p.Target))
            {
                Debug.Log($"Projectile {e} targetting {p.Target.Index}  ");
                return hitableLocalToWorld[p.Target].Position - localToWorld.Position;
            }
            return float3.zero;
        }

        private NativeHashMap<Entity, LocalToWorld> QueryHitPoint()
        {
            var hitableLocalToWorld = new NativeHashMap<Entity, LocalToWorld>(hitableQuery.CalculateEntityCount(), Allocator.TempJob);
            var hitableLocalToWorldWriter = hitableLocalToWorld.AsParallelWriter();
            Entities.WithStoreEntityQueryInField(ref hitableQuery)
            .ForEach((Entity e, int entityInQueryIndex, in LocalToWorld localToWorld, in HitPoint hitPoint) =>
            {
                hitableLocalToWorldWriter.TryAdd(hitPoint.Entity, localToWorld);
            }).ScheduleParallel();
            return hitableLocalToWorld;
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    //FIXME: Create a Hit System Group
    [UpdateBefore(typeof(HealthSystem))]
    [UpdateAfter(typeof(ProjectileSystem))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial class ShootProjectileSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery hitQuery;
        EntityQuery hitPoinQuery;
        EntityQuery projectileShootedQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            projectileShootedQuery = GetEntityQuery(typeof(Projectile), typeof(ProjectileShooted));
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var canShootProjectiles = GetComponentDataFromEntity<ShootProjectile>(true);
            var localToWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            var projectiles = GetComponentDataFromEntity<Projectile>(true);
            var hitableLocalToWorld = QueryHitPoint();
            cb.RemoveComponentForEntityQuery<ProjectileShooted>(projectileShootedQuery);
            Entities
            .WithStoreEntityQueryInField(ref hitQuery)
            .WithNone<IsProjectile>()
            .WithReadOnly(localToWorlds)
            .WithReadOnly(projectiles)
            .WithReadOnly(hitableLocalToWorld)
            .WithDisposeOnCompletion(hitableLocalToWorld)
            .WithReadOnly(canShootProjectiles)
            .ForEach((int entityInQueryIndex, Entity e, ref Hit hit) =>
            {
                if (canShootProjectiles.HasComponent(hit.Hitter) && hitableLocalToWorld.ContainsKey(hit.Hitted))
                {
                    Debug.Log($"{hit.Hitter} shoot a projectile at {hit.Hitted}");
                    var socket = canShootProjectiles[hit.Hitter].Socket;
                    var prefabEntity = canShootProjectiles[hit.Hitter].Prefab;
                    var projectile = projectiles[prefabEntity];
                    var targetPosition = hitableLocalToWorld[hit.Hitted].Position;
                    if (localToWorlds.HasComponent(socket))
                    {
                        var position = localToWorlds[socket].Position;
                        var translation = new Translation { Value = position };
                        var projectileEntity = cbp.Instantiate(entityInQueryIndex, prefabEntity);
                        var direction = targetPosition - position;
                        var angle = math.abs(math.degrees(math.atan2(direction.y, direction.x)));
                        var lookRotation = quaternion.LookRotation(direction, math.up());
                        var linearVelocity = math.normalize(direction) * projectile.Speed;
                        linearVelocity.y = 0;
                        cbp.AddComponent<LocalToWorld>(entityInQueryIndex, projectileEntity);
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, translation);
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, new PhysicsVelocity
                        {
                            Angular = new float3(lookRotation.value.x, 0, 0),
                            Linear = linearVelocity
                        });
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, new Projectile
                        {
                            Target = hit.Hitted,
                            Speed = projectile.Speed,
                            ShootBy = hit.Hitter,
                            MaxTargetHit = projectile.MaxTargetHit
                        });
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, new Rotation { Value = lookRotation });
                        cbp.AddComponent<ProjectileShooted>(entityInQueryIndex, projectileEntity);
                        hit.Damage = 0;
                    }

                }

            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }

        private NativeHashMap<Entity, LocalToWorld> QueryHitPoint()
        {
            var hitableLocalToWorld = new NativeHashMap<Entity, LocalToWorld>(hitPoinQuery.CalculateEntityCount(), Allocator.TempJob);
            var hitableLocalToWorldWriter = hitableLocalToWorld.AsParallelWriter();
            Entities.WithStoreEntityQueryInField(ref hitPoinQuery)
            .ForEach((Entity e, int entityInQueryIndex, in LocalToWorld localToWorld, in HitPoint hitPoint) =>
            {
                hitableLocalToWorldWriter.TryAdd(hitPoint.Entity, localToWorld);
            }).ScheduleParallel();
            return hitableLocalToWorld;
        }
    }
}
