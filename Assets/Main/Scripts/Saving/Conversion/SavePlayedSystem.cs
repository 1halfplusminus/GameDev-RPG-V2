using Unity.Entities;
using RPG.Gameplay;
using Unity.Jobs;
using RPG.Core;
using Unity.Animation;
using Unity.Transforms;
using Unity.Deformations;
using Unity.Collections;
using RPG.Mouvement;

namespace RPG.Saving
{
    [DisableAutoCreation]

    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateAfter(typeof(MapIdentifierSystem))]
    public class SavePlayedSystem : SaveConversionSystemBase<Played>
    {
        public SavePlayedSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity, Entity entity, Played component, SceneSection section)
        {
            Debug.Log($"Save played for {dstEntity}");
            DstEntityManager.AddComponent<Played>(dstEntity);
        }
    }
}
