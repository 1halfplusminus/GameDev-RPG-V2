using System.Collections.Generic;
using RPG.Animation;
using UnityEngine;
namespace RPG.Combat
{


    [CreateAssetMenu(fileName = "WeaponAsset", menuName = "RPG/Make Weapon", order = 0)]
    public class WeaponAsset : ScriptableObject
    {

        public GameObject WeaponPrefab;

        // FIXME: SHOULD BE IN WEAPON AUTHORING
        public AnimationClip Animation;

        public GameObject Projectile;

        public float Damage;

        public float Range;

        public float Cooldown;
        // Fixme should be set with custom editor
        public float AttackDuration;

        public SocketType SocketType;

        // Should be set with custom editor
        public List<float> HitEvents;

        public string GUID;

        public ClipAsset Clip;

    }

}
