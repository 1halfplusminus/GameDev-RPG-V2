using UnityEngine;

namespace RPG.Hybrid
{
    public class AudioSourceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((AudioSource audioSource) =>
            {
                AddHybridComponent(audioSource);
            });
        }
    }

}
