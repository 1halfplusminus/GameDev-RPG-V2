using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using RPG.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Saving
{
    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class UdemySaveSystem : SaveSystemBase
    {
        Dictionary<EntityQueryMask, ISerializer> serializers;
        EntityQuery queryIdentifier;
        protected override void OnCreate()
        {
            base.OnCreate();
            RegisterAllISerializers();
            queryIdentifier = GetEntityQuery(typeof(Identifier));
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((TriggeredSceneLoaded sceneLoaded) =>
            {
                Load(SaveSystem.GetPathFromSaveFile("test.save"));
            }).WithStructuralChanges().WithoutBurst().Run();
            Entities.ForEach((TriggerUnloadScene sceneUnLoad) =>
            {
                Save(SaveSystem.GetPathFromSaveFile("test.save"));
            }).WithStructuralChanges().WithoutBurst().Run();
        }

        public override void Load(string savePath)
        {
            Debug.Log($"Loading from file {savePath}");

            RestoreState(LoadFile(savePath));
        }

        private Dictionary<string, object> LoadFile(string fileName)
        {

            if (File.Exists(fileName))
            {
                using var stream = File.Open(fileName, FileMode.Open);
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream) as Dictionary<string, object>;
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
                if (serializer.Key.Matches(entity))
                {
                    var r = serializer.Value.Serialize(EntityManager, entity);
                    serializersState[serializer.GetType().ToString()] = r;
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
                    serializer.Value.UnSerialize(EntityManager, entity, entityState[serializerKey]);
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
            serializers = new Dictionary<EntityQueryMask, ISerializer>();
            var type = typeof(ISerializer);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
            foreach (var serializerType in types)
            {
                var serializer = Activator.CreateInstance(serializerType);
                if (serializer is ISerializer casted)
                {
                    var entityQuery = GetEntityQuery(casted.GetEntityQueryDesc());
                    serializers[entityQuery.GetEntityQueryMask()] = casted;
                }

            }
        }
    }
}

