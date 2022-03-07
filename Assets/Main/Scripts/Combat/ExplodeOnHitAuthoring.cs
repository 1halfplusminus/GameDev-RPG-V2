

using Unity.Entities;
using UnityEngine;

namespace RPG.Combat
{
    public struct ExplodeOnHit : IComponentData
    {

    }
    public class ExplodeOnHitAuthoring : MonoBehaviour
    {
        public float Radius;
    }

    public class ExplodeOnHitConversionSystem : ConvertToEntitySystem
    {

    }
}