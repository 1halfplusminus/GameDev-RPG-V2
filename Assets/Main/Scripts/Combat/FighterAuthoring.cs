using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace RPG.Combat
{
    using Unity.Animation.Hybrid;
    using UnityEngine;



    //FIXME: Weapon Authoring ?
    public class FighterAuthoring : MonoBehaviour
    {
        public WeaponAsset Weapon;
        public Transform LeftHandSocket;
        public Transform RightHandSocket;
    }

}
