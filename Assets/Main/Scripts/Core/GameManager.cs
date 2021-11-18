using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;
using System.IO;
using Unity.Animation.Hybrid;
using UnityEngine;
using RPG.Control;
using Unity.Transforms;
using RPG.Mouvement;
using RPG.Gameplay;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Jobs;
using Hash128 = Unity.Entities.Hash128;

public struct Identifier : IComponentData
{
    public Unity.Entities.Hash128 Id;
}

public class SaveableConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Transform transform) =>
        {
            var entity = TryGetPrimaryEntity(transform);
            if (entity != Entity.Null)
            {
                var id = transform.gameObject.GetInstanceID();
                var hash = new UnityEngine.Hash128();
                hash.Append(id);
                var identifier = new Identifier { Id = hash };
                DstEntityManager.AddComponentData<Identifier>(entity, identifier);
            }
        });
    }
}
public struct Identified : IComponentData
{

}
public interface ISavingConversionSystem
{
    EntityManager DstEntityManager { get; set; }
}
[DisableAutoCreation]
[UpdateAfter(typeof(SavingConversionSystem))]
public class SavePositionSystem : SystemBase, ISavingConversionSystem
{

    public EntityManager DstEntityManager { get; set; }
    SavingConversionSystem conversionSystem;
    EntityCommandBuffer commandBuffer;

    public SavePositionSystem(EntityManager entityManager)
    {
        DstEntityManager = entityManager;
    }
    protected override void OnCreate()
    {
        base.OnCreate();
        conversionSystem = DstEntityManager.World.GetOrCreateSystem<SavingConversionSystem>();
        commandBuffer = new EntityCommandBuffer(Allocator.Persistent);
    }
    protected override void OnUpdate()
    {
        var identifiableEntities = conversionSystem.IdentifiableEntities;
        var pWriter = commandBuffer.AsParallelWriter();
        Entities
        .WithReadOnly(identifiableEntities)
        .ForEach((int entityInQueryIndex, in Translation translation, in Identifier identifier) =>
        {
            var entity = SavingConversionSystem.GetOrCreateEntity(identifiableEntities, identifier.Id, pWriter, entityInQueryIndex);
            pWriter.AddComponent(entityInQueryIndex, entity, translation);
        }).ScheduleParallel();
        Dependency.Complete();
        var exclusifTransaction = DstEntityManager.BeginExclusiveEntityTransaction();
        commandBuffer.Playback(exclusifTransaction);
        DstEntityManager.EndExclusiveEntityTransaction();
    }

    protected override void OnDestroy()
    {
        base.OnCreate();
        commandBuffer.Dispose();
    }
}
[DisableAutoCreation]
public class SavingConversionSystem : SystemBase
{
    NativeHashMap<Unity.Entities.Hash128, Entity> identifiableEntities;

    EntityQuery identiableQuery;

    EntityCommandBuffer commandBuffer;

    public NativeHashMap<Unity.Entities.Hash128, Entity> IdentifiableEntities { get { return identifiableEntities; } }

    protected override void OnCreate()
    {
        base.OnCreate();
        identifiableEntities = new NativeHashMap<Unity.Entities.Hash128, Entity>(0, Allocator.Persistent);
        commandBuffer = new EntityCommandBuffer(Allocator.Persistent);

    }
    public JobHandle GetOutputDependency()
    {
        return Dependency;
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
        Dependency.Complete();
        var exclusifTransaction = EntityManager.BeginExclusiveEntityTransaction();
        commandBuffer.Playback(exclusifTransaction);
        EntityManager.EndExclusiveEntityTransaction();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        identifiableEntities.Dispose();
        commandBuffer.Dispose();
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
            All = new ComponentType[] { ComponentType.ReadOnly(typeof(Translation)), ComponentType.ReadOnly(typeof(Identifier)) },
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
    }
    private void RecreateLoadingWorld()
    {
        if (loadingWorld != null && loadingWorld.IsCreated)
        {
            loadingWorld.Dispose();
        }

        loadingWorld = new World("Loading Conversion");
    }
    public void Load()
    {
        if (File.Exists(SAVING_PATH))
        {
            UnityEngine.Debug.Log("File Exists Loading File");
            var currentWorld = World;
            RecreateLoadingWorld();
            using var binaryReader = CreateFileReader();
            SerializeUtility.DeserializeWorld(loadingWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader, objects);
            loadingWorld.EntityManager.EndExclusiveEntityTransaction();

            var loadingWorldIdentifiableEntities = IndexIdentifiableEntities(loadingWorld.EntityManager);
            var currentWorldIdentifiableEntities = IndexIdentifiableEntities(currentWorld.EntityManager);
            using var loadingEntitiesHash = loadingWorldIdentifiableEntities.GetKeyArray(Allocator.Temp);
            foreach (var hash in loadingEntitiesHash)
            {
                if (currentWorldIdentifiableEntities.ContainsKey(hash))
                {
                    Debug.Log("Find corresponding entities");
                    if (loadingWorld.EntityManager.HasComponent<Translation>(loadingWorldIdentifiableEntities[hash]))
                    {
                        var translation = loadingWorld.EntityManager.GetComponentData<Translation>(loadingWorldIdentifiableEntities[hash]);
                        currentWorld.EntityManager.AddComponentData<Translation>(currentWorldIdentifiableEntities[hash], new Translation() { Value = translation.Value });
                        currentWorld.EntityManager.AddComponentData<WarpTo>(currentWorldIdentifiableEntities[hash], new WarpTo() { Destination = translation.Value });
                    }
                    if (loadingWorld.EntityManager.HasComponent<Played>(loadingWorldIdentifiableEntities[hash]))
                    {
                        currentWorld.EntityManager.AddComponent<Played>(currentWorldIdentifiableEntities[hash]);
                    }

                }
            }

            loadingWorldIdentifiableEntities.Dispose();
            currentWorldIdentifiableEntities.Dispose();

            loadingWorld.EntityManager.CompleteAllJobs();
            currentWorld.EntityManager.CompleteAllJobs();
        }
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
        using var saveableEntitities = saveableQuery.ToEntityArray(Allocator.Temp);
        using var outputs = new NativeArray<Entity>(saveableQuery.CalculateEntityCountWithoutFiltering(), Allocator.Persistent);
        savingWorld.EntityManager.CopyEntitiesFrom(World.EntityManager, saveableEntitities, outputs);
        savingWorld.EntityManager.RemoveComponent<SceneTag>(outputs);

    }
    public void Save()
    {
        RecreateSavingWorld();
        AddQueryToSaveWorld(saveableQuery);
        AddQueryToSaveWorld(GetEntityQuery(typeof(Played)));

        var savingConversionWorld = new World("Saving");

        savingConversionWorld.GetOrCreateSystem<SavingConversionSystem>().Update();
        savingConversionWorld.Update();

        savingConversionWorld.GetOrCreateSystem<SavingConversionSystem>().GetOutputDependency().Complete();

        var savePositionSystem = new SavePositionSystem(savingConversionWorld.EntityManager);
        savingWorld.AddSystem(savePositionSystem);

        savePositionSystem.Update();

        savingWorld.EntityManager.CompleteAllJobs();
        savingConversionWorld.EntityManager.CompleteAllJobs();

        using var binaryWriter = CreateFileWriter();
        SerializeUtility.SerializeWorld(savingConversionWorld.EntityManager, binaryWriter);
        /*   SerializeUtility.SerializeWorld(savingWorld.EntityManager, binaryWriter, out objects); */

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