
using RPG.Combat;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace RPG.UI
{
    public struct DamageText : IComponentData
    {
        public Entity Prefab;
    }
    public struct DisplayDamage : IComponentData
    {
        public float Value;
    }
    public class DamageTextAuthoring : MonoBehaviour
    {
        public GameObject DamageTextPrefab;
    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class DamageTextDeclareReferenceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DamageTextAuthoring damageTextAuthoring) =>
            {
                DeclareReferencedPrefab(damageTextAuthoring.DamageTextPrefab);
            });
        }
    }

    public class DamageTextConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DamageTextAuthoring damageTextAuthoring) =>
            {
                var entity = GetPrimaryEntity(damageTextAuthoring);
                var prefabEntity = GetPrimaryEntity(damageTextAuthoring.DamageTextPrefab);
                DstEntityManager.AddComponentData(entity, new DamageText { Prefab = prefabEntity });
            });
        }
    }

    [UpdateInGroup(typeof(UISystemGroup))]
    [DisableAutoCreation]
    public partial class DamageTextSystem : SystemBase
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
            .WithNone<NoDamage>()
            .ForEach((in Hit hit) =>
            {
                if (hit.Damage > 0)
                {
                    if (HasComponent<DamageText>(hit.Hitted))
                    {
                        var localToWorld = GetComponent<LocalToWorld>(hit.Hitted);
                        var damageText = GetComponent<DamageText>(hit.Hitted);
                        var instance = cb.Instantiate(damageText.Prefab);
                        cb.AddComponent(instance, new DisplayDamage { Value = hit.Damage });
                        cb.AddComponent(instance, new Parent { Value = hit.Hitted });
                        cb.AddComponent<LocalToWorld>(instance, localToWorld);
                        cb.AddComponent<LocalToParent>(instance);
                    }
                }
            })
            // .WithoutBurst()
            .Run();

            Entities
            .ForEach((int entityInQueryIndex, in DisplayDamage displayDamage, in DynamicBuffer<Child> children) =>
            {
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children[i].Value;
                    cbp.AddComponent(entityInQueryIndex, child, displayDamage);
                }
            })
            .ScheduleParallel();

            Entities
            .ForEach((TextMesh text, in DisplayDamage displayDamage) =>
            {
                // Debug.Log($"Display damage {displayDamage.Value}");
                text.text = $"{displayDamage.Value:F0}";
            })
            .WithoutBurst()
            .Run();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}