

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RPG.Core
{
    public struct Addressable : IComponentData
    {
        public FixedString64 Address;
    }
    public class AddressableAuthoring : MonoBehaviour
    {
        public string Address;
    }

    public class AddressableConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((AddressableAuthoring addressableAuthoring) =>
            {
                var entity = GetPrimaryEntity(addressableAuthoring);
                DstEntityManager.AddComponentData<Addressable>(entity, new Addressable { Address = addressableAuthoring.Address });
            });
        }
    }
}