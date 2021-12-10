using Unity.Entities;
using UnityEngine;

namespace RPG.Combat
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        public Transform Target;
        public float Speed;

        public bool IsHoming;
    }

}