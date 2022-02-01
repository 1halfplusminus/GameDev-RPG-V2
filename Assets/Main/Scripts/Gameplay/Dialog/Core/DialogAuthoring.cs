using Unity.Entities;
using UnityEngine;

namespace RPG.Gameplay
{
    public struct DialogAsset : IComponentData
    {
        public Entity Value;
    }
    public class DialogAuthoring : MonoBehaviour
    {
        public DialogGraph DialogAsset;

        public GameObject InteractionUIPrefab;
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class DialogGraphDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DialogAuthoring dialogAuthoring) =>
            {
                DeclareReferencedAsset(dialogAuthoring.DialogAsset);
                DeclareReferencedPrefab(dialogAuthoring.InteractionUIPrefab);
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
                BlobAssetStore.AddUniqueBlobAsset(ref blobDialog);
                DstEntityManager.AddComponentData(entity, new Dialog { Reference = blobDialog });
                DstEntityManager.AddComponent<Prefab>(entity);
            });

            Entities.ForEach((DialogAuthoring dialogAuthoring) =>
            {
                var dialogEntity = GetPrimaryEntity(dialogAuthoring.DialogAsset);
                var dialogComponent = DstEntityManager.GetComponentData<Dialog>(dialogEntity);
                var entity = GetPrimaryEntity(dialogAuthoring);
                DstEntityManager.AddComponentData(entity, new DialogAsset { Value = dialogEntity });
                DstEntityManager.AddComponent<GameplayInput>(entity);
            });
        }
    }


}