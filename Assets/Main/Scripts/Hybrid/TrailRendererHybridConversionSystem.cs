using UnityEngine;

namespace RPG.Hybrid
{

    public class TrailRendererHybridConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((TrailRenderer trailRenderer) =>
            {
                AddHybridComponent(trailRenderer);
            });
        }
    }
}
