using RPG.Core;
using Unity.Entities;

namespace RPG.Control
{

    public struct PlayerControlled : IComponentData { }



    public class PlayerControlledConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerControlledAuthoring playerControlled) =>
            {
                var entity = GetPrimaryEntity(playerControlled);
                DstEntityManager.AddComponent<PlayerControlled>(entity);
                DstEntityManager.AddComponentData(entity, new Raycast { Distance = playerControlled.RaycastDistance });
                DstEntityManager.AddComponent<MouseClick>(entity);
            });
        }
    }

}
