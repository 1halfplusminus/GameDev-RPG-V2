using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;

namespace RPG.Control
{
    [UpdateAfter(typeof(CombatSystemGroup))]
    public class PlayersMoveSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        EntityQuery worldClickQueries;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            worldClickQueries = GetEntityQuery(new ComponentType[] {
                ComponentType.ReadOnly<WorldClick>()
            });
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, int entityInQueryIndex, ref MoveTo moveTo, in MouseClick mouseClick, in WorldClick worldClick) =>
            {
                if (mouseClick.CapturedThisFrame)
                {
                    moveTo.Stopped = false;
                    moveTo.Position = worldClick.WorldPosition;
                }

            }).ScheduleParallel();

            Entities
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, ref Fighter fighter, in MouseClick mouseClick) =>
            {

                if (mouseClick.CapturedThisFrame)
                {
                    if (fighter.Target == Entity.Null)
                    {

                        fighter.MoveTowardTarget = false;
                    }
                    else
                    {
                        fighter.MoveTowardTarget = true;
                    }
                }

            }).ScheduleParallel();
            // Look at fighter target if exists
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((ref LookAt lookAt, in Fighter fighter) =>
            {

                lookAt.Entity = fighter.Target;

            }).ScheduleParallel();
        }
    }
}

