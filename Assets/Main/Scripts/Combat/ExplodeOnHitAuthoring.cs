
using RPG.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Combat
{
    public struct ExplodeOnHit : IComponentData
    {
        public float Radius;
    }
    public class ExplodeOnHitAuthoring : MonoBehaviour
    {
        public float Radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }

    public class ExplodeOnHitConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ExplodeOnHitAuthoring explodeOnHit) =>
            {
                var entity = GetPrimaryEntity(explodeOnHit);
                DstEntityManager.AddComponentData(entity, new ExplodeOnHit { Radius = explodeOnHit.Radius });
            });
        }
    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class ExplodeOnHitSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        StepPhysicsWorld stepPhysicsWorld;
        BuildPhysicsWorld buildPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }
        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(stepPhysicsWorld.GetOutputDependency(), buildPhysicsWorld.GetOutputDependency(), Dependency);
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            Entities
            .WithReadOnly(collisionWorld)
            .WithNone<Playing>()
            .WithAll<ProjectileHitted>()
            .ForEach((Entity e, int entityInQueryIndex, in LocalToWorld localToWorld, in ExplodeOnHit explodeOnHit, in Projectile p, in PhysicsCollider collider) =>
            {
                if (collider.IsValid)
                {
                    var filter = collider.Value.Value.Filter;
                    var hits = new NativeList<ColliderCastHit>(Allocator.Temp);
                    collisionWorld.SphereCastAll(localToWorld.Position, explodeOnHit.Radius, 0f, 0f, ref hits, filter);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hitEntity = cbp.CreateEntity(entityInQueryIndex);
                        var hittedEntity = hits[i].Entity;
                        Debug.Log($"Hit with area of effect {hittedEntity.Index}");
                        cbp.AddComponent(entityInQueryIndex, hitEntity, new Hit() { Hitter = p.ShootBy, Hitted = hittedEntity });
                        cbp.AddComponent<IsProjectile>(entityInQueryIndex, hitEntity);
                    }
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}