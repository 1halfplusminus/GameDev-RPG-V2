
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
using UnityEngine.AI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Unity.Burst;
using RPG.Mouvement;

namespace RPG.Test
{


    // unsafe struct PathComponent : IComponentData, ISystemStateComponentData
    // {
    //     public IntPtr Ptr;
    // }
    // // struct CleanUpPath : IJobChunk
    // // {
    // //     EntityTypeHandle
    // // }
    // struct InitializePath : IJobChunk
    // {
    //     EntityTypeHandle EntityTypeHandle;
    //     EntityCommandBuffer entityCommandBuffer;

    //     public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //     {
    //         var path = new NavMeshPath();
    //         var entities = chunk.GetNativeArray(EntityTypeHandle);
    //         for (int i = 0; i < entities.Length; i++)
    //         {
    //             // var handle = GCHandle.Alloc(path, GCHandleType.Weak);
    //             // entityCommandBuffer
    //             // .AddComponent(entities[i], new PathComponent { Ptr = handle.AddrOfPinnedObject() });
    //         }
    //     }
    // }

    public class HybridTest
    {
        [Test]
        public void TestGC()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = em.CreateEntity();
            // em.AddComponentData(entity, new PathComponent());

        }
    }

}
