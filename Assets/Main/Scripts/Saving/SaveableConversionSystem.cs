using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using UnityEngine.Playables;

namespace RPG.Saving
{
    public class SaveableConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithNone<PlayableDirector>().ForEach((Transform transform) =>
            {
                var hash = new UnityEngine.Hash128();
                hash.Append(transform.gameObject.GetInstanceID());
                AddHashComponent(transform, hash);
            });
            Entities.ForEach((PlayableDirector director) =>
            {
                var hash = new UnityEngine.Hash128();
                hash.Append(director.playableAsset.GetInstanceID());
                AddHashComponent(director, hash);
            });
        }

        private void AddHashComponent(Component transform, Hash128 hash)
        {
            var entity = TryGetPrimaryEntity(transform);
            if (entity != Entity.Null && !DstEntityManager.HasComponent<Identifier>(entity))
            {

                var identifier = new Identifier { Id = hash };
                DstEntityManager.AddComponentData<Identifier>(entity, identifier);
            }
        }
    }
}
