using Unity.Entities;


namespace RPG.Core
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    class GameManager : MonoBehaviour
    {
        public GameSettingsAsset Settings;

    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class GameManagerDeclareReferenceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((GameManager gm) =>
            {
                DeclareReferencedAsset(gm.Settings);
                DeclareAssetDependency(gm.gameObject, gm.Settings);
            });

        }
    }
    /*   [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
      public class GameSettingsDeclareReferenceConversionSystem : GameObjectConversionSystem
      {
          protected override void OnUpdate()
          {

              var guids = AssetDatabase.FindAssets($"t:{typeof(GameSettingsAsset).Name}");
              foreach (string guid in guids)
              {
                  var path = AssetDatabase.GUIDToAssetPath(guid);
                  var gameSetting = AssetDatabase.LoadAssetAtPath<GameSettingsAsset>(path);
                  Debug.Log($"declare referenced asset find game setting {guid} {path}");
                  DeclareReferencedAsset(gameSetting);
                  break;
              }
          }
      } */
    public class GameSettingsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((GameSettingsAsset setting) =>
            {
                var entity = GetPrimaryEntity(setting);
                Debug.Log($"find game setting");
                DstEntityManager.AddComponentData(entity, new GameSettings
                {
                    NewGameScene = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(setting.NewGameScene)))
                });
            });
        }
    }

#endif
    public struct GameSettings : IComponentData
    {
        public Unity.Entities.Hash128 NewGameScene;
    }
}