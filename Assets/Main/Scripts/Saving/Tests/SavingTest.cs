
using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using Unity.Entities.Serialization;
using Unity.Scenes;
using RPG.Saving;
using System.Runtime.InteropServices;
using RPG.Core;

namespace RPG.Test
{

    struct ChunkComponents
    {
        public ulong Type;
        public BlobArray<byte> Values;
    }

    struct SerializableChunk
    {
        public BlobArray<Entity> Entities;
        public BlobArray<ChunkComponents> Components;

        public BlobArray<ulong> Types;

    }
    struct Save
    {
        public BlobArray<int> Data;
    }
    struct TestSave : IComponentData
    {
        public BlobArray<SerializableChunk> Data;
    }
    struct SavedComponent : IComponentData
    {
        public int Value;
    }
    
    public class SavingTest
    {

        [Test]
        public void TestSavingNativeArray()
        {
            // using var writer = new MemoryBinaryWriter();
            var path = Application.streamingAssetsPath + "/test.raw";
            var test = new NativeArray<int>(1, Allocator.Temp);
            test[0] = 1;
            using var blobBuilder = new BlobBuilder(Allocator.Persistent);
            ref var save = ref blobBuilder.ConstructRoot<Save>();
            var arrayBuilder = blobBuilder.Allocate(ref save.Data, test.Length);
            unsafe
            {
                UnsafeUtility
                .MemCpy(arrayBuilder.GetUnsafePtr(), test.GetUnsafePtr(), test.Length);
            }
            // using var blobRef = blobBuilder.CreateBlobAssetReference<Save>(Allocator.Temp);
            BlobAssetReference<Save>.Write(blobBuilder, path, 1);
            test.Dispose();
            // writer.WriteArray(test);
            // using var stream = File.Open(Application.streamingAssetsPath + "/test.raw", FileMode.OpenOrCreate);
            // var streamWriter = new System.IO.BinaryWriter(stream);
            // unsafe
            // {
            //     var ptr = test.GetUnsafePtr();
            //     for (int i = 0; i < writer.Length; i++)
            //     {
            //         streamWriter.Write(UnsafeUtility.ReadArrayElement<byte>(ptr, i));
            //     }
            // }
            // test.Dispose();
        }
        [Test]
        public void TestReadingNativeArray()
        {
            var path = Application.streamingAssetsPath + "/test.raw";
            BlobAssetReference<Save>.TryRead(path, 1, out var result);
            if (result.IsCreated)
            {
                for (int i = 0; i < result.Value.Data.Length; i++)
                {
                    Debug.Log($"{result.Value.Data[i]}");
                }
            }
        }
        [Test]
        public void TestSaving()
        {
            var path = Application.streamingAssetsPath + "/save.raw";
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            // Create test entity
            var entity = em.CreateEntity();
            em.AddComponents(entity, new ComponentTypes(new ComponentType[] { typeof(SavedComponent) }));
            em.SetComponentData(entity, new SavedComponent { Value = 10 });
            // Get chuncks
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<SavedComponent>());
            var chunks = query.CreateArchetypeChunkArray(Allocator.Temp);
            // Create Saveable blob asset
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var save = ref blobBuilder.ConstructRoot<TestSave>();
            var chuncksBuilder = blobBuilder.Allocate(ref save.Data, chunks.Length);

            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var componentTypes = chunk.Archetype.GetComponentTypes(Allocator.Temp);
                var entities = chunk.GetNativeArray(em.GetEntityTypeHandle());
                // create serializedChunck
                unsafe
                {
                    chuncksBuilder[i] = new SerializableChunk { };
                    // Build blob for serializable chunk
                    var entitiesBuilder = blobBuilder.Allocate(ref chuncksBuilder[i].Entities, chunk.ChunkEntityCount);
                    var chunckTypesBuilder = blobBuilder.Allocate(ref chuncksBuilder[i].Types, componentTypes.Length);
                    var chunkComponentsBuilder = blobBuilder.Allocate(ref chuncksBuilder[i].Components, componentTypes.Length);
                    UnsafeUtility.MemCpy(entitiesBuilder.GetUnsafePtr(), entities.GetUnsafeReadOnlyPtr(), chunk.ChunkEntityCount);
                    for (int j = 0; j < componentTypes.Length; j++)
                    {

                        var componentType = componentTypes[j];
                        var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
                        int elementSize = typeInfo.ElementSize;
                        chunkComponentsBuilder[j].Type = typeInfo.StableTypeHash;
                        chunckTypesBuilder[j] = typeInfo.StableTypeHash;
                        if (!componentType.IsZeroSized)
                        {
                            DynamicComponentTypeHandle chunkComponentType = em.GetDynamicComponentTypeHandle(componentType);

                            var components = chunk.GetDynamicComponentDataArrayReinterpret<byte>(chunkComponentType, elementSize);
                            var componentsBuilder = blobBuilder.Allocate(ref chunkComponentsBuilder[j].Values, components.Length);
                            UnsafeUtility.MemCpy(componentsBuilder.GetUnsafePtr(), components.GetUnsafePtr(), components.Length);
                        }

                        // writer.WriteArray(components);
                        // components.Dispose();
                    }
                }

                // componentTypes.Dispose();
            }
            BlobAssetReference<TestSave>.Write(blobBuilder, path, 1);
            chunks.Dispose();
        }
        [Test]
        public void TestSaveSerializeWorld()
        {
            // var catalog = Addressables.LoadContentCatalogAsync(Application.streamingAssetsPath + "/aa/catalog.json");
            // catalog.WaitForCompletion();
          
            var path = "world.asset";
            using var world = new World("Test");
            using var store = new BlobAssetStore();
            var gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(world, store);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Main/Character/Dady/Dady 1.prefab");
            var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(go, gameObjectConversionSettings);
            using var writer = new MemoryBinaryWriter();
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            SerializeUtilityHybrid.Serialize(world.EntityManager, writer, out var objRefs);
            ref var save = ref blobBuilder.ConstructRoot<TestSaveDeserializeWorld>();
            var dataBuilder = blobBuilder.Allocate(ref save.Data, writer.Length);
            var addressesBuilder = blobBuilder.Allocate(ref save.GUIDS, objRefs.Array.Length);
            for(int i = 0 ; i < addressesBuilder.Length; i++){
                blobBuilder.AllocateString(
                    ref addressesBuilder[i], 
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(objRefs.Array[i])));
            }
            unsafe
            {
                UnsafeUtility.MemCpy(dataBuilder.GetUnsafePtr(), writer.Data, writer.Length);
            }
            using var memoryBinaryWriter = new MemoryBinaryWriter();
            var fullPath = Application.dataPath + "/" + path; 
            BlobAssetReference<TestSaveDeserializeWorld>.Write( memoryBinaryWriter,blobBuilder, 1);
            BinaryAsset asset = AssetDatabase.LoadAssetAtPath<BinaryAsset>("Assets/"+path);
            if(asset == null){
                asset = ScriptableObject.CreateInstance<BinaryAsset>();
                asset.Data = new byte[0];
                AssetDatabase.CreateAsset(asset,"Assets/"+path);
            }
            byte[] arr = new byte[memoryBinaryWriter.Length];
            unsafe{
                Marshal.Copy((IntPtr)memoryBinaryWriter.Data, arr, 0, memoryBinaryWriter.Length);
            }
            asset.Data = arr;
            AssetDatabase.SaveAssetIfDirty(asset);
            // var textAsset = new TextAsset(Encoding.ASCII.GetString(arr));
            // AssetDatabase.ImportAsset("Assets/"+path);
            // AssetDatabase.CreateAsset(textAsset,path);
            // AssetDatabase.Refresh();
            // var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            var objRefSaved = AssetDatabase.LoadAssetAtPath<ReferencedUnityObjects>("Assets/"+path);
            if(objRefSaved != null){
                objRefSaved.Array = objRefs.Array;
                objRefSaved.CompanionObjectIndices = objRefs.CompanionObjectIndices;
                EditorUtility.SetDirty(objRefSaved);
            } else {
                objRefSaved = objRefs;
                AssetDatabase.AddObjectToAsset(objRefSaved,asset);
               objRefSaved.name = "Raw";
            }
            AssetDatabase.SaveAssetIfDirty(asset);
            AddressableExtensions.SetAddressableGroup(path,"Default Local Group");
            blobBuilder.Dispose();
        }
        [Test]
        public void TestSaveDeserializeWorld()
        {
            var path = Application.streamingAssetsPath + "/world.raw";
            BlobAssetReference<TestSaveDeserializeWorld>.TryRead(path, 1, out var saveRef);
            ref var save = ref saveRef.Value.Data;
            using var world = new World("Test");
            unsafe
            {
                using var reader = new MemoryBinaryReader((byte*)save.GetUnsafePtr(), save.Length);
                var objectRefs = ScriptableObject.CreateInstance<ReferencedUnityObjects>();
                // SerializeUtilityHybrid.SerializeObjectReferences(new UnityEngine.Object[] {go},out var objectRefs);
                // JsonUtility.FromJsonOverwrite(saveRef.Value.Json.ToString(), objectRefs);
                // objectRefs.Array[objectRefs.CompanionObjectIndices[0]] = go;
                SerializeUtilityHybrid.Deserialize(world.EntityManager, reader, objectRefs);
                World.DefaultGameObjectInjectionWorld.EntityManager.MoveEntitiesFrom(out var outputs, world.EntityManager);
                for (int i = 0; i < outputs.Length; i++)
                {
                    World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(outputs[i]);
                }
                
                Debug.Log("Finish");
            }
        }
        [Test]
        public void TestReadSave()
        {
            var path = Application.streamingAssetsPath + "/save.raw";
            BlobAssetReference<TestSave>.TryRead(path, 1, out var result);
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            var breakEntity = defaultWorld.EntityManager.CreateEntity();
            defaultWorld.EntityManager.AddComponentData(breakEntity, new SavedComponent { Value = 2 });
            // var entityRemap = defaultWord.EntityManager.CreateEntityRemapArray(Allocator.Temp);
            if (result.IsCreated)
            {
                ref var save = ref result.Value;
                for (int i = 0; i < save.Data.Length; i++)
                {
                    Debug.Log($"Found Entity {save.Data[i].Entities[0].Index}");
                    var e = defaultWorld.EntityManager.CreateEntity();
                    for (var j = 0; j < save.Data[i].Components.Length; j++)
                    {
                        ref var chunkComponent = ref save.Data[i].Components[j];
                        var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(chunkComponent.Type);
                        var componentType = TypeManager.GetType(typeIndex);
                        var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                        defaultWorld.EntityManager.AddComponent(e, componentType);
                        var dynamicComponentTypeHandle = defaultWorld.EntityManager.GetDynamicComponentTypeHandle(componentType);
                        var chunk = defaultWorld.EntityManager.GetChunk(e);
                        var components = chunk.GetDynamicComponentDataArrayReinterpret<byte>(dynamicComponentTypeHandle, typeInfo.ElementSize);
                        unsafe
                        {
                            var start = (chunk.ChunkEntityCount - save.Data[i].Entities.Length) * typeInfo.ElementSize;
                            UnsafeUtility.MemCpy(components.Slice(start).GetUnsafePtr(), chunkComponent.Values.GetUnsafePtr(), chunkComponent.Values.Length);
                        }
                        Debug.Log($"{componentType.Name}");
                    }
                    var savedComponent = defaultWorld.EntityManager.GetComponentData<SavedComponent>(e);
                    Debug.Log($" Saved component : {savedComponent.Value}");
                }
            }
            var existingComponent = defaultWorld.EntityManager.GetComponentData<SavedComponent>(breakEntity);
            Debug.Log($"Existing component : {existingComponent.Value}");
        }
    }

}
