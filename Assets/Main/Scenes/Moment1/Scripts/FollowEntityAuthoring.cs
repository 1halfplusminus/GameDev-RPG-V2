
using System.Collections.Generic;
using RPG.Core;
using Unity.Entities;
using UnityEngine;

[System.Serializable]
public class FollowEntityAuthoring : MonoBehaviour
{
    [SerializeField]
    public int Index;

    [SerializeField]

    public int Version;

}

public class FollowEntityConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((FollowEntityAuthoring followEntityAuthoring) =>
        {
            var entity = new Entity() { Index = followEntityAuthoring.Index, Version = followEntityAuthoring.Version };
            DstEntityManager.AddComponentData(GetPrimaryEntity(followEntityAuthoring), new Follow() { Entity = entity });
            DstEntityManager.AddComponentData(GetPrimaryEntity(followEntityAuthoring), new LookAt() { Entity = entity });
        });
    }
}