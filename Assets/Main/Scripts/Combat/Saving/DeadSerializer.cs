using Unity.Entities;
using UnityEngine;
using RPG.Core;

namespace RPG.Saving
{
    public struct DeadSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(IsDeadTag)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize is dead {e}");
            return em.HasComponent<IsDeadTag>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Is Dead state object {state}");
            if (state is true)
            {
                Debug.Log($"Unserialize is dead for {e}");
                em.AddComponent<IsDeadTag>(e);
            }
        }
    }
}

