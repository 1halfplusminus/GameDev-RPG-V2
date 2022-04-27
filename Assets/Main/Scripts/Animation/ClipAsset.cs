using UnityEngine;
using Unity.Animation;
using System;
using Unity.Entities;
using Unity.Entities.Serialization;


#if UNITY_EDITOR
using Unity.Animation.Hybrid;
#endif

namespace RPG.Animation
{
    [Serializable]
    public class ClipAsset : ScriptableObject
    {
        public byte[] Data;

        public BlobAssetStore blobAssetStore;

        private void InitData()
        {
            if (Data == null)
            {
                Data = new byte[0];
            }
        }

        public static BlobAssetReference<Clip> BuildClip(byte[] data, string name)
        {
            // using var world = new World("Clip", WorldFlags.Conversion);
            // var query = world.EntityManager.CreateEntityQuery(typeof(ChangeAttackAnimation));
            unsafe
            {
                fixed (byte* ptr = &data[0])
                {
                    using var binaryReader = new MemoryBinaryReader(ptr, data.Length);
                    BlobAssetReference<Clip> clone = default;
                    try
                    {
                        clone = binaryReader.Read<Clip>();
                    }
                    catch (Exception)
                    {
                        UnityEngine.Debug.Log($"Enable to read clip {name}");
                    }
                    return clone;
                    // SerializeUtility.DeserializeWorld(world.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
                    // world.EntityManager.EndExclusiveEntityTransaction();
                    // var changeAnimation = query.GetSingleton<ChangeAttackAnimation>();
                    // var clone = changeAnimation.Animation.Clone();


                }

            }

        }

        public BlobAssetReference<Clip> GetClip()
        {

            var clipRef = BuildClip(Data, this.name);
            return clipRef;
        }
#if UNITY_EDITOR
        public void SetClip(AnimationClip animationClip)
        {
            Data = GetClipData(animationClip);
        }
        public static byte[] GetClipData(AnimationClip animationClip)
        {
            // using var world = new World("Clip", WorldFlags.Conversion);
            var clipAssetRef = animationClip.ToDenseClip();
            // var clipBuilder = new BlobBuilder(Unity.Collections.Allocator.Persistent);
            // clipBuilder.Dispose();
            // BlobAssetReference<Clip>.Write(clipBuilder, "path", "test");
            // world.EntityManager.DestroyAndResetAllEntities();
            // var clipEntity = world.EntityManager.CreateEntity();
            // world.EntityManager.AddComponentData(clipEntity, new ChangeAttackAnimation { Animation = clipAssetRef });
            unsafe
            {
                using var binaryWriter = new MemoryBinaryWriter();
                binaryWriter.Write(clipAssetRef);
                // SerializeUtility.SerializeWorld(world.EntityManager, binaryWriter);
                var data = new byte[binaryWriter.Length];
                for (int i = 0; i < binaryWriter.Length; i++)
                {
                    data[i] = binaryWriter.Data[i];
                }
                return data;
            }
        }
#endif

    }



}