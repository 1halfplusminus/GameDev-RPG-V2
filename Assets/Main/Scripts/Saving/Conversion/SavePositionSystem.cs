using Unity.Entities;
using Unity.Transforms;
using RPG.Mouvement;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using RPG.Core;
using Unity.Scenes;
using RPG.Control;

namespace RPG.Saving
{
    // FIXME: Refactor like in scene
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateBefore(typeof(SaveInSceneSystem))]
    public class SavePositionSystem : SaveConversionSystemBase<Translation>
    {
        public SavePositionSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity,
                                        Entity entity)
        {
            var component = GetComponent(entity);
            // If saving type is File Copy Position to dst world
            var savingState = GetSingleton<SavingState>();
            if (savingState.Type == SavingStateType.FILE)
            {
                CopyPositionToDstWorld(dstEntity, component);
            }
            // If is loading from state restore only position for entity not player controlled & with in scene
            if (savingState.Type == SavingStateType.SCENE)
            {
                if (!EntityManager.HasComponent<InScene>(entity))
                {
                    CopyPositionToDstWorld(dstEntity, component);
                }
            }
        }

        private void CopyPositionToDstWorld(Entity dstEntity, Translation component)
        {
            var savingState = GetSingleton<SavingState>();
            Debug.Log($"{savingState.Direction} translation for {dstEntity}");
            DstEntityManager.AddComponentData(dstEntity, component);
            DstEntityManager.AddComponentData(dstEntity, new WarpTo { Destination = component.Value });
        }
    }

}
