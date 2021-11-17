using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using RPG.Core;
using Unity.Entities.Serialization;
using Unity.Collections;
using System.IO;
using RPG.Control;

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
            All = new ComponentType[] { ComponentType.ReadOnly(typeof(Unity.Transforms.LocalToWorld)), ComponentType.ReadOnly(typeof(PlayerControlled)) },

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

        savingWorld = new World("Saving");
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
            Debug.Log("File Exists Loading File");
            var currentWorld = World;
            RecreateLoadingWorld();
            using var binaryReader = CreateFileReader();
            SerializeUtility.DeserializeWorld(loadingWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader, objects);
            loadingWorld.EntityManager.EndExclusiveEntityTransaction();
            loadingWorld.EntityManager.CompleteAllJobs();
            currentWorld.EntityManager.CompleteAllJobs();
            currentWorld.EntityManager.CopyAndReplaceEntitiesFrom(loadingWorld.EntityManager);
        }
    }

    private StreamBinaryReader CreateFileReader()
    {
        DisposeFileReader();
        streamBinaryReader = new StreamBinaryReader(SAVING_PATH);
        return streamBinaryReader;
    }

    public void Save()
    {

        var currentWorld = World;
        RecreateSavingWorld();
        using var saveableEntitities = saveableQuery.ToEntityArray(Allocator.Temp);
        using var binaryWriter = CreateFileWriter();
        using var outputs = new NativeArray<Entity>(saveableQuery.CalculateEntityCountWithoutFiltering(), Allocator.Temp);
        savingWorld.EntityManager.CopyEntitiesFrom(currentWorld.EntityManager, saveableEntitities, outputs);
        savingWorld.EntityManager.RemoveComponent<SceneTag>(outputs);
        savingWorld.EntityManager.CompleteAllJobs();
        SerializeUtility.SerializeWorld(savingWorld.EntityManager, binaryWriter, out objects);
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
public class GameManager : MonoBehaviour
{

    World loadingWorld;
    // Start is called before the first frame update
    void Start()
    {
        /*  Debug.Log("Try Loading Saved File");
         if (File.Exists(SAVING_PATH))
         {
             Debug.Log("File Exists Loading File");
             var currentWorld = World.DefaultGameObjectInjectionWorld;
             loadingWorld = new World("Loading");
             var binaryReader = new StreamBinaryReader(SAVING_PATH);
             SerializeUtility.DeserializeWorld(loadingWorld.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
             loadingWorld.EntityManager.EndExclusiveEntityTransaction();
             currentWorld.EntityManager.CopyAndReplaceEntitiesFrom(loadingWorld.EntityManager);
             binaryReader.Dispose();
         } */

    }

    public void Save()
    {
        /*    if (loadingWorld != null)
           {
               loadingWorld.Dispose();
           }

           var currentWorld = World.DefaultGameObjectInjectionWorld;
           var savingWorld = new World("Saving");
           var binaryWriter = new StreamBinaryWriter(SAVING_PATH);
           var saveableQuery = currentWorld.EntityManager.CreateEntityQuery(typeof(Serializable));
           var saveableEntitities = saveableQuery.ToEntityArray(Allocator.Temp);
           savingWorld.EntityManager.CopyEntitiesFrom(currentWorld.EntityManager, saveableEntitities);
           SerializeUtility.SerializeWorld(savingWorld.EntityManager, binaryWriter);
           savingWorld.Dispose();
           saveableEntitities.Dispose();
           binaryWriter.Dispose(); */
    }
    void Destroy()
    {

    }
}
