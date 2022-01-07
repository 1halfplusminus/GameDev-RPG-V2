using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using RPG.Core;
using UnityEngine.VFX;
using RPG.Gameplay;
using Unity.Rendering;

namespace RPG.Combat
{
    public struct DestroyIfNoParticule : IComponentData
    {

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class DestroyVisualEffect : SystemBase
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

    // [UpdateInGroup(typeof(CombatSystemGroup))]
    // [UpdateBefore(typeof(HitSystem))]
    // public class DestroyProjectileOnHitSystem : SystemBase
    // {

    //     EntityQuery queryToDestroy;
    //     EntityCommandBufferSystem entityCommandBufferSystem;
    //     protected override void OnCreate()
    //     {
    //         base.OnCreate();
    //         queryToDestroy = GetEntityQuery(ComponentType.ReadOnly<Projectile>(), ComponentType.ReadOnly<ProjectileHitted>(), ComponentType.ReadOnly<DestroyProjectileOnHit>());
    //         entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    //         RequireForUpdate(queryToDestroy);
    //     }
    //     protected override void OnUpdate()
    //     {
    //         var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
    //         Entities.WithAll<Projectile, ProjectileHitted, DestroyProjectileOnHit>()
    //         .ForEach((int entityInQueryIndex, Entity e) =>
    //         {
    //             // cbp.DestroyEntity(entityInQueryIndex, e);
    //         }).ScheduleParallel();
    //         entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    //     }
    // }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(HitSystem))]
    public class ProjectileSystem : SystemBase
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
            .WithAny<IsHomingProjectile>()
            .ForEach((int entityInQueryIndex, Entity e, ref TargetLook targetLook, ref Rotation r, in Projectile p, in LocalToWorld localToWorld) =>
            {
                targetLook.TargetDirection = LookTarget(e, p, localToWorld, isDead, hitableLocalToWorld);
                r.Value = quaternion.LookRotation(targetLook.TargetDirection, math.up());
            }).ScheduleParallel();

            Entities
            .WithNone<ProjectileHitted>()
            .ForEach((int entityInQueryIndex, Entity e, ref Translation t, in LocalToWorld localToWorld, in Projectile p, in DeltaTime dt) =>
            {
                Debug.Log($"Projectile {e} moving to position {localToWorld.Forward}  ");
                t.Value += math.normalize(localToWorld.Forward) * p.Speed * dt.Value;
            }).ScheduleParallel();

            Entities
            .WithReadOnly(isDead)
            .WithNone<ProjectileHitted>()
            .ForEach((int entityInQueryIndex, Entity e, in Projectile p, in DynamicBuffer<StatefulTriggerEvent> ste) =>
            {
                for (int i = 0; i < ste.Length; i++)
                {
                    var other = ste[i].GetOtherEntity(e);
                    if (other != p.ShootBy)
                    {
                        if (!isDead.HasComponent(other))
                        {
                            Debug.Log($"Projectile {e} collid with {other.Index}  ");
                            var hitEntity = cbp.CreateEntity(entityInQueryIndex);
                            cbp.AddComponent(entityInQueryIndex, hitEntity, new Hit() { Hitter = p.ShootBy, Hitted = other });
                            cbp.AddComponent<IsProjectile>(entityInQueryIndex, hitEntity);
                            Debug.Log($"Projectile {e} destroyed by {other.Index}  ");
                            cbp.AddComponent<ProjectileHitted>(entityInQueryIndex, e);
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
    public class ShootProjectileSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery hitQuery;
        EntityQuery hitPoinQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var canShootProjectiles = GetComponentDataFromEntity<ShootProjectile>(true);
            var localToWorlds = GetComponentDataFromEntity<LocalToWorld>(true);
            var projectiles = GetComponentDataFromEntity<Projectile>(true);
            var hitableLocalToWorld = QueryHitPoint();

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
                        var lookRotation = quaternion.LookRotation(targetPosition - position, math.up());

                        cbp.AddComponent(entityInQueryIndex, projectileEntity, translation);
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, new Projectile { Target = hit.Hitted, Speed = projectile.Speed, ShootBy = hit.Hitter });
                        cbp.AddComponent(entityInQueryIndex, projectileEntity, new Rotation { Value = lookRotation });
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
