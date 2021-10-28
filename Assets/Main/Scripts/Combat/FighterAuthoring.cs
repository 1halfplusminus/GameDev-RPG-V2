using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace RPG.Combat
{

    public class FighterAuthoring : MonoBehaviour
    {
        public float WeaponRange;

        public float AttackCooldown;

        public float AttackDuration;

        public List<float> HitEvents;
    }

}
