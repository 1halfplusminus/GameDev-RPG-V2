using RPG.Combat;
using RPG.Core;
using RPG.Stats;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

namespace RPG.Gameplay
{
    public struct LevelUpEffect : IComponentData
    {
        public Entity Prefab;
    }
    public class LevelUpEffectAuthoring : MonoBehaviour
    {
        public VisualEffectReference Effect;
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class LevelUpDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate() => Entities.ForEach((LevelUpEffectAuthoring levelEffectAuthoring) =>
        {
            levelEffectAuthoring.Effect.ReleaseAsset();
            var handle = levelEffectAuthoring.Effect.LoadAssetAsync<GameObject>();
            levelEffectAuthoring.Effect.OperationHandle.Completed += (_) => DeclareReferencedPrefab(handle.Result);
            handle.WaitForCompletion();
        });
    }

    public class LevelUpEffectConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((LevelUpEffectAuthoring levelEffectAuthoring) =>
            {
                var entity = GetPrimaryEntity(levelEffectAuthoring);
                var prefabEntity = GetPrimaryEntity(levelEffectAuthoring.Effect.OperationHandle.Result as GameObject);
                DstEntityManager.AddComponentData(entity, new LevelUpEffect { Prefab = prefabEntity });
                Addressables.Release(levelEffectAuthoring.Effect.OperationHandle);
            });
        }
    }
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class LevelUpEffectSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities.WithAll<LeveledUp>()
            .ForEach((int entityInQueryIndex, Entity e, in LevelUpEffect effect) =>
            {
                var instance = cbp.Instantiate(entityInQueryIndex, effect.Prefab);
                cbp.AddComponent<LocalToParent>(entityInQueryIndex, instance);
                cbp.AddComponent(entityInQueryIndex, instance, new Parent { Value = e });
                cbp.AddComponent<Playing>(entityInQueryIndex, e);
                cbp.AddComponent<Spawned>(entityInQueryIndex, instance);
                cbp.AddComponent<DestroyIfNoParticule>(entityInQueryIndex, instance);
            }).ScheduleParallel();

            Entities
            .WithAll<DestroyIfNoParticule>()
            .WithNone<Spawned>()
            .WithAll<Playing>()
            .ForEach((Entity e, VisualEffect effect) =>
            {
                effect.AdvanceOneFrame();
                if (effect.aliveParticleCount == 0)
                {
                    Debug.Log("Destroying visual effect");
                    cb.DestroyEntity(e);
                }
            }).WithoutBurst().Run();

            RestoreHealthOnLevelUp();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void RestoreHealthOnLevelUp()
        {
            Entities
            .WithAll<LeveledUp>()
            .ForEach((ref Health health, in BaseStats baseStats, in RestaureHealthPercent restaureHealthPercent) =>
            {
                var newHealth = restaureHealthPercent.GetNewHealth(health, baseStats);
                Debug.Log($"Restaure health on level up new health: {newHealth} old health {health.Value}");
                health.Value = newHealth;
            }).ScheduleParallel();
        }
    }
}
