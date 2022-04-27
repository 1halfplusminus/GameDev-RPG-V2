using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using static RPG.Stats.ProgressionAsset;
namespace RPG.Stats
{
  
    [CreateAssetMenu(fileName = "ProgressionAsset", menuName = "RPG/Stats/New Progression", order = 0)]
    public class ProgressionAsset : ScriptableObject
    {
        [Serializable]
        public class ProgressionCurve
        {
            public AnimationCurve Curve;
            public Stats Stats;
            public float MinValue;
            public float MaxValue;
        }

        [Serializable]
        public class ClassProgression
        {
            public int MaxLevel;
            public CharacterClass CharacterClass;

            public ProgressionCurve[] Stats;
        }
        public List<ClassProgression> ProgressionByClass;

        public string GUID;
    }

    [Serializable]
    public struct Progression
    {
        public BlobArray<BlobArray<float>> Stats;


        public float GetStat(Stats stat)
        {
            return GetStat(stat, 1);
        }
        public float GetStat(Stats stat, int level)
        {
            return GetStat((int)stat, level);
        }
        public float GetStat(int stat, int level)
        {
            if (Stats.Length > stat)
            {
                var index = level - 1;
                ref var array = ref Stats[stat];
                if (array.Length != 0 && array.Length > index)
                {
                    return array[index];
                }
            }
            return 0;
        }
        public float[] GetStats(in Stats stat)
        {
            return Stats[(int)stat].ToArray();
        }

    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ProgressionBlobAssetSystem : SystemBase
    {
        public BlobAssetStore BlobAssetStore;
        protected override void OnCreate()
        {
            base.OnCreate();
            BlobAssetStore = new BlobAssetStore();
        }
        protected override void OnUpdate()
        {

        }

        private static float[] CurveToArray(AnimationCurve curve)
        {
            var lastKey = curve.keys.LastOrDefault();
            var results = new float[(int)lastKey.time + 1];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = curve.Evaluate(i);
            }
            return results;
        }
        public static BlobAssetReference<Progression> CreateProgression(ClassProgression asset)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Progression>();
            var stats = Enum.GetValues(typeof(Stats));
            var nestedArrays = builder.Allocate(ref root.Stats, stats.Length);
            for (int i = 0; i < asset.Stats.Length; i++)
            {
                var key = (int)asset.Stats[i].Stats;
                var statsData = CurveToArray(asset.Stats[i].Curve);
                var nestedArray = builder.Allocate(ref nestedArrays[key], statsData.Length);
                for (int j = 0; j < statsData.Length; j++)
                {
                    nestedArray[j] = statsData[j];
                }

            }
            var rootRef = builder.CreateBlobAssetReference<Progression>(Allocator.Persistent);
            builder.Dispose();
            return rootRef;
        }
        public static Unity.Entities.Hash128 GetHash(ClassProgression asset)
        {
            return GetHash(asset.CharacterClass);
        }
        public static Unity.Entities.Hash128 GetHash(CharacterClass characterClass)
        {
            var hash = new UnityEngine.Hash128();
            hash.Append((byte)characterClass);
            return hash;
        }
        public void AddProgression(ProgressionAsset progressionAsset)
        {
            foreach (var progressionByClass in progressionAsset.ProgressionByClass)
            {
                if (!GetProgression(progressionByClass.CharacterClass).IsCreated)
                {
                    var progressionRef = CreateProgression(progressionByClass);
                    var hash = GetHash(progressionByClass);
                    BlobAssetStore.TryAdd(hash, progressionRef);
                }
            }
        }
        public BlobAssetReference<Progression> GetProgression(CharacterClass characterClass)
        {
            var result = BlobAssetStore.TryGet<Progression>(GetHash(characterClass), out var progressionRef);
            return progressionRef;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (BlobAssetStore != null)
            {
                BlobAssetStore.Dispose();
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