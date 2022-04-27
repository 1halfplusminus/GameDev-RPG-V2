using Unity.Entities;
using UnityEngine;
using RPG.Saving;
using RPG.Control;
using Unity.Collections;
using Unity.Entities.Serialization;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace RPG.Stats
{
    public struct ComponentSerializer : IDisposable
    {
        public NativeArray<byte> Data;

        public World World;

        public void DisposeWorld()
        {
            if (World != null && World.IsCreated)
            {
                World.Dispose();
            }
        }
        public void Dispose()
        {

            DisposeWorld();
            DisposeData();
        }

        public void DisposeData()
        {
            if (Data != null && Data.IsCreated)
            {
                Data.Dispose();
            }
        }

        public T UnSerialize<T>(byte[] data) where T : struct, IComponentData
        {
            if (Data != null && Data.IsCreated)
            {
                Data.Dispose();
            }
            Data = new NativeArray<byte>(data.Length, Allocator.Persistent);
            Data.CopyFrom(data);
            var world = GetWorld();
            var query = world.EntityManager.CreateEntityQuery(typeof(T));
            unsafe
            {
                using var binaryReader = new MemoryBinaryReader((byte*)Data.GetUnsafePtr(),data.Length);
                SerializeUtility.DeserializeWorld(world.EntityManager.BeginExclusiveEntityTransaction(), binaryReader);
                world.EntityManager.EndExclusiveEntityTransaction();
                var component = query.GetSingleton<T>();
                return component;
            }

        }
        public byte[] Serialize<T>(T component) where T : struct, IComponentData
        {
            using var world = GetWorld();
            world.EntityManager.DestroyAndResetAllEntities();
            var clipEntity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(clipEntity, component);
            unsafe
            {
                using var binaryWriter = new MemoryBinaryWriter();
                SerializeUtility.SerializeWorld(world.EntityManager, binaryWriter);
                Data = new NativeArray<byte>(binaryWriter.Length, Allocator.Persistent);
                for (int i = 0; i < binaryWriter.Length; i++)
                {
                    Data[i] = binaryWriter.Data[i];
                }
                return Data.ToArray();
            }
        }

        private World GetWorld()
        {
            if (World == null || !World.IsCreated)
            {
                World = new World("ComponentSerializer");
            }
            return World;
        }
    }

    public struct BaseStatsSerializer : ISerializer
    {
        ProgressionBlobAssetSystem progressionBlobAssetSystem;
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(BaseStats)
                }
            };
        }
        public ProgressionBlobAssetSystem GetProgressionBlobAssetSystem(World world)
        {
            if (progressionBlobAssetSystem == null)
            {
                progressionBlobAssetSystem = world.GetExistingSystem<ProgressionBlobAssetSystem>();
            }
            return progressionBlobAssetSystem;
        }
        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize base stats {e}");
            var baseStats = em.GetComponentData<BaseStats>(e);
            // var componentSerializer = new ComponentSerializer();
            return baseStats;
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            // var componentSerializer = new ComponentSerializer();

            // Debug.Log($"base state {state}");
            if (state is BaseStats baseStats)
            {
                var currentBaseStats = em.GetComponentData<BaseStats>(e);
                // var baseStats = componentSerializer.UnSerialize<BaseStats>(data);
                baseStats.ProgressionAsset = currentBaseStats.ProgressionAsset;
                Debug.Log($"Unserialize baseStats for {e} {baseStats.Level} {baseStats.ProgressionAsset.Value.GetStat(Stats.Health, 1)}");
                em.AddComponentData(e, baseStats);
            }
            // componentSerializer.DisposeData()

        }
    }
    public struct ExperienceSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(PlayerControlled),
                    typeof(ExperiencePoint)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize experience point {e}");
            return em.GetComponentData<ExperiencePoint>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Experience state object {state}");
            if (state is ExperiencePoint experiencePoint)
            {
                Debug.Log($"Unserialize health for {e} {experiencePoint.Value}");
                em.AddComponentData(e, experiencePoint);

            }

        }
    }
}

