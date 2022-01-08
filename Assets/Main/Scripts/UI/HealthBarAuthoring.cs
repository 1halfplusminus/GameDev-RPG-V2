using RPG.Combat;
using RPG.Core;
using RPG.Stats;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public class HealthBarController : Object, IComponentData
    {
        VisualElement container;
        VisualElement overlay;
        public void Init(VisualElement root)
        {
            overlay = root.Q<VisualElement>("Overlay");
            container = root.Q<VisualElement>("Container");
            container.style.visibility = Visibility.Hidden;
        }

        public void SetHealh(Health health, BaseStats stats)
        {
            var ratio = health.GetRatio(stats.Level, stats.ProgressionAsset);
            container.style.visibility = Mathf.Approximately(ratio, 0) ? Visibility.Hidden : Visibility.Visible;
            var styleLength = new StyleLength(new Length(ratio * 100, LengthUnit.Percent));
            overlay.style.width = styleLength;
        }

        public void SetPosition(Camera camera, Vector3 position)
        {

            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                   container.panel, position, camera);
            newPosition.x = newPosition.x - container.layout.width / 2f;
            container.transform.position = newPosition;
        }
    }
    public class HealthBarAuthoring : MonoBehaviour
    {
        public GameObject HealthBarPrefab;
        public Vector3 Offset;

        void OnDrawGizmosSelected()
        {
            // Draw a semitransparent blue cube at the transforms position
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(transform.position + Offset, new Vector3(1, 0.2f, 0.1f));
        }
    }

    public struct HealthBar : IComponentData
    {
        public Entity Prefab;
        public float3 Offset;

    }
    public struct HealthBarInstance : IComponentData
    {

        public Entity Value;
    }
    public struct HealthBarOwner : IComponentData
    {

        public Entity Value;
    }

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class HealthBarDeclareReferenceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((HealthBarAuthoring healthBarAuthoring) =>
            {
                DeclareReferencedPrefab(healthBarAuthoring.HealthBarPrefab);
            });
        }
    }

    public class HealthBarConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((HealthBarAuthoring healthBarAuthoring) =>
            {
                var entity = GetPrimaryEntity(healthBarAuthoring);
                var prefabEntity = GetPrimaryEntity(healthBarAuthoring.HealthBarPrefab);
                DstEntityManager.AddComponentData(entity, new HealthBar { Prefab = prefabEntity, Offset = healthBarAuthoring.Offset });
            });
        }
    }

    public class HealthBarUISystem : SystemBase
    {

        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();

            Entities
            .WithNone<HealthBarInstance, IsDeadTag>()
            .WithAny<WasHitted, IsFighting>()
            .ForEach((Entity e, in Health health, in HealthBar healthBar) =>
            {
                var instance = cb.Instantiate(healthBar.Prefab);
                cb.AddComponent(e, new HealthBarInstance { Value = instance });
                cb.AddComponent(instance, new HealthBarOwner { Value = e });
            }).Schedule();

            Entities
            .WithNone<HealthBarController>()
            .WithAll<HealthBarUI, UIReady>()
            .ForEach((Entity e, UIDocument uiDocument, in HealthBarOwner owner) =>
            {
                var controller = new HealthBarController();
                controller.Init(uiDocument.rootVisualElement);
                EntityManager.AddComponentObject(e, controller);
                EntityManager.AddComponentObject(owner.Value, controller);

            }).WithStructuralChanges().WithoutBurst().Run();

            Entities
            .ForEach((Entity e, HealthBarController controller, in Health health, in BaseStats baseStats, in Translation translation, in HealthBar healthBar) =>
            {
                controller.SetHealh(health, baseStats);
                controller.SetPosition(Camera.main, translation.Value + healthBar.Offset);
            }).WithoutBurst().Run();

            Entities
            .WithAll<HealthBarInstance, IsDeadTag>()
            .ForEach((Entity e, HealthBarInstance healthBarInstance) =>
            {
                cb.DestroyEntity(healthBarInstance.Value);
                cb.RemoveComponent<HealthBarInstance>(e);
                cb.RemoveComponent<HealthBarController>(e);
            }).Schedule();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}