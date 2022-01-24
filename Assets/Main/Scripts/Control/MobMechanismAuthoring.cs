
using System;
using RPG.Combat;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


namespace RPG.Control
{
    [Serializable]
    public struct MobMechanism : IComponentData
    {
        public float ShoutRadius;
        public CollisionFilter CollisionFilter;
    }
    public struct LinkedMob : IBufferElementData
    {
        public Entity Entity;
    }

    public class MobMechanismAuthoring : MonoBehaviour
    {
        public FighterAuthoring[] linked;
        public float ShoutRadius;
        public PhysicsCategoryTags CollidWith;
        public PhysicsCategoryTags BelongTo;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ShoutRadius);
        }
    }

    [UpdateAfter(typeof(FighterConversionSystem))]
    public class MobMechanismConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((MobMechanismAuthoring mobMechanismAuthoring) =>
            {
                var entity = GetPrimaryEntity(mobMechanismAuthoring);
                DstEntityManager.AddComponentData(entity, new MobMechanism
                {
                    ShoutRadius = mobMechanismAuthoring.ShoutRadius,
                    CollisionFilter = new CollisionFilter { BelongsTo = mobMechanismAuthoring.BelongTo.Value, CollidesWith = mobMechanismAuthoring.CollidWith.Value }
                });
                AddLinkedMob(mobMechanismAuthoring, mobMechanismAuthoring);
                foreach (var linked in mobMechanismAuthoring.linked)
                {
                    AddLinkedMob(linked, mobMechanismAuthoring);
                }
            });
        }

        private void AddLinkedMob(Component b, MobMechanismAuthoring mobMechanismAuthoring)
        {
            var mobMechanismEntity = GetPrimaryEntity(mobMechanismAuthoring);
            var componentEntity = GetPrimaryEntity(b);
            var buffer = DstEntityManager.AddBuffer<LinkedMob>(componentEntity);
            buffer.Clear();
            if (componentEntity != mobMechanismEntity)
            {
                buffer.Add(new LinkedMob { Entity = mobMechanismEntity });
            }
            foreach (var linked in mobMechanismAuthoring.linked)
            {
                var linkedEntity = TryGetPrimaryEntity(linked);
                if (linkedEntity != Entity.Null && linkedEntity != componentEntity)
                {
                    buffer.Add(new LinkedMob { Entity = linkedEntity });
                }
            }
        }

    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class MobMechanismSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        BuildPhysicsWorld buildPhysicsWorld;

        StepPhysicsWorld stepPhysicsWorld;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        }
        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(Dependency, buildPhysicsWorld.GetOutputDependency(), stepPhysicsWorld.GetOutputDependency());
            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            var collisionWorld = physicsWorld.CollisionWorld;
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cpb = cb.AsParallelWriter();
            Entities
                   .WithReadOnly(collisionWorld)
                   .WithReadOnly(physicsWorld)
                   .WithAll<AIControlled>()
                   .WithAny<StartChaseTarget, Hitter, IsFighting>()
                   .ForEach((int entityInQueryIndex, Entity e, in MobMechanism mobMechanism, in LocalToWorld localToWorld, in DynamicBuffer<LinkedMob> linkedMob) =>
                   {
                       var target = Entity.Null;
                       if (HasComponent<StartChaseTarget>(e))
                       {
                           var startChaseTarget = GetComponent<StartChaseTarget>(e);
                           target = startChaseTarget.Target;
                       }
                       if (target == Entity.Null && HasComponent<Hitter>(e))
                       {
                           var hitter = GetComponent<Hitter>(e);
                           target = hitter.Value;
                       }
                       var linksFound = new NativeList<Entity>(Allocator.Temp);
                       var hits = new NativeList<ColliderCastHit>(Allocator.Temp);
                       collisionWorld.SphereCastAll(localToWorld.Position, mobMechanism.ShoutRadius, math.up(), 0, ref hits, mobMechanism.CollisionFilter);
                       for (int i = 0; i < hits.Length; i++)
                       {
                           var hittedEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                           if (HasComponent<MobMechanism>(hittedEntity))
                           {
                               Debug.Log($"hitted by raycast {hittedEntity.Index}");
                               linksFound.Add(hittedEntity);
                           }
                       }
                       for (int i = 0; i < linkedMob.Length; i++)
                       {
                           var linked = linkedMob[i];
                           linksFound.Add(linked.Entity);
                       }
                       for (int i = 0; i < linksFound.Length; i++)
                       {

                           var linkedEntity = linksFound[i];
                           if (linkedEntity != e && HasComponent<Fighter>(linkedEntity))
                           {
                               var fighter = GetComponent<Fighter>(linkedEntity);
                               if (fighter.Target == Entity.Null)
                               {
                                   fighter.Target = target;
                                   fighter.MoveTowardTarget = true;
                                   cpb.SetComponent(entityInQueryIndex, linkedEntity, fighter);
                               }

                           }
                           //  Debug.Log($"{e.Index} linked to {linkedEntity.Index} change linked entity target");
                       }
                       linksFound.Dispose();
                       hits.Dispose();
                   })
                   .ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }

    }

}