using Unity.Entities;
using RPG.Core;
using RPG.Mouvement;
using RPG.Combat;

namespace RPG.Control
{
    [UpdateAfter(typeof(CombatSystemGroup))]
    public class PlayersMoveSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        EntityQuery worldClickQueries;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            worldClickQueries = GetEntityQuery(new ComponentType[] {
                ComponentType.ReadOnly<WorldClick>()
            });
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((Entity player, int entityInQueryIndex, ref Fighter fighter, in MouseClick mouseClick, in WorldClick worldClick) =>
            {
                if (mouseClick.CapturedThisFrame)
                {
                    if (fighter.Target == Entity.Null)
                    {
                        fighter.MoveTowardTarget = false;
                        commandBuffer.AddComponent(entityInQueryIndex, player, new MoveTo(worldClick.WorldPosition));
                    }
                    else
                    {
                        fighter.MoveTowardTarget = true;
                    }
                }


            }).ScheduleParallel();


            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}

