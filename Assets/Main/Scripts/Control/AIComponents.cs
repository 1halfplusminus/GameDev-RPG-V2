
using Unity.Entities;
using UnityEngine;

namespace RPG.Control
{

    public struct AIControlled : IComponentData { }
    public struct ChasePlayer : IComponentData
    {
        public float ChaseDistance;
    }
    public class ChasePlayerAuthoring : MonoBehaviour
    {
        public float ChaseDistance;
    }

}