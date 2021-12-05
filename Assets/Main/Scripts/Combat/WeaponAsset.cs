namespace RPG.Combat
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "WeaponAsset", menuName = "RPG/Make Weapon", order = 0)]
    public class WeaponAsset : ScriptableObject
    {
        public GameObject WeaponPrefab;
        public AnimationClip Animation;

        public float Damage;

        public float Range;

        public float Cooldown;
        // Fixme should be set with custom editor
        public float AttackDuration;

        // Should be set with custom editor
        public List<float> HitEvents;

    }

}