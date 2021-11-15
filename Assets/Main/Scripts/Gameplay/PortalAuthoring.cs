
using UnityEngine;
using Unity.Entities;
using RPG.Core;
using RPG.Control;
using Unity.Scenes;
using RPG.Mouvement;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace RPG.Gameplay
{

#if UNITY_EDITOR
    using UnityEditor;
    public class PortalAuthoring : MonoBehaviour
    {

        public SceneAsset Scene;

        public int OtherScenePortalIndex;

        public int PortalIndex;

        [SerializeField]
        public Transform Warppoint;
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
                DstEntityManager.AddComponentData(entity, new Portal { Index = portalAuthoring.PortalIndex, WarpPoint = new LocalToWorld() { Value = portalAuthoring.Warppoint.transform.localToWorldMatrix } });
                DstEntityManager.AddComponentData(entity, new LinkPortal { Index = portalAuthoring.OtherScenePortalIndex, SceneGUID = SceneGUID });
                /*                DstEntityManager.AddComponentData(entity, new LocalToWorld() { Value = portalAuthoring.Warppoint.localToWorldMatrix }); */
                /* var sceneEntity = sceneSystem.LoadSceneAsync(SceneGUID, new SceneSystem.LoadParameters { Flags = SceneLoadFlags.DisableAutoLoad });
                DstEntityManager.AddSharedComponentData(sceneEntity, new SceneSection() { SceneGUID = SceneGUID }); */
            });
        }
    }
#endif
    public struct Portal : IComponentData
    {
        public int Index;

        public LocalToWorld WarpPoint;
    }
    public struct LinkPortal : IComponentData
    {
        public int Index;
        public Unity.Entities.Hash128 SceneGUID;

    }
    public struct CollidWithPlayer : IComponentData
    {
        public Entity Entity;
    }
    public struct WarpToPortal : IComponentData
    {
        public int PortalIndex;
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
    public struct SceneLoading : IComponentData
    {
        public Entity Player;
        public Entity Scene;
    }
    public struct UnloadScene : IComponentData
    {
        public Entity Entity;
    }

    [UpdateAfter(typeof(CoreSystemGroup))]
    public class PortalSystem : SystemBase
    {
        SceneSystem sceneSystem;


        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery portalQuery;

        EntityQuery needWarpQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            sceneSystem = World.GetOrCreateSystem<SceneSystem>();
            portalQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(LocalToWorld)), ComponentType.ReadOnly(typeof(Portal)));
            needWarpQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(WarpToPortal)));
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var _sceneSystem = sceneSystem;
            var em = EntityManager;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var query = portalQuery;
            Entities
            .ForEach((Entity e, in CollidWithPlayer collidWithPlayer, in Portal portal, in SceneSection currentScene, in LinkPortal linkPortal) =>
            {

                Debug.Log("Unload Scene" + currentScene.SceneGUID);
                var sceneEntity = _sceneSystem.LoadSceneAsync(linkPortal.SceneGUID);
                _sceneSystem.UnloadScene(_sceneSystem.GetSceneEntity(currentScene.SceneGUID));
                var newSceneRef = em.GetComponentData<SceneReference>(sceneEntity);
                Debug.Log("Loaded Scene " + newSceneRef.SceneGUID);
                commandBuffer.AddComponent(collidWithPlayer.Entity, new WarpToPortal { PortalIndex = linkPortal.Index });

            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            if (needWarpQuery.CalculateEntityCount() == 0)
            {
                Entities
                .ForEach((ref AnySceneLoading loading) =>
                {
                    loading.Value = false;
                }).ScheduleParallel();
                return;

            }
            Entities
            .ForEach((ref AnySceneLoading loading) =>
            {
                loading.Value = true;
            }).ScheduleParallel();
            var indexedPortals = new NativeHashMap<int, (LocalToWorld, Portal)>(portalQuery.CalculateEntityCount(), Allocator.TempJob);
            var indexedPortalWriter = indexedPortals.AsParallelWriter();
            Entities
            .ForEach((Entity e, int entityInQueryIndex, in Portal portal, in LocalToWorld localToWorld) =>
            {
                indexedPortalWriter.TryAdd(portal.Index, (localToWorld, portal));
            }).ScheduleParallel();

            Entities
            .WithReadOnly(indexedPortals)
            .WithDisposeOnCompletion(indexedPortals)
            .ForEach((int entityInQueryIndex, Entity e, in WarpToPortal warp) =>
            {
                if (indexedPortals.ContainsKey(warp.PortalIndex))
                {
                    Debug.Log("Portail Found Warping Player");
                    var destination = indexedPortals[warp.PortalIndex].Item2.WarpPoint;
                    commandBufferP.AddComponent(entityInQueryIndex, e, new Translation() { Value = destination.Position });
                    commandBufferP.AddComponent(entityInQueryIndex, e, new WarpTo() { Destination = destination.Position });
                    commandBufferP.AddComponent(entityInQueryIndex, e, new Rotation() { Value = quaternion.LookRotation(destination.Forward, destination.Up) });
                    commandBufferP.RemoveComponent<WarpToPortal>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
