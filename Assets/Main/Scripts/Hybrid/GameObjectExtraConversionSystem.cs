
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
namespace RPG.Hybrid
{
#if UNITY_EDITOR
    public struct DebugName : IComponentData
    {
        public FixedString4096 Name;
    }
    public class GameObjectNameConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Transform t) =>
            {
                var entity = GetPrimaryEntity(t);
                DstEntityManager.AddComponentData(entity, new DebugName { Name = t.gameObject.name });
            });
        }
    }
#endif
}