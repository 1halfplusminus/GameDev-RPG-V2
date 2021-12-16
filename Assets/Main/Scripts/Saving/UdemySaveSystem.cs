using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RPG.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using System.Linq;

namespace RPG.Saving
{
    public struct LastSceneSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<LastScene>() }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            var lastScenesBuffer = em.GetBuffer<LastScene>(e);
            using var nativeArray = lastScenesBuffer.ToNativeArray(Allocator.Temp);
            var array = nativeArray.ToArray();
            return array;
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {

            if (state is LastScene[] lastScenes)
            {
                var lastScene = lastScenes.LastOrDefault();
                if (lastScenes.Length >= 1)
                {
                    Debug.Log($"Unserializing LastScene {lastScene.SceneGUID}");
                    em.AddComponentData(e, new TriggerSceneLoad { SceneGUID = lastScene.SceneGUID });
                }

            }

        }
    }

    public struct SceneSaveCheckpoint : IComponentData
    {

    }
    [Serializable]
    public struct LastScene : IBufferElementData
    {
        public Hash128 SceneGUID;
    }
    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class UdemySaveSystem : SaveSystemBase
    {
        List<ISerializer> serializers;
        EntityQuery queryIdentifier;

        EntityQuery lastSceneBuildQuery;

        EntityQuery sceneLoadedQuery;

        const string LastSceneEntityIdentifier = "LastSceneBuildIndex";


        protected EntityQueryDesc LastSceneBuildEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly<LastScene>(), ComponentType.ReadOnly<Identifier>() }
            };
        }
        protected void CreateLastBuildEntity()
        {
            var entity = EntityManager.CreateEntity(LastSceneBuildEntityQueryDesc().All);
            EntityManager.AddBuffer<LastScene>(entity);
            var hash = new UnityEngine.Hash128();
            hash.Append(LastSceneEntityIdentifier);
            EntityManager.AddComponentData(entity, new Identifier() { Id = hash });
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            RegisterAllISerializers();
            queryIdentifier = GetEntityQuery(typeof(Identifier));
            lastSceneBuildQuery = GetEntityQuery(LastSceneBuildEntityQueryDesc());
            CreateLastBuildEntity();
        }
        protected override void OnUpdate()
        {
            var lastSceneEntity = lastSceneBuildQuery.GetSingletonEntity();
            var lastSceneBuildBuffer = EntityManager.GetBuffer<LastScene>(lastSceneEntity);
            var sceneLoadedCount = sceneLoadedQuery.CalculateEntityCount();
            if (sceneLoadedCount > 0)
            {
                lastSceneBuildBuffer.Clear();
                lastSceneBuildBuffer.Capacity = sceneLoadedCount;
            }

            Entities
            .WithStoreEntityQueryInField(ref sceneLoadedQuery)
            .WithChangeFilter<SceneLoaded>()
            .ForEach((in SceneLoaded sceneLoaded) =>
            {
                lastSceneBuildBuffer.Add(new LastScene { SceneGUID = sceneLoaded.SceneGUID });
            }).Schedule();

            Entities.ForEach((in TriggeredSceneLoaded sceneLoaded) =>
            {
                //FIXME: Shouldn't know the default file path
                Load(SaveSystem.GetPathFromSaveFile("test.save"));

            }).WithStructuralChanges().WithoutBurst().Run();
            Entities.ForEach((Entity e, in SceneSaveCheckpoint sceneLoaded) =>
           {
               //FIXME: Shouldn't know the default file path
               Save(SaveSystem.GetPathFromSaveFile("test.save"));
               EntityManager.RemoveComponent<SceneSaveCheckpoint>(e);
           }).WithStructuralChanges().WithoutBurst().Run();
            Entities.ForEach((in TriggerUnloadScene sceneUnLoad) =>
            {
                //FIXME: Shouldn't know the default file path
                Save(SaveSystem.GetPathFromSaveFile("test.save"));
            }).WithStructuralChanges().WithoutBurst().Run();

        }
        public override bool LoadLastScene(string savePath)
        {
            var states = LoadFile(savePath);
            var lastSceneEntity = lastSceneBuildQuery.GetSingletonEntity();
            var lastSceneEntityId = lastSceneBuildQuery.GetSingleton<Identifier>();
            var id = lastSceneEntityId.Id.ToString();
            if (states.ContainsKey(id))
            {
                var lastSceneState = (Dictionary<string, object>)states[id];
                RestoreSerializersState(lastSceneState, lastSceneEntity);
                return true;
            }
            return false;
        }
        public override void Load(string savePath)
        {
            Debug.Log($"Loading from file {savePath}");
            var states = LoadFile(savePath);
            var lastSceneEntityId = lastSceneBuildQuery.GetSingleton<Identifier>();
            states.Remove(lastSceneEntityId.Id.ToString());
            RestoreState(states);
        }

        private Dictionary<string, object> LoadFile(string fileName)
        {

            if (File.Exists(fileName))
            {
                using var stream = File.Open(fileName, FileMode.Open);
                var formatter = new BinaryFormatter();

                try
                {
                    return formatter.Deserialize(stream) as Dictionary<string, object>;
                }
                catch (Exception) { };

            }
            var state = new Dictionary<string, object>();
            return state;
        }

        private void CaptureState(Dictionary<string, object> rootState)
        {
            using var identifieds = queryIdentifier.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var identifiedEntities = queryIdentifier.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < identifiedEntities.Length; i++)
            {
                var entity = identifiedEntities[i];
                var identified = identifieds[i];
                var id = identified.Id.ToString();
                rootState[id] = CaptureSerializersState(entity);
            }

        }

        private Dictionary<string, object> CaptureSerializersState(Entity entity)
        {
            var serializersState = new Dictionary<string, object>();
            foreach (var serializer in serializers)
            {
                var serializerKey = serializer.GetType().ToString();
                var entityQuery = GetEntityQuery(serializer.GetEntityQueryDesc());
                if (entityQuery.Matches(entity))
                {
                    var r = serializer.Serialize(EntityManager, entity);
                    Debug.Log($"Capture Serializer State serializer key: ${serializerKey} value:  {r}");
                    serializersState[serializerKey] = r;
                }
            }
            return serializersState;
        }

        private void RestoreState(Dictionary<string, object> rootState)
        {
            using var identifieds = queryIdentifier.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var identifiedEntities = queryIdentifier.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < identifiedEntities.Length; i++)
            {
                var entity = identifiedEntities[i];
                var id = identifieds[i].Id.ToString();
                if (rootState.ContainsKey(id))
                {
                    RestoreSerializersState(rootState[id] as Dictionary<string, object>, entity);
                }

            }
        }

        private void RestoreSerializersState(Dictionary<string, object> entityState, Entity entity)
        {
            foreach (var serializer in serializers)
            {
                var serializerKey = serializer.GetType().ToString();

                if (entityState.ContainsKey(serializerKey))
                {
                    Debug.Log($"Restore Serializer State serializer key: ${serializerKey} value:  {entityState[serializerKey]}");
                    serializer.UnSerialize(EntityManager, entity, entityState[serializerKey]);
                }
            }
        }

        public override void Save(string savePath)
        {
            var state = LoadFile(savePath);
            CaptureState(state);
            SaveFile(savePath, state);
        }

        private void SaveFile(string savePath, object state)
        {
            Debug.Log($"Writing to file {savePath}");
            using var stream = File.Open(savePath, FileMode.Create);
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, state);
        }

        private void RegisterAllISerializers()
        {
            serializers = new List<ISerializer>();
            var type = typeof(ISerializer);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
            foreach (var serializerType in types)
            {
                var serializer = Activator.CreateInstance(serializerType);
                if (serializer is ISerializer casted)
                {

                    serializers.Add(casted);
                }

            }
        }

    }
}

