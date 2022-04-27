using UnityEngine;

namespace RPG.Hybrid
{

    public class TrailRendererHybridConversionSystem : GameObjectConversionSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            this.AddTypeToCompanionWhiteList(typeof(TrailRenderer));
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((TrailRenderer trailRenderer) =>
            {
                var entity = GetPrimaryEntity(trailRenderer);
                DstEntityManager.AddComponentObject(entity,trailRenderer);
            });
        }
    }
}
