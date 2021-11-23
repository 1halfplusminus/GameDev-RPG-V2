using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;
using System.IO;
using UnityEngine;
using Unity.Transforms;
using RPG.Mouvement;
using RPG.Gameplay;
using Unity.Jobs;
using Hash128 = Unity.Entities.Hash128;
using UnityEngine.Playables;
using System;

public struct Identifier : IComponentData
{
    public Unity.Entities.Hash128 Id;
}

public class SaveableConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.WithNone<PlayableDirector>().ForEach((Transform transform) =>
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(transform.gameObject.GetInstanceID());
            AddHashComponent(transform, hash);
        });
        Entities.ForEach((PlayableDirector director) =>
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(director.playableAsset.GetInstanceID());
            AddHashComponent(director, hash);
        });
    }

    private void AddHashComponent(Component transform, Hash128 hash)
    {
        var entity = TryGetPrimaryEntity(transform);
        if (entity != Entity.Null && !DstEntityManager.HasComponent<Identifier>(entity))
        {

            var identifier = new Identifier { Id = hash };
            DstEntityManager.AddComponentData<Identifier>(entity, identifier);
        }
    }
}
public struct Identified : IComponentData
{

}
public interface ISavingConversionSystem
{
    EntityManager DstEntityManager { get; }


}

public class SavingSystemGroup : ComponentSystemGroup { }
[DisableAutoCreation]
[UpdateInGroup(typeof(SavingSystemGroup))]
public class SavePositionSystem : SystemBase, ISavingConversionSystem
{

    public EntityManager DstEntityManager { get; }
    SavingConversionSystem conversionSystem;

    EntityCommandBufferSystem commandBufferSystem;
    public SavePositionSystem(EntityManager entityManager)
    {
        DstEntityManager = entityManager;

    }
    protected override void OnCreate()
    {
        base.OnCreate();
        conversionSystem = DstEntityManager.World.GetOrCreateSystem<SavingConversionSystem>();
        commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();

    }
    protected override void OnUpdate()
    {
        var identifiableEntities = conversionSystem.IdentifiableEntities;
        var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        Entities
        .WithReadOnly(identifiableEntities)
        .ForEach((int entityInQueryIndex, in Translation translation, in Identifier identifier) =>
        {
            var entity = SavingConversionSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
            pWriter.AddComponent(entityInQueryIndex, entity, translation);
            pWriter.AddComponent(entityInQueryIndex, entity, new WarpTo { Destination = translation.Value });
        }).ScheduleParallel();

        commandBufferSystem.AddJobHandleForProducer(Dependency);

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


}

[DisableAutoCreation]

[UpdateInGroup(typeof(SavingSystemGroup))]
public class SavePlayedSystem : SystemBase, ISavingConversionSystem
{

    public EntityManager DstEntityManager { get; }
    SavingConversionSystem conversionSystem;

    EntityCommandBufferSystem commandBufferSystem;
    public SavePlayedSystem(EntityManager entityManager)
    {
        DstEntityManager = entityManager;

    }
    protected override void OnCreate()
    {
        base.OnCreate();
        conversionSystem = DstEntityManager.World.GetOrCreateSystem<SavingConversionSystem>();
        commandBufferSystem = DstEntityManager.World.GetOrCreateSystem<EntityCommandBufferSystem>();

    }
    protected override void OnUpdate()
    {
        var identifiableEntities = conversionSystem.IdentifiableEntities;
        var pWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        Entities
        .WithReadOnly(identifiableEntities)
        .ForEach((int entityInQueryIndex, in Played played, in Identifier identifier) =>
        {
            var entity = SavingConversionSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
            pWriter.AddComponent(entityInQueryIndex, entity, played);
        }).ScheduleParallel();

        commandBufferSystem.AddJobHandleForProducer(Dependency);

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


}


public class SavingConversionSystem : SystemBase
{
    NativeHashMap<Unity.Entities.Hash128, Entity> identifiableEntities;

    EntityQuery identiableQuery;



    public NativeHashMap<Unity.Entities.Hash128, Entity> IdentifiableEntities { get { return identifiableEntities; } }

    EntityCommandBufferSystem entityCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        identifiableEntities = new NativeHashMap<Unity.Entities.Hash128, Entity>(0, Allocator.Persistent);
        entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

    }


    public static bool TryGetEntity(NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, out Entity entity)
    {
        entity = Entity.Null;
        if (entities.ContainsKey(hash))
        {
            entity = entities[hash];
            return true;
        }
        return false;
    }
    public static Entity GetOrCreateEntity(NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, EntityCommandBuffer transaction)
    {

        var entityFound = TryGetEntity(entities, hash, out var entity);
        if (!entityFound)
        {
            entity = transaction.CreateEntity();
            transaction.AddComponent<Identifier>(entity, new Identifier { Id = hash });

        }

        return entity;
    }
    public static Entity GetOrCreateEntity(NativeHashMap<Unity.Entities.Hash128, Entity> entities, Hash128 hash, EntityCommandBuffer.ParallelWriter transaction, int entityInQueryIndex)
    {

        var entityFound = TryGetEntity(entities, hash, out var entity);
        if (!entityFound)
        {
            entity = transaction.CreateEntity(entityInQueryIndex);
            transaction.AddComponent<Identifier>(entityInQueryIndex, entity, new Identifier { Id = hash });
        }

        return entity;
    }
    protected override void OnUpdate()
    {
        var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        identifiableEntities.Capacity += identiableQuery.CalculateEntityCount();
        var identifiedNowWritter = identifiableEntities.AsParallelWriter();
        var commandBufferP = commandBuffer.AsParallelWriter();
        Entities
        .WithStoreEntityQueryInField(ref identiableQuery)
        .WithNone<Identified>()
        .ForEach((int entityInQueryIndex, Entity e, in Identifier identifier) =>
        {
            identifiedNowWritter.TryAdd(identifier.Id, e);
            commandBufferP.AddComponent<Identified>(entityInQueryIndex, e);
        })
        .ScheduleParallel();

        entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        identifiableEntities.Dispose();
    }

}



[UpdateInGroup(typeof(InitializationSystemGroup))]
[DisableAutoCreation]
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
    object[] objects;
    protected override void OnCreate()
    {
        base.OnCreate();
        RecreateSerializeConversionWorld();
        saveableQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[] { ComponentType.ReadOnly(typeof(Identifier)) },
        });
    }


    protected override void OnUpdate()
    {
        Entities.ForEach((ref Serializable serializable) =>
        {
            serializable.Number += 1;
        }).ScheduleParallel();
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
        if (File.Exists(SAVING_PATH))
        {
            UnityEngine.Debug.Log("File Exists Loading File");
            // Load world from file
            using var conversionWorld = RecreateSerializeConversionWorld();
            using var binaryReader = CreateFileReader();
            SerializeUtility.DeserializeWorld(conversionWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
            conversionWorld.EntityManager.EndExclusiveEntityTransaction();

            AddConversionSystems(conversionWorld, World.EntityManager);
            UpdateConversionSystems(conversionWorld);
        }
    }
    public void Load(EntityQuery query)
    {
        // Load world from file
        using var conversionWorld = RecreateSerializeConversionWorld();
        AddQueryToConversionWorld(query);

        AddConversionSystems(conversionWorld, World.EntityManager);
        UpdateConversionSystems(conversionWorld);
    }
    private static SystemBase[] GetSavingSystem(EntityManager em)
    {
        return new SystemBase[] { new SavePositionSystem(em), new SavePlayedSystem(em) };
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
        DisposeFileReader();
        streamBinaryReader = new StreamBinaryReader(SAVING_PATH);
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
        var serializedSavingWorld = Save(saveableQuery);
        using var binaryWriter = CreateFileWriter();
        SerializeUtility.SerializeWorld(serializedSavingWorld.EntityManager, binaryWriter);
    }
    public World Save(EntityQuery query)
    {

        using var conversionWorld = RecreateSerializeConversionWorld();
        AddQueryToConversionWorld(query);
        Debug.Log($"Save query {conversionWorld.Name}");
        World serializedSavingWorld = GetOrCreateSerializedWorld();

        AddConversionSystems(conversionWorld, serializedSavingWorld.EntityManager);

        UpdateConversionSystems(conversionWorld);
        UpdateSerializedWorld(serializedSavingWorld);

        conversionWorld.EntityManager.CompleteAllJobs();
        serializedSavingWorld.EntityManager.CompleteAllJobs();

        return serializedSavingWorld;
    }
    public static void Save(EntityManager srcEntityManager, World conversionWorld, World SerializeWorld, EntityQuery query)
    {
        AddQueryToWorld(srcEntityManager, conversionWorld, query);

        World serializedSavingWorld = CreateSerializedWorld();

        AddConversionSystems(conversionWorld, serializedSavingWorld.EntityManager);

        UpdateConversionSystems(conversionWorld);
        UpdateSerializedWorld(serializedSavingWorld);

        conversionWorld.EntityManager.CompleteAllJobs();
        serializedSavingWorld.EntityManager.CompleteAllJobs();
    }
    private static void UpdateSerializedWorld(World world)
    {
        world.GetOrCreateSystem<InitializationSystemGroup>().Update();
    }
    private static void UpdateConversionSystems(World world)
    {
        world.GetOrCreateSystem<SavingSystemGroup>().Update();
    }
    private static void AddConversionSystems(World conversionWorld, EntityManager dstManager)
    {
        SystemBase[] systems = GetSavingSystem(dstManager);
        var savingSystemGroup = conversionWorld.GetOrCreateSystem<SavingSystemGroup>();
        foreach (var system in systems)
        {
            conversionWorld.AddSystem(system);
            savingSystemGroup.AddSystemToUpdateList(system);
        }
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
        DisposeFileWriter();
        streamBinaryWriter = new StreamBinaryWriter(SAVING_PATH);
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