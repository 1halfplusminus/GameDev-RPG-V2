

using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace RPG.Hybrid
{

    public class TerrainColliderAuthoring : MonoBehaviour
    {
        public PhysicsCategoryTags BelongTo;
        public PhysicsCategoryTags CollidWith;

        public CollisionFilter GetFilter()
        {
            return new CollisionFilter { CollidesWith = CollidWith.Value, BelongsTo = BelongTo.Value };
        }
    }
}