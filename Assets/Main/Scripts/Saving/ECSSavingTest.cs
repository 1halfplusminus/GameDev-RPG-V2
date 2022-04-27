using System.Collections.Generic;
using RPG.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RPG.Saving
{
    public struct TestSaveDeserializeWorld
    {
        public BlobArray<byte> Data;
        public BlobArray<BlobString> GUIDS;
    }
    [DisableAutoCreation]
    public struct TestSystem : ISystem
    {
        public bool Runned;
        public void OnCreate(ref SystemState state)
        {
            Runned = false;
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        public void OnUpdate(ref SystemState state)
        {
            if (Runned) { return; }
            Runned = true;
            unsafe
            {
                var cubePrefab = Addressables.LoadAssetAsync<ReferencedUnityObjects>("Assets/world.asset");
                var data = Addressables.LoadAssetAsync<BinaryAsset>("Assets/world.asset");
                data.WaitForCompletion();
                cubePrefab.WaitForCompletion();
                if(data.Status != AsyncOperationStatus.Succeeded) {
                    return;
                }
                // Addressables.InstantiateAsync("Assets/Main/Character/Cube.prefab");
                unsafe
                {
                    fixed (byte* ptr = data.Result.Data)
                    {
                        // var test = data.Result as object;
                        using var blobReader = new MemoryBinaryReader(ptr, data.Result.Data.Length);
                        BlobAssetReference<TestSaveDeserializeWorld>.TryRead(blobReader, 1, out var saveRef);
                        ref var save = ref saveRef.Value.Data;
                        ref var guids = ref saveRef.Value.GUIDS;
                        using var world = new World("Test");
                        unsafe
                        {
                            using var reader = new MemoryBinaryReader((byte*)save.GetUnsafePtr(), save.Length);
                            var objectRefs = cubePrefab.Result;
                            // objectRefs.Array = new Object[guids.Length];
                            // objectRefs.CompanionObjectIndices = new int[0];
                            // for(int i = 0; i < guids.Length; i++){
                            //     var assetReference = new AssetReference(guids[i].ToString());
                            //     var assetHandle = assetReference.LoadAssetAsync<Object>();
                            //     assetHandle.WaitForCompletion();
                            //     objectRefs.Array[i] = assetHandle.Result;
                            // }
                            // JsonUtility.FromJsonOverwrite(saveRef.Value.Json.ToString(), objectRefs);
                            // var meshRenderer = cubePrefab.Result.GetComponentInChildren<Renderer>();
                            // if(meshRenderer != null){
                            //     var materials = meshRenderer.sharedMaterials;
                            //     var objects = new Object[materials.Length + 1];
                            // //     var meshFilter = cubePrefab.Result.GetComponentInChildren<MeshFilter>();
                            // //     if(meshFilter != null){
                            // //         objects[0] = meshFilter.sharedMesh;
                            // //     }
                            // //     var skinnedMeshRenderer = cubePrefab.Result.GetComponentInChildren<SkinnedMeshRenderer>();
                            // //     if(skinnedMeshRenderer != null){
                            // //         objects[0] = skinnedMeshRenderer.sharedMesh;
                            // //     }
                            // //     for(int i = 0 ; i < materials.Length; i++){
                            // //         objects[i+1] = materials[i];
                            // //     }
                            // //     objectRefs.Array = objects;
                            // // }
                            // // var meshRenderer = cubePrefab.Result.GetComponent<SkinnedMeshRenderer>();
                            SerializeUtilityHybrid.Deserialize(world.EntityManager, reader, objectRefs);

                            var realWorld = state.World;
                            realWorld.EntityManager.MoveEntitiesFrom(out var outputs, world.EntityManager);
                            for (int i = 0; i < outputs.Length; i++)
                            {
                                realWorld.EntityManager.Instantiate(outputs[i]);
                            }
                            Debug.Log("Finish");
                            outputs.Dispose();
                        }
                    }
                }
            }
        }
    }
}