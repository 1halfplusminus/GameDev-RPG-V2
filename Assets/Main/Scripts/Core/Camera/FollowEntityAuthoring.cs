using RPG.Core;
using UnityEngine;
using RPG.Hybrid;
using Unity.Entities;
using System.Collections.Generic;


#if UNITY_EDITOR
public class FollowEntityAuthoring : MonoBehaviour
{


    [SerializeField]
    public string Tag;


}
[DisableAutoCreation]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class FollowEntityConversionSystem : GameObjectConversionSystem
{
    Dictionary<string, Entity> tags;
    protected override void OnCreate()
    {
        base.OnCreate();
        if (tags == null)
        {
            tags = new Dictionary<string, Entity>();
        }

        Debug.Log("On Create " + tags.Count);
    }

    protected override void OnUpdate()
    {

        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly(typeof(TagFilter)));
        Debug.Log("Query Count =" + query.CalculateEntityCount());
        Entities.ForEach((TagFilterAuthoring tagFilterAuthoring) =>
        {
            tags.Add(tagFilterAuthoring.Tag.Trim(), GetPrimaryEntity(tagFilterAuthoring));
            Debug.Log("Tag Filter authoring Tag " + tagFilterAuthoring.Tag);
            EntityManager.AddSharedComponentData(GetPrimaryEntity(tagFilterAuthoring), new TagFilter());
        });
        Entities.ForEach((FollowEntityAuthoring followEntityAuthoring) =>
        {
            var tag = followEntityAuthoring.Tag;
            Debug.Log(" Follow Entity Tag " + tag);
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly(typeof(TagFilterAuthoring)));
            var tagFilter = new TagFilter(tag);
            if (tags.ContainsKey(followEntityAuthoring.Tag.Trim()))
            {
                Debug.Log("Here" + query.CalculateEntityCount());
                var entity = tags[followEntityAuthoring.Tag.Trim()];
                DstEntityManager.AddComponentData(GetPrimaryEntity(followEntityAuthoring), new Follow() { Entity = entity });
                DstEntityManager.AddComponentData(GetPrimaryEntity(followEntityAuthoring), new LookAt() { Entity = entity });
            }
            else
            {
                Debug.Log("No Entity Found With Tag " + tagFilter.Tag.ToString() + " count = " + query.CalculateEntityCount());
            }

        });
    }
}
#endif