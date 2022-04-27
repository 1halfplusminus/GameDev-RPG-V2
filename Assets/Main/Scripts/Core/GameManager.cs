using Unity.Entities;
using UnityEngine;

using Hash128 = Unity.Entities.Hash128;

namespace RPG.Core
{
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class GameManagerDeclareReferenceConversionSystem : GameObjectConversionSystem
    {

        protected override void OnUpdate()
        {
            Entities.ForEach((GameManager gm) =>
            {

                DeclareReferencedAsset(gm.Settings);
            });

        }
    }

    public class GameSettingsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {

            Entities.ForEach((GameSettingsAsset setting) =>
            {
                var entity = GetPrimaryEntity(setting);
                Debug.Log($"find game setting  {setting.NewGameScene} and {setting.PlayerScene}");
                DstEntityManager.AddComponentData(entity, new GameSettings
                {
                    NewGameScene = ToHash(setting.NewGameScene),
                    PlayerScene = ToHash(setting.PlayerScene),
                });
            });
        }
        private Hash128 ToHash(string value)
        {
            return new Hash128(value);
        }

    }


    public class GameManager : MonoBehaviour
    {
        public GameSettingsAsset Settings;

    }


}