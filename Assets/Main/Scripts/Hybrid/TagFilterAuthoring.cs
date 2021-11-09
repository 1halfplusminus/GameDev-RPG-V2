using Unity.Entities;
using Unity.Collections;
using System;
using UnityEngine;

#if UNITY_EDITOR
namespace RPG.Hybrid
{

    public struct TagFilter : ISharedComponentData
    {

        public int Tag;

        public TagFilter(string tag)
        {

            Tag = 0;
        }



    }

    public class TagFilterAuthoring : MonoBehaviour
    {
        public string Tag;

    }
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameObjectNameConversionSystem))]
    public class TagFilterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((TagFilterAuthoring tagFilterAuthoring) =>
            {
                var entity = GetPrimaryEntity(tagFilterAuthoring);
                DstEntityManager.SetComponentData(entity, new DebugName { Name = tagFilterAuthoring.Tag });
                DstEntityManager.AddSharedComponentData(entity, new TagFilter(tagFilterAuthoring.Tag));
            });
        }
    }
}
#endif