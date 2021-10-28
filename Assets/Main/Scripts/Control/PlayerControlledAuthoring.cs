using UnityEngine;
using Unity.Entities;
using RPG.Combat;

namespace RPG.Control
{
    [RequireComponent(typeof(FighterAuthoring))]
    public class PlayerControlledAuthoring : MonoBehaviour
    {
        public float RaycastDistance;

    }
}
