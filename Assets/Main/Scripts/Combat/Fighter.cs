using RPG.Mouvement;
using Unity.Entities;

namespace RPG.Combat
{
    [WriteGroup(typeof(MoveTo))]

    public struct Fighter : IComponentData
    {
        public Entity Target;
        public bool MoveTowardTarget;

        public float WeaponRange;
    }

}