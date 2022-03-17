using Unity.Collections.LowLevel.Unsafe;
using RPG.Combat;
using RPG.Control;
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

        private Button AttackClosestTargetButton;
        private bool AttackClosestTargetClicked;
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
            AttackClosestTargetButton = root.Q<Button>("ActionButton");
            AttackClosestTargetButton.clicked += () =>
            {
                AttackClosestTargetClicked = true;
            };
            Setting.clicked += () =>
            {
                SettingClicked = true;
            };
            Inventory.clicked += () =>
            {
                InventoryClicked = true;
            };
        }
        public void ProcessAtackButton(EntityCommandBuffer cb, Entity e)
        {
            if (AttackClosestTargetClicked)
            {
                cb.AddComponent<AttackClosestTarget>(e);
                AttackClosestTargetClicked = false;
            }
        }

        public unsafe void ProcessMouvement(ref MoveTo moveTo, ref Fighter fighter, LookAt lookAt, Entity e, EntityCommandBuffer commandBuffer)
        {
            if (!Joystick.Mouvement.Equals(float2.zero))
            {

                var vec = Camera.main.transform.rotation.eulerAngles;
                var y = Mathf.Round(vec.y / 90) * 90;
                var direction = new float3(-Joystick.Mouvement.x, 0f, Joystick.Mouvement.y);
                moveTo.Direction = math.rotate(Quaternion.Euler(0, y, 0), direction);
                moveTo.UseDirection = true;
                moveTo.SpeedPercent = 1f;
                moveTo.Stopped = false;
                fighter.MoveTowardTarget = false;
                fighter.Target = Entity.Null;
                lookAt.Entity = Entity.Null;
                commandBuffer.AddComponent(e, lookAt);
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