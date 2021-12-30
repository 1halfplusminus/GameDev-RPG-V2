using Unity.Entities;
using RPG.Core;
using RPG.Stats;
using Unity.Mathematics;
namespace RPG.Gameplay
{
    [GenerateAuthoringComponent]
    public struct RestaureHealthPercent : IComponentData
    {
        public float Value;

        public float GetNewHealth(Health currentHealth, BaseStats baseStats)
        {
            var newHealth = baseStats.ProgressionAsset.Value.GetStat(Stats.Stats.Health, baseStats.Level);
            var regen = newHealth * (Value / 100);
            return math.max(currentHealth.Value, regen);
        }
    }

}
