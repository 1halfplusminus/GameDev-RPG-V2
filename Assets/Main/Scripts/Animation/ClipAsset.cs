using UnityEngine;
using Unity.Animation;
using System;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Animation.Hybrid;

namespace RPG.Animation
{
    [Serializable]
    public class ClipAsset : ScriptableObject
    {
        public NativeArray<byte> NativeData;
        public byte[] Data;

        public World world;

        BlobAssetReference<Clip> Ref;

        private void OnAwake()
        {
            RebuildNativeData(Data);
        }
        private void OnEnable()
        {
            RebuildNativeData(Data);
        }
        private void InitData()
        {
            if (Data == null)
            {
                Data = new byte[0];
            }
        }

        public static BlobAssetReference<Clip> BuildClip(NativeArray<byte> data, out World world)
        {
            world = new World("Clip");
            var query = world.EntityManager.CreateEntityQuery(typeof(ChangeAttackAnimation));
            unsafe
            {
                using var binaryReader = new MemoryBinaryReader((byte*)data.GetUnsafePtr());
                SerializeUtility.DeserializeWorld(world.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
                world.EntityManager.EndExclusiveEntityTransaction();
                var changeAnimation = query.GetSingleton<ChangeAttackAnimation>();
                return changeAnimation.Animation;
            }
        }

        public BlobAssetReference<Clip> GetClip()
        {
            DisposeWorld();
            RebuildNativeData(Data);
            var clipRef = BuildClip(NativeData, out world);
            return clipRef;
            /* using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                UnityEngine.Debug.Log($"In GetClip {Data.Length} {clipRef.Value.FrameCount}");
                ref Clip clipRoot = ref blobBuilder.ConstructRoot<Clip>();
                SetRef(ref clipRoot.Bindings, ref clipRef.Value.Bindings);
                SetRef(ref clipRoot.Samples, ref clipRef.Value.Samples);
                SetRef(ref clipRoot.SynchronizationTags, ref clipRef.Value.SynchronizationTags);
                clipRoot.Duration = clipRef.Value.Duration;
                clipRoot.SampleRate = clipRef.Value.SampleRate;
                Ref = blobBuilder.CreateBlobAssetReference<Clip>(Allocator.Persistent);
                return Ref;
            } */

        }
#if UNITY_EDITOR
        public void SetClip(AnimationClip animationClip)
        {
            DisposeNativeDataIfNeeded();
            NativeData = GetClipData(animationClip);
            Data = NativeData.ToArray();
        }
        public static NativeArray<byte> GetClipData(AnimationClip animationClip)
        {
            using var world = new World("Clip");
            var clipAssetRef = animationClip.ToDenseClip();
            world.EntityManager.DestroyAndResetAllEntities();
            var clipEntity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(clipEntity, new ChangeAttackAnimation { Animation = clipAssetRef });
            unsafe
            {
                using var binaryWriter = new MemoryBinaryWriter();
                SerializeUtility.SerializeWorld(world.EntityManager, binaryWriter);
                var data = new NativeArray<byte>(binaryWriter.Length, Allocator.Persistent);
                for (int i = 0; i < binaryWriter.Length; i++)
                {
                    data[i] = binaryWriter.Data[i];
                }
                return data;
            }
        }
#endif
        private void RebuildWorld()
        {
            DisposeWorld();
            world = new World(GetInstanceID().ToString());
        }

        private void DisposeWorld()
        {
            if (world != null && world.IsCreated)
            {
                world.Dispose();
            }
        }

        private void RebuildNativeData(int size)
        {
            DisposeNativeDataIfNeeded();
            NativeData = new NativeArray<byte>(size, Allocator.Persistent);
        }

        private void DisposeNativeDataIfNeeded()
        {
            if (NativeData != null && NativeData.IsCreated)
            {
                NativeData.Dispose();
            }
        }

        private void RebuildNativeData(byte[] Data)
        {
            RebuildNativeData(Data != null ? Data.Length : 0);
            if (Data != null)
            {
                NativeData.CopyFrom(Data);
            }
        }
        private void OnDisable()
        {
            DisposeNativeDataIfNeeded();
            DisposeWorld();
        }
    }



}