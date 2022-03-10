using Unity.Collections;
using Unity.Entities;

namespace RPG.Combat
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    struct Loaded : IComponentData
    {

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    class WeaponReferenceSystemLoader : SystemBase
    {
        NativeHashMap<FixedString64, WeaponAssetData> weapons;
        EntityQuery weaponReferenceQuery;
        EntityQuery weaponDataQuery;

        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            weapons = new NativeHashMap<FixedString64, WeaponAssetData>(0, Allocator.Persistent);
            entityCommandBufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
            // RequireForUpdate(weaponReferenceQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            weapons.Clear();
            weapons.Dispose();
            weapons = new NativeHashMap<FixedString64, WeaponAssetData>(weaponDataQuery.CalculateEntityCount(), Allocator.Persistent);
            var weaponsWriter = weapons.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref weaponDataQuery)
            .ForEach((in WeaponAssetData weaponData) => weaponsWriter.TryAdd(weaponData.Weapon.Value.Weapon.GUID, weaponData))
            .ScheduleParallel();
            var _weapons = weapons;
            Entities
            .WithReadOnly(_weapons)
            .WithAll<Prefab>()
            .WithStoreEntityQueryInField(ref weaponReferenceQuery)
            .WithNone<Loaded>()
            .ForEach((Entity _, in WeaponAssetReference weaponAssetReference) =>
            {
                if (!_weapons.ContainsKey(weaponAssetReference.Address))
                {
                    LoadWeapon(weaponAssetReference.Address);
                }
            }).WithoutBurst().Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        public AsyncOperationHandle<GameObject> LoadAssetAsync(FixedString64 address)
        {
            return Addressables.LoadAssetAsync<GameObject>(address.ToString());
        }
        public Entity LoadWeapon(FixedString64 address)
        {

            var weaponAuthoringHandle = LoadAssetAsync(address);
            var weaponAuthoring = weaponAuthoringHandle.WaitForCompletion();
            Entity weaponPrefab = ConvertWeapon(weaponAuthoring);
            Addressables.Release(weaponAuthoringHandle);
            return weaponPrefab;
        }

        private Entity ConvertWeapon(GameObject weaponAuthoring)
        {
            var convertToEntitySystem = World.GetExistingSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(World, convertToEntitySystem.BlobAssetStore);
            return GameObjectConversionUtility.ConvertGameObjectHierarchy(weaponAuthoring.gameObject, conversionSetting);
        }

        protected override void OnDestroy()
        {
            weapons.Dispose();
            base.OnDestroy();
        }
    }

}
