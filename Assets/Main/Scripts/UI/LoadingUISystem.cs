using RPG.Control;
using RPG.Core;
using RPG.Gameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public struct EnableControl : IComponentData
    {
        public FixedList128<Entity> Entities;
    }
    public struct UIReady : IComponentData { }
    public struct Fade : IComponentData
    {
        public enum FadeDirection
        {
            In, Out
        }
        public float From;

        public float To;
        public bool IsFinish;
        public float Duration;

        public float _timeElapsed;

        public float Current;

        public FadeDirection Direction { get { return From >= To ? FadeDirection.Out : FadeDirection.In; } }
        public void Update(float deltaTime)
        {

            if (IsFinish)
            {
                return;
            }
            _timeElapsed += deltaTime;
            if (_timeElapsed >= Duration)
            {
                _timeElapsed = 0f;
                IsFinish = true;
                Current = To;
                return;
            }
            var step = math.smoothstep(0.0f, Duration, _timeElapsed);
            Current = math.lerp(From, To, step);
            Debug.Log($"current opacity: {Current} ,  step: {step}");
        }

    }
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
           })
           .WithoutBurst().Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
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
            var commandBufferP = commandBuffer.AsParallelWriter();

            Entities
            .WithAll<LoadingUI>()
            .ForEach((Entity e, UIDocument uiDocument, in AnySceneLoading anySceneLoading) =>
            {
                commandBuffer.AddComponent(e, new Show() { });
                commandBuffer.AddComponent(e, new DeltaTime { });
                commandBuffer.AddComponent(e, new Fade()
                {
                    To = 1f,
                    From = 0.0f,
                    Duration = 1f
                });
                foreach (var trigger in anySceneLoading.Triggers)
                {
                    commandBuffer.AddComponent<DisabledControl>(trigger);
                }
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<LoadingUI>()
            .ForEach((Entity e, UIDocument uiDocument, in AnySceneFinishLoading anySceneLoading) =>
            {
                Debug.Log("Hide UI when Loading finish");
                commandBuffer.AddComponent(e, new DeltaTime { });
                commandBuffer.AddComponent(e, new Fade()
                {
                    To = 0f,
                    From = 1f,
                    Duration = 1.5f
                });
                commandBuffer.AddComponent(e, new EnableControl()
                {
                    Entities = anySceneLoading.Triggers
                });
                // foreach (var trigger in anySceneLoading.Triggers)
                // {
                //     commandBuffer.RemoveComponent<DisabledControl>(trigger);
                // }
            })
            .WithoutBurst()
            .Run();

            // TODO: move in a system in controls
            Entities
            .WithNone<Fade>()
            .ForEach((int entityInQueryIndex, Entity e, in EnableControl enableControl) =>
            {
                foreach (var entity in enableControl.Entities)
                {
                    commandBufferP.RemoveComponent<DisabledControl>(entityInQueryIndex, entity);
                }
                commandBufferP.RemoveComponent<EnableControl>(entityInQueryIndex, e);
            })
            .ScheduleParallel();


            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class FadeSystem : SystemBase
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
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities
            .ForEach((Entity e, int entityInQueryIndex, ref Fade fade, in DeltaTime deltaTime) =>
            {

                if (fade.IsFinish)
                {
                    commandBufferP.RemoveComponent<Fade>(entityInQueryIndex, e);
                    return;
                }
                Debug.Log($"Updating Fading  {fade.Duration} DeltaTime: {deltaTime.Value}");
                fade.Update(deltaTime.Value);
            })
            .ScheduleParallel();

            Entities
           .WithAll<Fade>()
           .ForEach((int entityInQueryIndex, Entity e, UIDocument uiDocument, in Fade fade) =>
           {
               var visualElement = uiDocument.rootVisualElement.Q<VisualElement>();
               if (fade.Direction == Fade.FadeDirection.Out && fade.IsFinish)
               {
                   commandBuffer.AddComponent(e, new Hide() { });
               }
               else
               {
                   visualElement.style.opacity = fade.Current;
               }

           })
           .WithoutBurst()
           .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class ShowHideUISystem : SystemBase
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
            .WithAll<Show>()
            .ForEach((Entity e, UIDocument uiDocument) =>
            {

                var visualElement = uiDocument.rootVisualElement.Q<VisualElement>();
                visualElement.style.visibility = Visibility.Visible;
                visualElement.style.display = DisplayStyle.Flex;
                commandBuffer.RemoveComponent<Show>(e);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAny<UIReady>()
            .WithAll<Hide>()
            .ForEach((Entity e, UIDocument uiDocument) =>
            {
                var visualElement = uiDocument.rootVisualElement.Q<VisualElement>();
                visualElement.style.visibility = Visibility.Hidden;
                visualElement.style.display = DisplayStyle.None;
                commandBuffer.RemoveComponent<Hide>(e);
            })
            .WithoutBurst()
            .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}