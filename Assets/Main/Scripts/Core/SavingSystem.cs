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

[DisableAutoCreation]
[UpdateAfter(typeof(SavingConversionSystem))]
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
[UpdateAfter(typeof(SavingConversionSystem))]
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

    World loadingWorld;
    World savingWorld;

    StreamBinaryWriter streamBinaryWriter;

    StreamBinaryReader streamBinaryReader;

    EntityQuery saveableQuery;

    object[] objects;
    protected override void OnCreate()
    {
        base.OnCreate();
        RecreateSavingWorld();
        RecreateLoadingWorld();
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

    private void RecreateSavingWorld()
    {
        if (savingWorld != null && savingWorld.IsCreated)
        {
            savingWorld.Dispose();
        }

        savingWorld = new World("Saving Conversion");

        savingWorld.CreateSystem<UpdateWorldTimeSystem>();
        savingWorld.CreateSystem<InitializationSystemGroup>();
        savingWorld.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    private void RecreateLoadingWorld()
    {
        if (loadingWorld != null && loadingWorld.IsCreated)
        {
            loadingWorld.Dispose();
        }

        loadingWorld = new World("Loading");
    }
    public void Load()
    {
        if (File.Exists(SAVING_PATH))
        {
            UnityEngine.Debug.Log("File Exists Loading File");
            // Load world from file
            RecreateLoadingWorld();
            using var binaryReader = CreateFileReader();
            SerializeUtility.DeserializeWorld(loadingWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
            loadingWorld.EntityManager.EndExclusiveEntityTransaction();
            SystemBase[] systems = GetSavingSystem(World.EntityManager);
            foreach (var system in systems)
            {
                loadingWorld.AddSystem(system);
                system.Update();
            }

            loadingWorld.EntityManager.CompleteAllJobs();

        }
    }

    private SystemBase[] GetSavingSystem(EntityManager em)
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
    public void AddQueryToSaveWorld(EntityQuery query)
    {
        AddQueryToWorld(savingWorld, query);
    }
    public void AddQueryToWorld(World world, EntityQuery query)
    {
        var count = query.CalculateEntityCountWithoutFiltering();
        using var saveableEntitities = query.ToEntityArray(Allocator.Temp);
        using var outputs = new NativeArray<Entity>(query.CalculateEntityCountWithoutFiltering(), Allocator.Temp);
        world.EntityManager.CopyEntitiesFrom(World.EntityManager, saveableEntitities, outputs);
        world.EntityManager.RemoveComponent<SceneTag>(outputs);


    }
    public void Save()
    {
        RecreateSavingWorld();
        AddQueryToSaveWorld(saveableQuery);

        var savingConversionWorld = new World("Saving");
        SystemBase[] systems = GetSavingSystem(savingConversionWorld.EntityManager);
        foreach (var system in systems)
        {
            savingWorld.AddSystem(system);
            system.Update();
        }

        savingConversionWorld.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>().Update();

        savingWorld.EntityManager.CompleteAllJobs();
        savingConversionWorld.EntityManager.CompleteAllJobs();

        using var binaryWriter = CreateFileWriter();
        SerializeUtility.SerializeWorld(savingConversionWorld.EntityManager, binaryWriter);
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
        if (loadingWorld != null && loadingWorld.IsCreated)
        {
            loadingWorld.Dispose();
        }
        if (savingWorld != null && savingWorld.IsCreated)
        {
            savingWorld.Dispose();
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