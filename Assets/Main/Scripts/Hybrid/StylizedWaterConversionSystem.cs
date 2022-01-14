
namespace RPG.Hybrid
{
    public class StylizedWaterConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((StylizedWater.StylizedWaterURP stylizedWater) =>
            {
                AddHybridComponent(stylizedWater);
            });
        }
    }

}