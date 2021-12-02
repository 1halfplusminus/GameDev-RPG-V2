
#if UNITY_EDITOR
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace RPG.Core
{

    public class TriggerSceneLoadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        SceneAsset SceneAsset;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (SceneAsset != null)
            {
                dstManager.AddComponentData(entity, new TriggerSceneLoad { SceneGUID = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(SceneAsset))) });
            }

        }
    }
}

#endif

