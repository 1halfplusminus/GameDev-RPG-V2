
  
// Runtime component, SceneSystem uses Entities.Hash128 to identify scenes.
using Unity.Entities;

using UnityEngine;
using Unity.Scenes;
using Unity.Collections;

    // Authoring component, a SceneAsset can only be used in the Editor
    public class SceneLoaderAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string GUID = "c3fbcc17b9edfe0dfb90df2ccb8b2e54";
        public void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var hash = new Unity.Entities.Hash128(GUID);
            dstManager.AddComponentData(entity, new SceneLoader {Guid =  hash });
        }
    }

public struct SceneLoader : IComponentData
{
    public Unity.Entities.Hash128 Guid;
}

public partial class SceneLoaderSystem : SystemBase
{
    private SceneSystem m_SceneSystem;
    private EntityQuery m_NewRequests;

    protected override void OnCreate()
    {
        m_SceneSystem = World.GetExistingSystem<SceneSystem>();
        m_NewRequests = GetEntityQuery(typeof(SceneLoader));
    }

    protected override void OnUpdate()
    {
        var requests = m_NewRequests.ToComponentDataArray<SceneLoader>(Allocator.Temp);

        for (int i = 0; i < requests.Length; i += 1)
        {
            m_SceneSystem.LoadSceneAsync(requests[i].Guid, new SceneSystem.LoadParameters{Flags = SceneLoadFlags.LoadAsGOScene });
        }

        requests.Dispose();
        EntityManager.DestroyEntity(m_NewRequests);
    }
}