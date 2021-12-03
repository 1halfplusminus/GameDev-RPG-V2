using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace RPG.Combat
{
    //FIXME: Weapon Authoring ?
    public class FighterAuthoring : MonoBehaviour
    {
        public Transform WeaponSocket;

        public GameObject WeaponPrefab;
        //FIXME: Move to Weapon
        public float WeaponRange;

        public float AttackCooldown;

        public float AttackDuration;

        public List<float> HitEvents;

        public float WeaponDamage;
    }

}
