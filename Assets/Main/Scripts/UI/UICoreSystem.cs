using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    [UpdateInGroup(typeof(UISystemGroup))]
    public class UICoreSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
           .WithNone<UIReady>()
           .WithAll<UIDocument>()
           .ForEach((Entity e, UIDocument uiDocument) =>
           {
               if (uiDocument.rootVisualElement != null)
               {

                   commandBuffer.AddComponent<UIReady>(e);
               }
               else
               {
                   Debug.LogError("No root visual element");
               }
           })
           .WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}