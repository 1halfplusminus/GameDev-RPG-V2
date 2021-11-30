using Unity.Entities;

namespace RPG.Saving
{
    public interface ISerializer
    {
        object Serialize(EntityManager em, Entity e);
        void UnSerialize(EntityManager em, Entity e, object state);

        EntityQueryDesc GetEntityQueryDesc();
    }
}

