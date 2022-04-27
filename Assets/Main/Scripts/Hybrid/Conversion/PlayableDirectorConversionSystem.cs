using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using RPG.Hybrid;
using Cinemachine;

public struct LinkCinemachineBrain : IComponentData { }

[UpdateAfter(typeof(CinemachineCameraConversionSystem))]
public class PlayableDirectorConversionSystem : GameObjectConversionSystem
{
    HashSet<PlayableAsset> dones;
    EntityQuery query;
    protected override void OnCreate()
    {
        base.OnCreate();
        this.AddTypeToCompanionWhiteList(typeof(PlayableDirector));
        dones = new HashSet<PlayableAsset>();
        query = EntityManager.CreateEntityQuery(typeof(CinemachineBrain));
    }
    protected override void OnUpdate()
    {
        dones.Clear();
        CinemachineBrain brain = null;
        if (query.CalculateEntityCount() > 0)
        {
            Entity brainEntity = query.GetSingletonEntity();
            brain = EntityManager.GetComponentObject<CinemachineBrain>(brainEntity);
        }

        Entities.ForEach((PlayableDirector playableDirector) =>
        {
            if (playableDirector.playableAsset == null) { return; }
            var entity = GetPrimaryEntity(playableDirector);
            DstEntityManager.AddComponentObject(entity,playableDirector);
            LinkBrain(playableDirector, brain, entity);
            ConvertOutputRecurse(playableDirector, playableDirector.playableAsset);
        });

        /* Entities.ForEach((CinemachineTrack track) =>
        {
            Debug.Log("Convert CinemachineTrack");
        }); */
    }
    private void LinkBrain(PlayableDirector playableDirector, CinemachineBrain brain, Entity entity)
    {
        var outputs = playableDirector.playableAsset.outputs;
        foreach (var output in outputs)
        {
            if (output.sourceObject is CinemachineTrack)
            {
                if (brain != null)
                {
                    Debug.Log($"Set Generic Binding");
                    playableDirector.SetGenericBinding(output.sourceObject, brain);
                }
                else
                {
                    DstEntityManager.AddComponent<LinkCinemachineBrain>(entity);
                    break;
                }
            }
        }
    }
    private void ConvertOutputRecurse(PlayableDirector playableDirector, PlayableAsset asset)
    {
        if (dones.Contains(asset))
        {
            return;
        }
        dones.Add(asset);
        var outputs = asset.outputs;
        foreach (var output in outputs)
        {
            if (output.sourceObject is PlayableAsset playableAsset)
            {
                if (HasPrimaryEntity(playableAsset))
                {
                    var outputEntity = GetPrimaryEntity(playableAsset);
                    DstEntityManager.AddComponentObject(outputEntity, playableAsset);
                    ConvertOutputRecurse(playableDirector, playableAsset);
                }
            }
        }
    }
}
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class InitialiseHybridPayableDirectory : SystemBase
{
    protected override void OnUpdate()
    {
        if (!HasSingleton<CinemachineBrainTag>()) { return; }
        var cinemachineBrainEntity = GetSingletonEntity<CinemachineBrainTag>();
        var cinemachineBrain = EntityManager.GetComponentObject<CinemachineBrain>(cinemachineBrainEntity);
        Entities
        .WithChangeFilter<LinkCinemachineBrain>()
        .ForEach((PlayableDirector playableDirector) =>
        {
            var outputs = playableDirector.playableAsset.outputs;
            foreach (var output in outputs)
            {
                if (output.sourceObject is CinemachineTrack cinemachineTrack)
                {
                    Debug.Log("Link cinemachineBrain");
                    playableDirector.SetGenericBinding(output.sourceObject, cinemachineBrain);
                }
            }
        }).WithoutBurst().Run();
    }
}