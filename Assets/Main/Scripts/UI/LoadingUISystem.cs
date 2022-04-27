using RPG.Control;
using RPG.Core;
using RPG.Gameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public struct EnableControl : IComponentData
    {
        public FixedList128Bytes<Entity> Entities;
    }

    public struct IsLoading : IComponentData { }
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
            // var step = math.smoothstep(Current, Duration, _timeElapsed);
            Current = math.lerp(From, To, _timeElapsed / Duration);

        }

    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class LoadingUISystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery anySceneLoadingQuery;
        EntityQuery loadingUIPrefabQuery;

        EntityQuery loadingUIQuery;

        EntityQuery fadedLoadingUIQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            anySceneLoadingQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new ComponentType[] {
                    typeof(TriggerSceneLoad), typeof(LoadSceneAsync)
                },
                None = new ComponentType[]{
                    typeof(TriggeredSceneLoaded)
                }
            });
            loadingUIPrefabQuery = GetEntityQuery(ComponentType.ReadWrite<LoadingUI>(), ComponentType.ReadWrite<Prefab>());
            loadingUIQuery = GetEntityQuery(ComponentType.ReadWrite<LoadingUI>(), ComponentType.ReadWrite<UIDocument>());
            fadedLoadingUIQuery = GetEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[]{
                   typeof(Fade),typeof(IsLoading)
                },
                All = new ComponentType[]{
                   typeof(LoadingUI)
                }
            });
        }

        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var sceneLoadingCount = anySceneLoadingQuery.CalculateEntityCount();
            if (sceneLoadingCount > 0 && loadingUIPrefabQuery.CalculateEntityCount() == 1 && loadingUIQuery.CalculateEntityCount() == 0)
            {
                Debug.Log("Instanciate LoadingUI");
                var instance = EntityManager.Instantiate(loadingUIPrefabQuery.GetSingletonEntity());
                EntityManager.RemoveComponent<SceneTag>(instance);
                EntityManager.AddComponent<AnySceneLoading>(instance);
                EntityManager.AddComponentData(instance, new DeltaTime { });
                EntityManager.AddComponentData(instance, new Fade()
                {
                    To = 100f,
                    From = 90f,
                    Duration = 0.1f
                });
                EntityManager.AddComponent<IsLoading>(instance);
                EntityManager.AddComponent<AnySceneLoading>(instance);
            }
            Entities
            .WithAny<IsLoading>()
            .WithAll<LoadingUI>()
            .ForEach((Entity e) =>
            {
                if (sceneLoadingCount == 0)
                {
                    Debug.Log("Hide UI when Loading finish");
                    commandBuffer.RemoveComponent<IsLoading>(e);
                    commandBuffer.AddComponent(e, new DeltaTime { });
                    commandBuffer.AddComponent(e, new Fade()
                    {
                        To = 0f,
                        From = 100f,
                        Duration = 1f
                    });
                }

            })
            .Schedule();

            Entities
            .WithNone<Fade, IsLoading, Hide>()
            .WithAll<LoadingUI, UIReady>()
            .ForEach((Entity e) =>
            {
                Debug.Log("Destroy Loading UI LoadingUI");
                commandBuffer.DestroyEntity(e);
            })
            .Run();
        }
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class FadeSystem : SystemBase
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
    public partial class ShowHideUISystem : SystemBase
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
                visualElement.visible = true;
                visualElement.SetEnabled(true);
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