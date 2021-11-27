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
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SavingConversionSystemGroup))]
    [UpdateBefore(typeof(SaveInSceneSystem))]
    public class SavePositionSystem : SaveConversionSystemBase<Translation>
    {
        public SavePositionSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override void Convert(Entity dstEntity,
                                        Entity entity,
                                       Translation component, SceneSection section)
        {
            // If saving type is File Copy Position to dst world
            var savingState = GetSingleton<SavingState>();
            if (savingState.Type == SavingStateType.FILE)
            {
                CopyPositionToDstWorld(dstEntity, entity, component);
            }
            // If is loading from state restore only position for entity not player controlled & with in scene
            if (savingState.Type == SavingStateType.SCENE)
            {
                if (!EntityManager.HasComponent<InScene>(entity))
                {
                    CopyPositionToDstWorld(dstEntity, entity, component);
                }
            }
        }

        private void SavePositionNPC(Entity dstEntity, Entity entity, Translation component)
        {
            if (DstEntityManager.HasComponent<InScene>(dstEntity)) { return; }
            CopyPositionToDstWorld(dstEntity, entity, component);
        }

        private void CopyPositionToDstWorld(Entity dstEntity, Entity entity, Translation component)
        {
            Debug.Log($"Save translation for {dstEntity}");
            DstEntityManager.AddComponentData<Translation>(dstEntity, component);
            DstEntityManager.AddComponentData<WarpTo>(dstEntity, new WarpTo { Destination = component.Value });
        }
    }

}
