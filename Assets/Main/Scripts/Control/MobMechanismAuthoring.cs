
using RPG.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


namespace RPG.Control
{
    public struct LinkedMob : IBufferElementData
    {
        public Entity Entity;
    }

    public class MobMechanismAuthoring : MonoBehaviour
    {
        public FighterAuthoring[] linked;

    }

    [UpdateAfter(typeof(FighterConversionSystem))]
    public class MobMechanismConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((MobMechanismAuthoring mobMechanismAuthoring) =>
            {
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

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cpb = cb.AsParallelWriter();
            Entities
            .WithAll<AIControlled>()
            .WithAny<StartChaseTarget, WasHitted>()
            .ForEach((int entityInQueryIndex, Entity e, in DynamicBuffer<LinkedMob> linkedMob) =>
            {
                var target = Entity.Null;
                if (HasComponent<StartChaseTarget>(e))
                {
                    var startChaseTarget = GetComponent<StartChaseTarget>(e);
                    target = startChaseTarget.Target;
                }
                if (target == Entity.Null && HasComponent<WasHitted>(e))
                {
                    var wasHitted = cpb.SetBuffer<WasHitteds>(entityInQueryIndex, e);
                    if (wasHitted.Length > 0)
                    {
                        target = wasHitted[^1].Hitter;
                    }

                }
                for (int i = 0; i < linkedMob.Length; i++)
                {
                    var linked = linkedMob[i];
                    if (linked.Entity != e && HasComponent<Fighter>(linked.Entity))
                    {
                        var fighter = GetComponent<Fighter>(linked.Entity);
                        if (fighter.Target == Entity.Null)
                        {
                            fighter.Target = target;
                            fighter.MoveTowardTarget = true;
                            cpb.SetComponent(entityInQueryIndex, linked.Entity, fighter);
                        }
                        Debug.Log($"{e} linked to {linked} change linked entity target");
                    }
                    Debug.Log($"{e.Index} linked to {linked.Entity.Index} change linked entity target");
                }

            })
            .ScheduleParallel();


            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

        }

    }

}