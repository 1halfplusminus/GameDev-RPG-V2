using Unity.Entities;
using UnityEngine;
[DisableAutoCreation]
public class SkinnedMeshRendererConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Animator anim) =>
        {

            var entity = GetPrimaryEntity(anim);
            AddHybridComponent(anim);
            /* foreach (var item in anim.GetComponentsInChildren<Transform>())
            {
                DeclareDependency(item, anim);
                AddHybridComponent(item);
            } */
        });
    }
}