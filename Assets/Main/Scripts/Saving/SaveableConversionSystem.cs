using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using UnityEngine.Playables;
using RPG.Control;
using RPG.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RPG.Saving
{
    public struct SpawnIdentifier : IComponentData
    {
        public Hash128 Id;
    }
    public class SaveableConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {

            Entities.ForEach((PlayableDirector director) =>
            {
                var hash = new UnityEngine.Hash128();
#if UNITY_EDITOR
                hash.Append(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(director.playableAsset)).ToString());
#endif

                AddHashComponent(director, hash);
            });
            Entities
            .WithNone<PlayableDirector>().ForEach((PlayerSpawner playerSpawner) =>
            {
                var hash = new UnityEngine.Hash128();
                hash.Append(playerSpawner.gameObject.GetInstanceID());
                var entity = TryGetPrimaryEntity(playerSpawner);
                if (entity != Entity.Null)
                {

                    var identifier = new SpawnIdentifier { Id = hash };
                    DstEntityManager.AddComponentData(entity, identifier);
                }
            });

            Entities
            .WithAny<PlayerControlledAuthoring, GuardLocationAuthoring>()
            .WithNone<PlayableDirector, PlayerSpawner>().ForEach((Transform transform) =>
            {
                var hash = new UnityEngine.Hash128();
                hash.Append(transform.gameObject.GetInstanceID());
                AddHashComponent(transform, hash);
            });

        }

        private void AddHashComponent(Component transform, Hash128 hash)
        {
            var entity = TryGetPrimaryEntity(transform);
            if (entity != Entity.Null && !DstEntityManager.HasComponent<Identifier>(entity) && !DstEntityManager.HasComponent<Prefab>(entity))
            {

                var identifier = new Identifier { Id = hash };
                DstEntityManager.AddComponentData<Identifier>(entity, identifier);
            }
        }
    }
}
