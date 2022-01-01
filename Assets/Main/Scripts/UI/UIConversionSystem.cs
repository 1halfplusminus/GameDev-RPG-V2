
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
    public class UIDeclareReferencedObjectsConversionSystem : GameObjectConversionSystem
    {
        public const string UI_ADDRESSABLE_LABEL = "InGameUI";
        public const string UI_GROUP_LABEL = "Game UI";


        protected override void OnUpdate()
        {
            Entities.ForEach((GameManager gm) =>
            {
                var handle = Addressables.LoadAssetsAsync<GameObject>(UI_ADDRESSABLE_LABEL, (r) =>
                {
                });
                handle.WaitForCompletion();
                foreach (var r in handle.Result)
                {
                    if (r && r.GetComponent<GameUIAuthoring>() != null)
                    {
                        Debug.Log($"Declare UI For {r.name}");
                        DeclareReferencedPrefab(r);
                    }
                }

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