using System.Collections.Generic;
using RPG.Control;
using RPG.Saving;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RPG.Combat
{
    public struct EquipableSocketsSerializer : ISerializer
    {

        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(EquipableSockets),
                    typeof(PlayerControlled)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            var equipableSockets = em.GetComponentData<EquipableSockets>(e);
            var weapons = new List<string>();
            foreach (var socket in equipableSockets.ToList())
            {
                if (em.HasComponent<Equipped>(socket))
                {
                    var equipped = em.GetComponentData<Equipped>(socket);
                    Debug.Log($"Serialize weapon {equipped.Equipable.Value.Weapon.Range}  {equipped.Equipable.Value.Weapon.GUID} for {e}");
                    weapons.Add(equipped.Equipable.Value.Weapon.GUID.ToString());
                }
            }
            return weapons;
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"weapon state object {state}");
            var equipableSockets = em.GetComponentData<EquipableSockets>(e);
            var convertToEntitySystem = em.World.GetExistingSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(em.World, convertToEntitySystem.BlobAssetStore);
            if (state is List<string> weapons)
            {
                Debug.Log($"State is list of weapon {weapons.Count}");
                foreach (var weaponAddress in weapons)
                {
                    var weaponAuthoringHandle = Addressables.LoadAssetAsync<GameObject>("Hero/" + weaponAddress);
                    var weaponAuthoring = weaponAuthoringHandle.WaitForCompletion();
                    var weaponPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(weaponAuthoring.gameObject, conversionSetting);
                    Debug.Log($"Unserialize weapon at address {weaponAddress} , prefab {weaponPrefab.Index}");
                    var hash = new UnityEngine.Hash128();
                    hash.Append(weaponAddress);
                    BlobAssetReference<WeaponBlobAsset> weaponBlobAsset;
                    var hasWeaponAsset = convertToEntitySystem.BlobAssetStore.TryGet(hash, out weaponBlobAsset);
                    if (hasWeaponAsset)
                    {
                        Debug.Log($"Unserialize weapon {weaponBlobAsset.Value}");
                        var weaponEntity = weaponBlobAsset.Value.Entity;
                        var weapon = weaponBlobAsset.Value.Weapon;
                        var socket = equipableSockets.GetSocketForWeapon(weapon);
                        em.AddComponentData(weaponEntity, new EquipInSocket { Socket = socket });
                    }

                }

            }

        }
    }
}

