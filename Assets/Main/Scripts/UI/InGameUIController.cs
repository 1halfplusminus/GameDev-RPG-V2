
using RPG.Core;
using RPG.Mouvement;
using RPG.Stats;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static ExtensionMethods.EcsConversionExtension;
namespace RPG.UI
{
    public class InGameUIController : Object, IComponentData
    {
        private Label PlayerHealth;
        private Label PlayerExperiencePoint;
        private Label EnemyHealth;
        private Label Level;

        private Button Inventory;
        private Button Setting;
        private VisualElement EnemyContainer;
        public bool InventoryClicked;
        public bool SettingClicked;
        private Joystick Joystick;
        public void Init(VisualElement root)
        {
            PlayerHealth = root.Q<Label>("Health");
            EnemyHealth = root.Q<Label>("EnemyHealth");
            PlayerExperiencePoint = root.Q<Label>("Experience");
            Level = root.Q<Label>("Level");
            EnemyContainer = root.Q<VisualElement>("EnemyContainer");
            Inventory = root.Q<Button>("InventoryButton");
            Joystick = root.Q<Joystick>();
            Setting = root.Q<Button>("SettingButton");
            Setting.clicked += () =>
            {
                SettingClicked = true;
            };
            Inventory.clicked += () =>
            {
                InventoryClicked = true;
            };
        }
        public void ProcessMouvement(ref MoveTo moveTo, ref Translation translation, in DeltaTime deltaTime, in Mouvement.Mouvement mouvement)
        {
            Debug.Log($"Move To {Joystick.Mouvement}");
            if (!Joystick.Mouvement.Equals(float2.zero))
            {

                var right = math.normalize(Camera.main.transform.right);
                var forward = math.normalize(Camera.main.transform.forward);
                var direction = (-Joystick.Mouvement.x * right) + (Joystick.Mouvement.y * forward);
                moveTo.Direction = direction;
                moveTo.UseDirection = true;
                moveTo.SpeedPercent = 1f;
                moveTo.Stopped = false;
            }
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
            label.text = $"{health.Value:F0} / {progressionAsset.Value.GetStat(Stats.Stats.Health, level):F0}";
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