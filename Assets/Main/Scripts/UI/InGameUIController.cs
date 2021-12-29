
using RPG.Core;
using RPG.Stats;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public class InGameUIController : Object, IComponentData
    {
        private Label PlayerHealth;
        private Label PlayerExperiencePoint;
        private Label EnemyHealth;
        private Label Level;

        private VisualElement EnemyContainer;
        public void Init(VisualElement root)
        {
            PlayerHealth = root.Q<Label>("Health");
            EnemyHealth = root.Q<Label>("EnemyHealth");
            PlayerExperiencePoint = root.Q<Label>("Experience");
            Level = root.Q<Label>("Level");
            EnemyContainer = root.Q<VisualElement>("EnemyContainer");
        }
        public void SetLevel(BaseStats baseStats)
        {
            Level.Clear();
            Level.text = baseStats.Level.ToString();
        }
        public void SetExperiencePoint(float value)
        {
            PlayerExperiencePoint.Clear();
            PlayerExperiencePoint.text = value.ToString("F0");
        }
        public void SetPlayerHealth(Health health, int level, BlobAssetReference<Progression> progressionAsset)
        {
            SetHealth(PlayerHealth, health, level, progressionAsset);
        }

        private void SetHealth(Label label, Health health, int level, BlobAssetReference<Progression> progressionAsset)
        {
            label.Clear();
            label.text = health.GetPercent(level, progressionAsset).ToString("P0");
        }

        public void SetEnemyHealth(Entity e, EntityManager em)
        {
            if (e != Entity.Null)
            {
                EnemyContainer.style.display = DisplayStyle.Flex;
                var health = em.GetComponentData<Health>(e);
                var baseStats = em.GetComponentData<BaseStats>(e);
                SetHealth(EnemyHealth, health, baseStats.Level, baseStats.ProgressionAsset);
            }
            else
            {
                EnemyContainer.style.display = DisplayStyle.None;
                EnemyHealth.Clear();
                EnemyHealth.text = "N/A";
            }

        }

    }

}