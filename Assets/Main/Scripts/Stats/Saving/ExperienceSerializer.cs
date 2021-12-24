using Unity.Entities;
using UnityEngine;
using RPG.Control;
using RPG.Saving;

namespace RPG.Stats
{
    public struct ExperienceSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(PlayerControlled),
                    typeof(ExperiencePoint)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize experience point {e}");
            return em.GetComponentData<ExperiencePoint>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Experience state object {state}");
            if (state is ExperiencePoint experiencePoint)
            {
                Debug.Log($"Unserialize health for {e} {experiencePoint.Value}");
                em.AddComponentData(e, experiencePoint);

            }

        }
    }
}

