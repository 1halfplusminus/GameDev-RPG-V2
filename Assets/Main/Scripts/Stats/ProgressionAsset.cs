

namespace RPG.Stats
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using Unity.Entities;
    using Unity.Collections;
    using static RPG.Stats.ProgressionAsset;

    [CreateAssetMenu(fileName = "ProgressionAsset", menuName = "RPG/Stats/New Progression", order = 0)]
    public class ProgressionAsset : ScriptableObject
    {
        [Serializable]
        public class ClassProgression
        {
            public CharacterClass CharacterClass;

            public List<float> Health;

            public List<float> Damage;
        }
        public List<ClassProgression> ProgressionByClass;

        public string GUID;
    }

    public struct Progression
    {
        public BlobArray<float> Health;

        public BlobArray<float> Damage;

        public float GetHealth(int level)
        {
            var index = level - 1;
            if (Health.Length >= index)
            {
                return Health[index];
            }
            return 0;
        }
    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class ProgressionBlobAssetSystem : SystemBase
    {
        BlobAssetStore blobAssetStore;
        protected override void OnCreate()
        {
            base.OnCreate();
            blobAssetStore = new BlobAssetStore();
        }
        protected override void OnUpdate()
        {

        }
        public static BlobAssetReference<Progression> GetProgression(ClassProgression asset)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Progression>();
            builder.Construct(ref root.Health, asset.Health.ToArray());
            builder.Construct(ref root.Damage, asset.Damage.ToArray());
            var rootRef = builder.CreateBlobAssetReference<Progression>(Allocator.Persistent);
            return rootRef;
        }
        public static Unity.Entities.Hash128 GetHash(ClassProgression asset)
        {
            return GetHash(asset.CharacterClass);
        }
        public static Unity.Entities.Hash128 GetHash(CharacterClass characterClass)
        {
            var hash = new UnityEngine.Hash128();
            hash.Append(((byte)characterClass));
            return hash;
        }
        public void AddProgression(ProgressionAsset progressionAsset)
        {
            foreach (var progressionByClass in progressionAsset.ProgressionByClass)
            {
                if (!GetProgression(progressionByClass.CharacterClass).IsCreated)
                {
                    var progressionRef = GetProgression(progressionByClass);
                    var hash = GetHash(progressionByClass);
                    blobAssetStore.TryAdd(hash, progressionRef);
                }
            }
        }
        public BlobAssetReference<Progression> GetProgression(CharacterClass characterClass)
        {
            blobAssetStore.TryGet<Progression>(GetHash(characterClass), out var progressionRef);
            return progressionRef;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (blobAssetStore != null)
            {
                blobAssetStore.Dispose();
            }
        }
    }
    public class ProgressionConversionSystem : GameObjectConversionSystem
    {
        ProgressionBlobAssetSystem progressionBlobAssetSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            progressionBlobAssetSystem = DstEntityManager.World.GetOrCreateSystem<ProgressionBlobAssetSystem>();
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((ProgressionAsset progressionAsset) =>
            {
                progressionBlobAssetSystem.AddProgression(progressionAsset);
            });
        }
    }
}