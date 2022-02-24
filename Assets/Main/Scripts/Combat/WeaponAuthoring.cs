using Unity.Entities;
using UnityEngine;

namespace RPG.Combat
{
    public struct WeaponAssetReference : IComponentData
    {
        public Unity.Entities.Hash128 GUID;

        public BlobAssetReference<WeaponAssetData> Weapon;
    }
    public class WeaponAuthoring : MonoBehaviour
    {
        public WeaponAsset WeaponAsset;
    }

}
