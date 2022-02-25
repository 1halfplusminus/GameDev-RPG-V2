using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RPG.Combat
{
    public struct WeaponAssetReference : IComponentData
    {
        public Unity.Entities.Hash128 GUID;

        public FixedString64 Address;

    }
    public class WeaponAuthoring : MonoBehaviour
    {
        public WeaponAsset WeaponAsset;
    }

}
