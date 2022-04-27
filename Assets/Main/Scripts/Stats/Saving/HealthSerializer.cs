using Unity.Entities;
using UnityEngine;
using RPG.Core;

namespace RPG.Saving
{
    public struct HealthSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(Health)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize health {e}");
            return em.GetComponentData<Health>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Health state object {state}");
            if (state is Health health)
            {
                Debug.Log($"Unserialize health for {e} {health.Value}");
                em.AddComponentData(e, health);

            }

        }
    }
}

