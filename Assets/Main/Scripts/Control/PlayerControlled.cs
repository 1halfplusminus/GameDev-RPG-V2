using RPG.Core;
using Unity.Entities;
using RPG.Combat;

namespace RPG.Control
{

    [UpdateAfter(typeof(CombatSystemGroup))]
    public class ControlSystemGroup : ComponentSystemGroup
    {

    }

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
