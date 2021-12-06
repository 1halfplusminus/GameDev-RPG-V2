using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;
using RPG.Animation;


namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class FighterEquipWeaponSystem : SystemBase
    {

        EntityQuery fighterEquipWeaponQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate(fighterEquipWeaponQuery);
        }

        protected override void OnUpdate()
        {
            var weapons = GetComponentDataFromEntity<Weapon>(true);
            Entities
            .WithReadOnly(weapons)
            .WithStoreEntityQueryInField(ref fighterEquipWeaponQuery)
            .WithChangeFilter<Equipped>()
            .ForEach((Entity e, ref Fighter fighter, ref DynamicBuffer<HitEvent> hitEvents, in Equipped equiped) =>
            {
                if (weapons.HasComponent(equiped.Entity))
                {
                    Debug.Log($"Fighter {e.Index} equip weapon {equiped.Entity}");
                    var weapon = weapons[equiped.Entity];
                    var weaponHitEvents = weapon.HitEvents;
                    fighter.AttackDuration = weapon.AttackDuration;
                    fighter.Cooldown = weapon.Cooldown;
                    fighter.Damage = weapon.Damage;
                    fighter.Range = weapon.Range;
                    hitEvents.Clear();
                    hitEvents.Capacity = weaponHitEvents.Length;
                    for (int i = 0; i < weaponHitEvents.Length; i++)
                    {
                        hitEvents.Add(new HitEvent { Time = weaponHitEvents[i] });
                    }
                }
            }).ScheduleParallel();
            EntityManager.RemoveComponent<Equipped>(fighterEquipWeaponQuery);
        }

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class SpawnWeaponSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery spawnWeaponQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(spawnWeaponQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref spawnWeaponQuery)
            .ForEach((int entityInQueryIndex, Entity e, in SpawnWeapon spawn, in LocalToWorld localToWorld, in EquipedBy equipedBy) =>
            {
                Debug.Log($"spawn weapon prefab {spawn.Prefab.Index} at : ${localToWorld.Position}");
                if (spawn.Prefab != Entity.Null)
                {
                    cbp.AddComponent(entityInQueryIndex, e, new Spawn() { Prefab = spawn.Prefab, Parent = e });
                }
                cbp.AddComponent(entityInQueryIndex, equipedBy.Entity, new Equipped { Entity = spawn.Weapon });
                cbp.AddComponent(entityInQueryIndex, equipedBy.Entity, new ChangeAttackAnimation() { Animation = spawn.Animation });
                cbp.RemoveComponent<SpawnWeapon>(entityInQueryIndex, e);
            }).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class EquipWeaponInSocketSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery equipPrefabInSocketQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(equipPrefabInSocketQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref equipPrefabInSocketQuery)
            .WithChangeFilter<EquipInSocket>()
            .ForEach((int entityInQueryIndex, Entity e, in EquipInSocket equipWeapon, in EquippedPrefab prefab, in ChangeAttackAnimation changeAttackAnimation) =>
            {
                Debug.Log($"Equip {e.Index} in socket : ${equipWeapon.Socket}");
                cbp.AddComponent(entityInQueryIndex, equipWeapon.Socket, new SpawnWeapon { Prefab = prefab.Value, Animation = changeAttackAnimation.Animation, Weapon = e });
                cbp.RemoveComponent<EquipInSocket>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}
