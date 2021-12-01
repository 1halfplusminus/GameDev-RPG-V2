using RPG.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace RPG.Saving
{
    public struct PlayedSerialization : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(Played)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize played for {e}");
            return true;
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"UnSerialize played for {e}");
            Debug.Log($"Add played for {e}");
            em.AddComponent<Played>(e);
        }
    }
}

