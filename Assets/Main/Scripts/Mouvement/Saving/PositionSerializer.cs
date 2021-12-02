using RPG.Core;
using RPG.Mouvement;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RPG.Saving
{
    /*     public struct WarpToSerializer : ISerializer
        {
            public EntityQueryDesc GetEntityQueryDesc()
            {
                return new EntityQueryDesc()
                {
                    All = new ComponentType[] {
                        typeof(WarpTo)
                    }
                };
            }

            public object Serialize(EntityManager em, Entity e)
            {
                Debug.Log($"Serialize warp to {e}");
                return em.GetComponentData<WarpTo>(e);
            }

            public void UnSerialize(EntityManager em, Entity e, object state)
            {
                Debug.Log($"Unserialize warp to for ${e}");
                if (state is WarpTo warpTo)
                {
                    if (!em.HasComponent<TriggeredSceneLoaded>(e))
                    {
                        em.AddComponentData(e, new Translation { Value = warpTo.Destination });
                        em.AddComponentData(e, new WarpTo { Destination = translation.Value });
                    }

                }

            }
        } */
    public struct PositionSerializer : ISerializer
    {
        public EntityQueryDesc GetEntityQueryDesc()
        {
            return new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    typeof(Translation)
                }
            };
        }

        public object Serialize(EntityManager em, Entity e)
        {
            Debug.Log($"Serialize translation {e}");
            return em.GetComponentData<Translation>(e);
        }

        public void UnSerialize(EntityManager em, Entity e, object state)
        {
            Debug.Log($"Unserialize translation for ${e}");
            if (state is Translation translation)
            {
                if (!em.HasComponent<TriggeredSceneLoaded>(e))
                {
                    em.AddComponentData(e, new Translation { Value = translation.Value });
                    em.AddComponentData(e, new WarpTo { Destination = translation.Value });
                }

            }

        }
    }
}

