using Unity.Entities;
using UnityEngine.UIElements;
using static Unity.Entities.ComponentType;
using RPG.Gameplay;
using RPG.Control;
using UnityEngine;
using Unity.Transforms;
using System;
using Unity.Physics.Systems;
using Unity.Jobs;
using Unity.Physics;
using Unity.Collections;

namespace RPG.UI
{
    public struct RenderDialog : IComponentData
    {
        public Entity DialogEntity;
        public BlobAssetReference<BlobDialog> DialogAsset;
    }
    public struct DialogInstance : IComponentData
    {
        public Entity Instance;
    }
    public struct DialogInteractionUI : IComponentData
    {
        public Entity Prefab;

        public BlobAssetReference<BlobDialog> DialogAsset;
    }

    public struct DialogInteractionUIInstance : IComponentData
    {
        public Entity Instance;
        public Entity Player;
    }
    public struct InDialog : IComponentData
    {
    }
    [UpdateInGroup(typeof(UISystemGroup))]
    public class DialogInteractionUISystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        BuildPhysicsWorld buildPhysicsWorld;

        StepPhysicsWorld stepPhysicsWorld;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        }
        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(buildPhysicsWorld.GetOutputDependency(), stepPhysicsWorld.GetOutputDependency(), Dependency);
            var physicWorld = buildPhysicsWorld.PhysicsWorld;
            var collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithNone<DialogInteractionUIInstance>()
            .ForEach((int entityInQueryIndex, Entity e, in LocalToWorld localToWorld, in CollidWithPlayer collidWithPlayer, in DialogInteractionUI dialogInteractionUI, in DialogAsset dialogAsset) =>
            {
                if (collidWithPlayer.State != Core.EventOverlapState.Exit)
                {
                    var instance = cbp.Instantiate(entityInQueryIndex, dialogInteractionUI.Prefab);
                    cbp.AddComponent(entityInQueryIndex, e, new DialogInteractionUIInstance { Instance = instance, Player = collidWithPlayer.Entity });
                    cbp.AddComponent(entityInQueryIndex, instance, new GameplayInput());
                    cbp.AddComponent(entityInQueryIndex, instance, dialogAsset);
                    cbp.AddComponent(entityInQueryIndex, instance, new Translation { Value = localToWorld.Position });
                }
            }).ScheduleParallel();

            Entities
            .WithReadOnly(collisionWorld)
            .ForEach((int entityInQueryIndex, Entity e, in DialogInteractionUIInstance dialogInteractionUIInstance, in LocalToWorld localToWorld) =>
            {
                var distanceCast = new PointDistanceInput { Filter = CollisionFilter.Default, Position = localToWorld.Position, MaxDistance = 3 };
                var hits = new NativeList<DistanceHit>(Allocator.Temp);
                collisionWorld.CalculateDistance(distanceCast, ref hits);
                var playerFound = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit.Entity == dialogInteractionUIInstance.Player && !HasComponent<DisabledControl>(dialogInteractionUIInstance.Player))
                    {
                        playerFound = true;
                    }
                }
                if (!playerFound)
                {
                    cbp.RemoveComponent<DialogInteractionUIInstance>(entityInQueryIndex, e);
                    cbp.DestroyEntity(entityInQueryIndex, dialogInteractionUIInstance.Instance);
                }
            }).ScheduleParallel();
            Entities
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer collidWithPlayer, in DialogInteractionUIInstance dialogInteractionUIInstance) =>
            {
                if (collidWithPlayer.State == Core.EventOverlapState.Exit)
                {
                    cbp.RemoveComponent<DialogInteractionUIInstance>(entityInQueryIndex, e);
                    cbp.DestroyEntity(entityInQueryIndex, dialogInteractionUIInstance.Instance);
                }
            }).ScheduleParallel();
            Entities
            .WithNone<InteractWithUIDialogController>()
            .WithAll<UIReady, DialogInteraction>().ForEach((Entity e, UIDocument document, in LocalToWorld localToWorld) =>
            {
                Debug.Log("Press e to start dialog");
                var controller = new InteractWithUIDialogController();
                controller.Init(document.rootVisualElement);
                cb.AddComponent(e, controller);
                controller.SetPosition(Camera.main, localToWorld.Position);
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<DialogInteraction>()
            .WithNone<InDialog>()
            .ForEach((Entity e, InteractWithUIDialogController interactWithUIDialogController, ref GameplayInput gameplayInput) =>
            {
                if (interactWithUIDialogController.ClickedThisFrame)
                {
                    gameplayInput.DialogInteractionPressedThisFrame = true;
                    interactWithUIDialogController.ClickedThisFrame = false;
                }
            })
            .WithoutBurst()
            .Run();

            Entities
            .WithAll<DialogInteraction>()
            .WithNone<InDialog>()
            .ForEach((int entityInQueryIndex, Entity e, in GameplayInput gameplayInput, in DialogAsset dialogAsset) =>
            {
                if (gameplayInput.DialogInteractionPressedThisFrame)
                {
                    Debug.Log("Instanciate Dialog");
                    cbp.AddComponent<Hide>(entityInQueryIndex, e);
                    cbp.Instantiate(entityInQueryIndex, dialogAsset.Value);
                    cbp.AddComponent<InDialog>(entityInQueryIndex, e);
                }
            })
            .ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    [UpdateInGroup(typeof(UISystemGroup))]
    public class DialogUISystem : SystemBase
    {
        EntityQuery dialogUIPrefabQuery;
        EntityQuery dialogUIQuery;

        EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            dialogUIPrefabQuery = GetEntityQuery(ReadOnly<Prefab>(), ReadOnly<DialogUI>());
            dialogUIQuery = GetEntityQuery(ReadOnly<RenderDialog>());
            endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        private Action OnClose(Entity entity)
        {
            return () =>
            {
                EntityManager.DestroyEntity(entity);
            };
        }
        protected override void OnUpdate()
        {
            var cb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            var dialogUIPrefab = dialogUIPrefabQuery.GetSingletonEntity();
            Entities
            .WithNone<DialogInstance>()
            .ForEach((int entityInQueryIndex, Entity e, in Dialog dialog) =>
            {
                var instance = cbp.Instantiate(entityInQueryIndex, dialogUIPrefab);
                cbp.AddComponent(entityInQueryIndex, instance, new RenderDialog { DialogAsset = dialog.Reference, DialogEntity = e });
                cbp.AddComponent(entityInQueryIndex, e, new DialogInstance { Instance = instance });
            }).ScheduleParallel();

            Entities
            .WithAll<DialogUI, UIReady>()
            .WithNone<DialogController>()
            .ForEach((UIDocument document, Entity e, in RenderDialog displayDialog) =>
            {
                var controller = new DialogController();
                controller.onClose += OnClose(e);
                controller.Init(document.rootVisualElement);
                controller.ShowNode(displayDialog.DialogAsset, displayDialog.DialogAsset.Value.StartIndex);
                cb.AddComponent(e, controller);
                cb.AddComponent(displayDialog.DialogEntity, controller);
            })
            .WithoutBurst()
            .Run();

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}