
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    [GenerateAuthoringComponent]
    public struct InGameUI : IComponentData { }


    public class InGameUIController : Object, IComponentData
    {
        private Label PlayerHealth;
        private Label PlayerExperiencePoint;
        private Label EnemyHealth;
        public void Init(VisualElement root)
        {
            PlayerHealth = root.Q<Label>("Health");
            EnemyHealth = root.Q<Label>("EnemyHealth");
            PlayerExperiencePoint = root.Q<Label>("Experience");
        }

        public void SetExperiencePoint(float value)
        {
            PlayerExperiencePoint.Clear();
            PlayerExperiencePoint.text = value.ToString();
        }
        public void SetPlayerHealth(Health health)
        {
            SetHealth(PlayerHealth, health);
        }

        private void SetHealth(Label label, Health health)
        {
            label.Clear();
            label.text = (health.Value / health.MaxHealth).ToString("P0");
        }

        public void SetEnemyHealth(Entity e, EntityManager em)
        {
            if (e != Entity.Null)
            {
                EnemyHealth.parent.style.display = DisplayStyle.Flex;
                var health = em.GetComponentData<Health>(e);
                SetHealth(EnemyHealth, health);
            }
            else
            {
                EnemyHealth.parent.style.display = DisplayStyle.None;
                EnemyHealth.Clear();
                EnemyHealth.text = "N/A";
            }

        }

    }

    public interface IUIController
    {
        void Init(VisualElement root);
    }

}