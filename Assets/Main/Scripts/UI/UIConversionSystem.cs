
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace RPG.UI
{
    public struct GameUI : IComponentData
    {
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class GameUIAssetDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((GameUIAsset gameUIAsset) =>
            {
                DeclareReferencedAsset(gameUIAsset.VisualTreeAsset);
            });
        }
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class UIDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        public const string UI_ADDRESSABLE_LABEL = "GameUI";
        public const string UI_GROUP_LABEL = "GameUI";


        protected override void OnUpdate()
        {
            Entities.ForEach((GameManager gm) =>
            {
                var handle = Addressables.LoadAssetsAsync<GameUIAsset>(UI_ADDRESSABLE_LABEL, (r) =>
                {
                    DeclareReferencedAsset(r);
                });
                var handle2 = Addressables.LoadAssetsAsync<GameObject>(UI_ADDRESSABLE_LABEL, (r) =>
                {
                    if (r.GetComponent<GameUIAuthoring>() != null)
                    {
                        Debug.Log($"Declare UI For {r.name}");
                        DeclareReferencedPrefab(r);
                    }
                });
                handle.WaitForCompletion();
                handle2.WaitForCompletion();
            });
        }
    }
    public class GameUIAuthoringConversionSystem : GameObjectConversionSystem
    {

        protected override void OnUpdate()
        {
            Entities.ForEach((GameUIAuthoring go) =>
            {
                var entity = GetPrimaryEntity(go);
                DstEntityManager.AddComponent<GameUI>(entity);
            });
        }
    }

}