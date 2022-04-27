using Unity.Entities;
using UnityEngine;

public static class AudioConversionUtilities {
    public static Entity DeclareAudioSource(AudioSource audioSource, GameObjectConversionSystem conversionSystem){
        conversionSystem.AddTypeToCompanionWhiteList(typeof(AudioSource));
        var entity = conversionSystem.GetPrimaryEntity(audioSource);
        conversionSystem.DstEntityManager.AddComponentObject(entity,audioSource);
        conversionSystem.DeclareAssetDependency(audioSource.gameObject, audioSource.clip);
        return entity;
    }
}