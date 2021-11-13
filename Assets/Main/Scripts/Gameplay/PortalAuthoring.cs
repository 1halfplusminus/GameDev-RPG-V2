
using UnityEngine;
using Unity.Entities;
using RPG.Core;
using RPG.Control;
using Unity.Scenes;


namespace RPG.Gameplay
{
    using Unity.Collections;
#if UNITY_EDITOR
    using UnityEditor;
    public class PortalAuthoring : MonoBehaviour
    {

        public SceneAsset Scene;

        public int OtherScenePortalIndex;


        public int PortalIndex;
    }

    public class PortalConversionSystem : GameObjectConversionSystem
    {
        SceneSystem sceneSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            sceneSystem = DstEntityManager.World.GetOrCreateSystem<SceneSystem>();

        }
        protected override void OnUpdate()
        {
            Entities.ForEach((PortalAuthoring portalAuthoring) =>
            {
                var SceneGUID = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(portalAuthoring.Scene)));
                var entity = GetPrimaryEntity(portalAuthoring);
                DstEntityManager.AddComponentData(entity, new Portal { Index = portalAuthoring.PortalIndex, SceneGUID = SceneGUID });
                /* var sceneEntity = sceneSystem.LoadSceneAsync(SceneGUID, new SceneSystem.LoadParameters { Flags = SceneLoadFlags.DisableAutoLoad });
                DstEntityManager.AddSharedComponentData(sceneEntity, new SceneSection() { SceneGUID = SceneGUID }); */
            });
        }
    }
#endif
    public struct Portal : IComponentData
    {
        public int Index;

        public Unity.Entities.Hash128 SceneGUID;
    }
    public struct CollidWithPlayer : IComponentData
    {
        public Entity Entity;
    }
    public class CollidWithPlayerSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var players = GetComponentDataFromEntity<PlayerControlled>(true);
            Entities.ForEach((int entityInQueryIndex, Entity e, DynamicBuffer<StatefulTriggerEvent> triggerEvents) =>
            {
                foreach (var triggerEvent in triggerEvents)
                {
                    var otherEntity = triggerEvent.GetOtherEntity(e);
                    if (players.HasComponent(otherEntity))
                    {
                        Debug.Log("Collid with player");
                        commandBufferP.AddComponent(entityInQueryIndex, e, new CollidWithPlayer { Entity = otherEntity });
                        break;
                    }
                }
            }).WithReadOnly(players).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    public struct LoadingScene : IComponentData
    {
        public Entity Player;
        public Entity Scene;
    }
    public struct UnloadScene : IComponentData
    {
        public Entity Entity;
    }


    public class PortalSystem : SystemBase
    {
        SceneSystem sceneSystem;


        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();

            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var _sceneSystem = sceneSystem;
            var em = EntityManager;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
            .ForEach((Entity e, in CollidWithPlayer collidWithPlayer, in Portal portal, in SceneTag currentScene) =>
            {

                Debug.Log("Change scene");
                var sceneEntity = _sceneSystem.LoadSceneAsync(portal.SceneGUID);
                _sceneSystem.UnloadScene(currentScene.SceneEntity);
                /*     var sceneQuery = em.CreateEntityQuery(new EntityQueryDesc()
                    {
                        All = new[] { ComponentType.ReadOnly(typeof(SceneTag)) },
                        None = new[] { ComponentType.ReadWrite(typeof(SubScene)), ComponentType.ReadWrite(typeof(SceneReference)) }
                    });
                    sceneQuery.SetSharedComponentFilter(new SceneTag { SceneEntity = currentScene.SceneEntity });
                    commandBuffer.DestroyEntitiesForEntityQuery(sceneQuery); */

                /*    commandBuffer.DestroyEntitiesForEntityQuery(sceneQuery); */
                /*                commandBuffer.AddComponent(e, new LoadingScene() { }); */
                /*          commandBuffer.AddComponent(sceneEntity, new SceneReference() { SceneGUID = currentScene.SceneGUID }); */


            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            /*      Entities
                 .WithStructuralChanges()
                     .WithoutBurst()
                     .ForEach((Entity e, in LoadingScene loadingScene, in Portal portal) =>
                     {
                         Debug.Log("Here");
                         var sceneEntity = _sceneSystem.LoadSceneAsync(portal.SceneGUID, new SceneSystem.LoadParameters() { AutoLoad = true });

                     }).Run(); */

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
