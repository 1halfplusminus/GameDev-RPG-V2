using RPG.Gameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public struct FadeIn : IComponentData
    {
        public float Opacity;
        public float Speed;
    }

    public struct FadeOut : IComponentData
    {

        public float Opacity;
        public float Speed;
        public float Min;
        public bool IsFinish;
    }
    public class LoadingUISystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
            .WithNone<FadeIn, FadeOut>()
            .WithAll<LoadingUI>()
            .WithChangeFilter<AnySceneLoading>()
            .ForEach((Entity e, UIDocument uiDocument, in AnySceneLoading anySceneLoading) =>
            {
                var visualElement = uiDocument.rootVisualElement.Q<VisualElement>();
                if (anySceneLoading.Value == true)
                {

                    visualElement.style.opacity = 1f;
                }
                else
                {
                    commandBuffer.AddComponent(e, new FadeOut()
                    {
                        Min = 90f,
                        Opacity = visualElement.style.opacity.value,
                        Speed = 0.001f
                    });
                }
            })
            .WithoutBurst()
            .Run();

            Entities
           .WithNone<FadeIn>()
           .WithAll<FadeOut>()
           .ForEach((Entity e, UIDocument uiDocument, FadeOut fadeOut) =>
           {
               var visualElement = uiDocument.rootVisualElement.Q<VisualElement>();
               visualElement.style.opacity = !fadeOut.IsFinish ? fadeOut.Opacity : 0.0f;
               Debug.Log("Fading out");
           })
           .WithoutBurst()
           .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    public class UIFadeInFadeOutSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithNone<FadeIn>()
            .ForEach((Entity e, int entityInQueryIndex, ref FadeOut fadeOut) =>
            {
                if (fadeOut.IsFinish)
                {
                    commandBuffer.RemoveComponent<FadeOut>(entityInQueryIndex, e);
                    return;
                }
                fadeOut.Opacity = math.clamp(fadeOut.Opacity - fadeOut.Speed, fadeOut.Min, 1f);
                if (fadeOut.Opacity <= fadeOut.Min)
                {
                    fadeOut.IsFinish = true;
                }

            })
            .ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}