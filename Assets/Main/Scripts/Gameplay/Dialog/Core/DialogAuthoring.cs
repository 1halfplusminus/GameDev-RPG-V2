using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    public class DialogAuthoring : MonoBehaviour
    {
        public DialogGraph DialogAsset;
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class DialogGraphDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DialogAuthoring dialogAuthoring) =>
            {
                DeclareReferencedAsset(dialogAuthoring.DialogAsset);
            });
        }
    }
    public class DialogGraphConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DialogGraph dialogGraph) =>
            {
                var entity = GetPrimaryEntity(dialogGraph);
                var blobDialog = BlobAssetStore.GetDialog(dialogGraph);
                DstEntityManager.AddComponentData(entity, new Dialog { Reference = blobDialog });
            });

            Entities.ForEach((DialogAuthoring dialogAuthoring) =>
            {
                var dialogEntity = GetPrimaryEntity(dialogAuthoring.DialogAsset);
                var dialogComponent = DstEntityManager.GetComponentData<Dialog>(dialogEntity);
                var entity = GetPrimaryEntity(dialogAuthoring);
                DstEntityManager.AddComponentData(entity, dialogComponent);
            });
        }
    }


}