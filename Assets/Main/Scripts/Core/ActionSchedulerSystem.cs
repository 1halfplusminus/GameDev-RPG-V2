using Unity.Entities;

namespace RPG.Core
{

    /*   public interface IAction : IComponentData
      {
          public bool Canceled { get; set; }
      }

      public interface IStartAction<T> where T : struct, IComponentData
      {
          public T Action { get; set; }
      }

      public struct StartAction : IComponentData
      {
          public IComponentData Action;
      }

      public struct CurrentAction : IComponentData
      {
          public IComponentData Action;
      }
      [UpdateInGroup(typeof(CoreSystemGroup))]
      public class ActionSchedulerSystem : SystemBase
      {
          EntityCommandBufferSystem entityCommandBufferSystem;
          protected override void OnCreate()
          {
              base.OnCreate();
              entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
          }
          protected override void OnUpdate()
          {
              var em = EntityManager;
              var commandBufferParallel = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
              Entities.ForEach((int entityInQueryIndex, Entity e, StartAction action) =>
              {
                  if (em.HasComponent(e, action.Action.GetType()))
                  {

                  }

              }).ScheduleParallel();


              entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
          }
      } */
}
