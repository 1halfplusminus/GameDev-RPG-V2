
using Cinemachine;
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;

[GenerateAuthoringComponent]
public class MomentOneCinematicOne : IComponentData
{

}


public class MomentOneCinematic : SystemBase
{
    protected override void OnUpdate()
    {
        var em = EntityManager;
        Entities.WithChangeFilter<MomentOneCinematicOne>()
        .WithAll<MomentOneCinematicOne>()
        .ForEach((PlayableDirector director) =>
        {
            Debug.Log("Looking for moment 1");
            var cameraEntity = GetSingletonEntity<ThirdPersonCamera>();
            var camera = em.GetComponentObject<CinemachineVirtualCamera>(cameraEntity);
            // FIXME: Pass this in editor
            director.SetReferenceValue("7f3ccff41bccc1aa08eb0611c8c09131", camera);
            director.SetReferenceValue("798a713441ed787dc9f34356489f1a81", camera);
        }).WithoutBurst().Run();

    }
}

