using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;
using System.IO;
using UnityEngine;
using Unity.Jobs;
using Hash128 = Unity.Entities.Hash128;
using Unity.Assertions;
using RPG.Core;

namespace RPG.Saving
{
    public struct Identifier : IComponentData
    {
        public Unity.Entities.Hash128 Id;
    }
    public struct Identified : IComponentData
    {

    }
    public interface ISavingConversionSystem
    {
        EntityManager DstEntityManager { get; }


    }
    public struct HasSpawnIdentified : IComponentData
    {

    }

    [UpdateInGroup(typeof(SavingSystemGroup))]
    [UpdateBefore(typeof(SaveSystem))]
    public class IdentifiableSystem : SystemBase
    {
        NativeHashMap<Unity.Entities.Hash128, Entity> identifiableEntities;

        EntityQuery identiableQuery;
        EntityQuery identifiedQuery;
        public NativeHashMap<Unity.Entities.Hash128, Entity> IdentifiableEntities
        {
            get
            {
                var ids = IndexQuery(identifiedQuery);
                return ids;
            }
        }

        EntityCommandBufferSystem entityCommandBufferSystem;

        private JobHandle outputDependency;

        public static NativeHashMap<Unity.Entities.Hash128, Entity> IndexQuery(EntityQuery query)
        {
            var ids = new NativeHashMap<Unity.Entities.Hash128, Entity>(query.CalculateEntityCount(), Allocator.TempJob);
            var datas = query.ToComponentDataArray<Identifier>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < datas.Length; i++)
            {
                if (ids.ContainsKey(datas[i].Id))
                {
                    Debug.Log($"{entities[i]} and {ids[datas[i].Id]} as the same identifier : {datas[i].Id}");
                    ids.Remove(datas[i].Id);
                }
                ids.TryAdd(datas[i].Id, entities[i]);
            }
            entities.Dispose();
            datas.Dispose();
            return ids;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            identifiableEntities = new NativeHashMap<Unity.Entities.Hash128, Entity>(0, Allocator.Persistent);
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            identifiedQuery = GetEntityQuery(typeof(Identifier));
            RequireForUpdate(identiableQuery);
        }


        public static bool TryGetEntity(in NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, out Entity entity)
        {
            entity = Entity.Null;
            if (entities.ContainsKey(hash))
            {
                entity = entities[hash];
                return true;
            }
            return false;
        }
        public static Entity GetOrCreateEntity(in NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, EntityCommandBuffer transaction)
        {

            var entityFound = TryGetEntity(entities, hash, out var entity);
            if (!entityFound)
            {
                entity = transaction.CreateEntity();
                transaction.AddComponent<Identifier>(entity, new Identifier { Id = hash });

            }

            return entity;
        }
        public static Entity GetOrCreateEntity(in NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, EntityCommandBuffer.ParallelWriter transaction, int entityInQueryIndex)
        {

            var entityFound = TryGetEntity(entities, hash, out var entity);
            if (!entityFound)
            {
                Debug.Log($"Entity not found creating {entity}");
                entity = transaction.CreateEntity(entityInQueryIndex);
                transaction.AddComponent<Identifier>(entityInQueryIndex, entity, new Identifier { Id = hash });
            }

            return entity;
        }
        protected override void OnUpdate()
        {
            /*          identifiableEntities.Clear(); */
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            identifiableEntities.Capacity += identiableQuery.CalculateEntityCount();
            var identifiedNowWritter = identifiableEntities.AsParallelWriter();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref identiableQuery)
            .WithNone<Identified>()
            .ForEach((int entityInQueryIndex, Entity e, in Identifier identifier) =>
            {
                commandBufferP.AddComponent<Identified>(entityInQueryIndex, e);
            })
            .ScheduleParallel();

            // Entities
            // .WithAll<Identified>()
            // .ForEach((int entityInQueryIndex, Entity e, in Identifier identifier) =>
            // {
            //     identifiedNowWritter.TryAdd(identifier.Id, e);
            // })
            // .ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

            outputDependency = Dependency;
        }

        public JobHandle GetOutputDependency()
        {
            return outputDependency;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            identifiableEntities.Dispose();
        }

    }



    [UpdateInGroup(typeof(SavingSystemGroup))]
    public class SaveSystem : SystemBase
    {
        const string SAVING_PATH = "save.bin";

        World conversionWorld;

        World serializedWorld;

        StreamBinaryWriter streamBinaryWriter;

        StreamBinaryReader streamBinaryReader;

        EntityQuery saveableQuery;

        public World ConversionWorld { get { return conversionWorld; } }
        public World SerializedWorld { get { return serializedWorld; } }
        protected override void OnCreate()
        {
            base.OnCreate();
            saveableQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadOnly(typeof(Identifier)), ComponentType.ReadOnly(typeof(SceneSection)) },
            });
        }


        protected override void OnUpdate()
        {

        }

        public World RecreateSerializeConversionWorld()
        {
            if (conversionWorld != null && conversionWorld.IsCreated)
            {
                conversionWorld.Dispose();
            }

            conversionWorld = CreateConversionWorld();
            return conversionWorld;
        }
        public static World CreateConversionWorld()
        {

            var conversionWorld = new World("Saving Conversion");
            conversionWorld.CreateSystem<UpdateWorldTimeSystem>();
            var initializationSystemGroup = conversionWorld.CreateSystem<InitializationSystemGroup>();
            var beginInitializationECS = conversionWorld.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            initializationSystemGroup.AddSystemToUpdateList(beginInitializationECS);
            return conversionWorld;
        }
        public void Load()
        {
            Load(SAVING_PATH);
        }
        public void Load(FixedString128 saveFile)
        {
            if (File.Exists(saveFile.ToString()))
            {
                UnityEngine.Debug.Log("File Exists Loading File");
                // Load world from file
                var conversionWorld = RecreateSerializeConversionWorld();
                // Load File
                using var binaryReader = CreateFileReader(saveFile.ToString());
                SerializeUtility.DeserializeWorld(conversionWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
                conversionWorld.EntityManager.EndExclusiveEntityTransaction();

                Load(conversionWorld);

                GetOrCreateSerializedWorld().EntityManager.CopyAndReplaceEntitiesFrom(conversionWorld.EntityManager);
            }
        }

        public void Load(World conversionWorld)
        {
            AddConversionSystems(conversionWorld, World.EntityManager);
            UpdateConversionSystems(conversionWorld);
        }

        public void Load(EntityQuery query)
        {
            var serializedWorld = GetOrCreateSerializedWorld();

            /*             using var currentWorldIdentified = IdentifiableSystem.IndexQuery(query); */
            var serializedWorldIdentified = serializedWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly(typeof(Identifier)));
            /* using var keys = currentWorldIdentified.GetKeyArray(Allocator.Temp);
            using var listToSerializeEntity = new NativeList<Entity>(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (serializedWorldIdentified.ContainsKey(key))
                {
                    listToSerializeEntity.Add(serializedWorldIdentified[key]);
                }
            }
            var srcEntities = listToSerializeEntity.ToArray(Allocator.Temp); */
            using var conversionWorld = RecreateSerializeConversionWorld();
            AddQueryToWorld(serializedWorld.EntityManager, conversionWorld, serializedWorldIdentified);
            /*        conversionWorld.EntityManager.CopyEntitiesFrom(serializedWorld.EntityManager, srcEntities);
                   srcEntities.Dispose(); */
            Load(conversionWorld);
        }
        private static SystemBase[] GetSavingSystem(EntityManager em)
        {
            return new SystemBase[] { new SaveIdentifierSystem(em), new SavePlayedSystem(em), new SaveHealthSystem(em), new SavePositionSystem(em) };
        }

        private NativeHashMap<Unity.Entities.Hash128, Entity> IndexIdentifiableEntities(EntityManager em)
        {
            var identifyEntityQuery = em.CreateEntityQuery(typeof(Identifier));
            using var loadingIdentifiers = identifyEntityQuery.ToComponentDataArray<Identifier>(Allocator.Temp);
            using var loadingEntities = identifyEntityQuery.ToEntityArray(Allocator.Temp);
            var loadingEntityByIdentifier = new NativeHashMap<Unity.Entities.Hash128, Entity>(loadingEntities.Length, Allocator.Persistent);
            for (int i = 0; i < loadingEntities.Length; i++)
            {
                var identifier = loadingIdentifiers[i];
                if (!loadingEntityByIdentifier.ContainsKey(identifier.Id))
                {
                    var entity = loadingEntities[i];
                    loadingEntityByIdentifier[identifier.Id] = entity;
                }
                else
                {
                    Debug.LogError("Multiple entity with same identifier");
                }

            }
            return loadingEntityByIdentifier;
        }

        private StreamBinaryReader CreateFileReader()
        {
            return CreateFileReader(SAVING_PATH);
        }
        private StreamBinaryReader CreateFileReader(string savePath)
        {
            DisposeFileReader();
            streamBinaryReader = new StreamBinaryReader(savePath);
            return streamBinaryReader;
        }
        public void AddQueryToConversionWorld(EntityQuery query)
        {
            AddQueryToWorld(World.EntityManager, conversionWorld, query);
        }
        public static void AddQueryToWorld(EntityManager srcEntityManager, World dstWorld, EntityQuery query)
        {
            var count = query.CalculateEntityCount();
            using var saveableEntitities = query.ToEntityArray(Allocator.Temp);
            using var outputs = new NativeArray<Entity>(count, Allocator.Temp);
            dstWorld.EntityManager.CopyEntitiesFrom(srcEntityManager, saveableEntitities, outputs);
            dstWorld.EntityManager.RemoveComponent<SceneTag>(outputs);
        }
        public void Save()
        {
            Save(SAVING_PATH);
        }
        public void Save(FixedString128 saveFile)
        {
            var serializedSavingWorld = Save(saveableQuery);
            using var binaryWriter = CreateFileWriter(saveFile);
            SerializeUtility.SerializeWorld(serializedSavingWorld.EntityManager, binaryWriter);
        }
        public World Save(EntityQuery query)
        {

            using var conversionWorld = RecreateSerializeConversionWorld();
            AddQueryToWorld(World.EntityManager, conversionWorld, query);
            Debug.Log($"Save query {conversionWorld.Name}");
            var serializedSavingWorld = GetOrCreateSerializedWorld();

            AddConversionSystems(conversionWorld, serializedSavingWorld.EntityManager);

            UpdateConversionSystems(conversionWorld);
            UpdateSerializedWorld(serializedSavingWorld);

            conversionWorld.EntityManager.CompleteAllJobs();
            serializedSavingWorld.EntityManager.CompleteAllJobs();

            return serializedSavingWorld;
        }

        private static void UpdateSerializedWorld(World world)
        {
            world.GetOrCreateSystem<InitializationSystemGroup>().Update();
        }
        private static void UpdateConversionSystems(World world)
        {
            world.GetOrCreateSystem<SavingConversionSystemGroup>().Update();
        }
        private static void AddConversionSystems(World conversionWorld, EntityManager dstManager)
        {
            SystemBase[] systems = GetSavingSystem(dstManager);
            var savingSystemGroup = conversionWorld.GetOrCreateSystem<SavingConversionSystemGroup>();
            foreach (var system in systems)
            {
                conversionWorld.AddSystem(system);
                savingSystemGroup.AddSystemToUpdateList(system);
            }
            savingSystemGroup.SortSystems();
        }
        private World GetOrCreateSerializedWorld()
        {
            if (serializedWorld == null || !serializedWorld.IsCreated)
            {
                serializedWorld = CreateSerializedWorld();
            }
            return serializedWorld;
        }

        private static World CreateSerializedWorld()
        {
            var serializedWorld = new World("Serialized World");
            var initializationSystemGroup = serializedWorld.GetOrCreateSystem<InitializationSystemGroup>();
            var endInitializationCommandBuffer = serializedWorld.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            initializationSystemGroup.AddSystemToUpdateList(endInitializationCommandBuffer);
            return serializedWorld;
        }

        private StreamBinaryWriter CreateFileWriter()
        {
            return CreateFileWriter(SAVING_PATH);
        }
        private StreamBinaryWriter CreateFileWriter(FixedString128 saveFile)
        {
            DisposeFileWriter();
            streamBinaryWriter = new StreamBinaryWriter(saveFile.ToString());
            return streamBinaryWriter;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DisposeFileWriter();
            DisposeFileReader();
            if (conversionWorld != null && conversionWorld.IsCreated)
            {
                conversionWorld.Dispose();
            }
            if (serializedWorld != null && serializedWorld.IsCreated)
            {
                serializedWorld.Dispose();
            }
        }

        private void DisposeFileReader()
        {
            if (streamBinaryReader != null)
            {
                streamBinaryReader.Dispose();
            }
        }

        private void DisposeFileWriter()
        {
            if (streamBinaryWriter != null)
            {
                streamBinaryWriter.Dispose();
            }
        }
    }
}
