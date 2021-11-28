using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;
using System.IO;
using UnityEngine;
using RPG.Core;


namespace RPG.Saving
{

    public struct HasSpawnIdentified : IComponentData
    {

    }

    public enum SavingStateType : byte
    {

        SCENE = 1 << 0,
        FILE = 3 << 0,
    }
    public enum SavingStateDirection : byte
    {

        LOADING = 4 << 0,
        SAVING = 12 << 0,

    }


    public struct SavingState : IComponentData
    {
        public SavingStateDirection Direction;
        public SavingStateType Type;

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

        private void CreateSavingStateEntity(EntityManager em, SavingStateType type, SavingStateDirection direction)
        {
            var entity = em.CreateEntity(typeof(SavingState));
            em.AddComponentData(entity, new SavingState { Direction = direction, Type = type });
        }
        private void UnloadAllCurrentlyLoadedScene(EntityManager dstManager)
        {
            if (dstManager.World.Flags == WorldFlags.Game)
            {
                var loadedSceneQuery = dstManager.CreateEntityQuery(typeof(SceneLoaded));
                using var currentLoadedScene = loadedSceneQuery.ToComponentDataArray<SceneLoaded>(Allocator.Temp);
                using var loadedScenes = loadedSceneQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < currentLoadedScene.Length; i++)
                {
                    dstManager.AddComponentData(loadedScenes[i], new UnloadScene() { SceneEntity = loadedScenes[i] });
                }
            }

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

            var conversionWorld = new World("Saving Conversion", WorldFlags.Staging);
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
                Debug.Log("File Exists Loading File");
                UnloadAllCurrentlyLoadedScene(EntityManager);
                LoadFileInSerializedWorld(saveFile);
                LoadSerializedWorld(SavingStateType.FILE);

            }
        }

        private void LoadFileInSerializedWorld(FixedString128 saveFile)
        {

            // Load world from file
            using var tempFileConversionWorld = RecreateSerializeConversionWorld();
            using var binaryReader = CreateFileReader(saveFile.ToString());
            SerializeUtility.DeserializeWorld(tempFileConversionWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
            conversionWorld.EntityManager.EndExclusiveEntityTransaction();

            var serializedWorld = GetOrCreateSerializedWorld();
            CreateSavingStateEntity(tempFileConversionWorld.EntityManager, SavingStateType.FILE, SavingStateDirection.SAVING);
            AddConversionSystems(tempFileConversionWorld, serializedWorld.EntityManager);
            UpdateConversionSystems(tempFileConversionWorld);

        }

        public void Load(World conversionWorld, SavingStateType type)
        {
            CreateSavingStateEntity(conversionWorld.EntityManager, type, SavingStateDirection.LOADING);
            AddConversionSystems(conversionWorld, World.EntityManager);
            UpdateConversionSystems(conversionWorld);
        }

        public void LoadSerializedWorld(SavingStateType type)
        {
            using var conversionWorld = RecreateSerializeConversionWorld();
            var serializedWorld = GetOrCreateSerializedWorld();
            var serializedWorldIdentified = serializedWorld
            .EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Identifier>());
            AddQueryToWorld(serializedWorld.EntityManager, conversionWorld, serializedWorldIdentified);
            Load(conversionWorld, type);
        }

        private static List<SystemBase> GetSavingSystem(EntityManager em)
        {
            return new List<SystemBase> { new MapIdentifierSystem(em), new SavePlayedSystem(em), new SaveHealthSystem(em), new SavePositionSystem(em), new SaveInSceneSystem(em) };
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
        }
        public void Save()
        {
            Save(SAVING_PATH);
        }
        public void Save(FixedString128 saveFile)
        {

            var serializedSavingWorld = Save(saveableQuery, SavingStateType.FILE);
            using var binaryWriter = CreateFileWriter(saveFile);
            SerializeUtility.SerializeWorld(serializedSavingWorld.EntityManager, binaryWriter);
        }
        public World Save(EntityQuery query, SavingStateType type)
        {


            using var conversionWorld = RecreateSerializeConversionWorld();
            CreateSavingStateEntity(conversionWorld.EntityManager, type, SavingStateDirection.SAVING);
            AddQueryToWorld(World.EntityManager, conversionWorld, query);
            Debug.Log($"Save query {conversionWorld.Name}");
            var serializedSavingWorld = GetOrCreateSerializedWorld();

            AddConversionSystems(conversionWorld, serializedSavingWorld.EntityManager);

            UpdateConversionSystems(conversionWorld);
            UpdateSerializedWorld(serializedSavingWorld);

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
        private static void AddConversionSystem(World conversionWorld, SystemBase system)
        {
            var savingSystemGroup = conversionWorld.GetOrCreateSystem<SavingConversionSystemGroup>();
            conversionWorld.AddSystem(system);
            savingSystemGroup.AddSystemToUpdateList(system);
            savingSystemGroup.SortSystems();
        }
        private static void AddConversionSystems(World conversionWorld, EntityManager dstManager)
        {
            var systems = GetSavingSystem(dstManager);
            Debug.Log($"Add Conversion Systems for dst world to {dstManager.World.Name} with flag: {dstManager.World.Flags}");
            var savingSystemGroup = conversionWorld.GetOrCreateSystem<SavingConversionSystemGroup>();
            if ((dstManager.World.Flags & WorldFlags.Conversion) != WorldFlags.None)
            {
                Debug.Log($"Add Create Identifier to {dstManager.World.Name} with flag: {dstManager.World.Flags}");
                var createIdentifierSystem = new CreateIdentifierSystem(dstManager);
                systems.Add(createIdentifierSystem);
            }
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
            var serializedWorld = new World("Serialized World", WorldFlags.Conversion);
            var initializationSystemGroup = serializedWorld.GetOrCreateSystem<InitializationSystemGroup>();
            var endInitializationCommandBuffer = serializedWorld.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            initializationSystemGroup.AddSystemToUpdateList(endInitializationCommandBuffer);
            return serializedWorld;
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
