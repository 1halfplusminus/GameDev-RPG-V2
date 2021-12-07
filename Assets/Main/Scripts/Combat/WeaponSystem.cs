using Unity.Entities;
using RPG.Core;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;
using RPG.Animation;
using Unity.Rendering;

namespace RPG.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class FighterEquipWeaponSystem : SystemBase
    {

        EntityQuery fighterEquipWeaponQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(fighterEquipWeaponQuery);
        }

        protected override void OnUpdate()
        {
            var cbp = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var weapons = GetComponentDataFromEntity<Weapon>(true);
            Entities
            .WithReadOnly(weapons)
            .WithStoreEntityQueryInField(ref fighterEquipWeaponQuery)
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
                        hitEvents.Add(new HitEvent { Time = weaponHitEvents[i], Equipped = equiped.Entity });
                    }
                }
            }).ScheduleParallel();
            EntityManager.RemoveComponent<Equipped>(fighterEquipWeaponQuery);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
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
                if (spawn.Projectile != Entity.Null)
                {
                    cbp.AddComponent(entityInQueryIndex, equipedBy.Entity, new ShootProjectile() { Prefab = spawn.Projectile });
                }
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
            .ForEach((int entityInQueryIndex, Entity e, in EquipInSocket equipWeapon, in EquippedPrefab prefab, in ChangeAttackAnimation changeAttackAnimation, in ShootProjectile shootProjectile) =>
            {
                Debug.Log($"Equip {e.Index} in socket : ${equipWeapon.Socket.Index}");
                cbp.AddComponent(entityInQueryIndex, equipWeapon.Socket, new SpawnWeapon { Prefab = prefab.Value, Animation = changeAttackAnimation.Animation, Weapon = e, Projectile = shootProjectile.Prefab });
                cbp.RemoveComponent<EquipInSocket>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class CollidWithPickableWeaponSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery collidWithPickableweaponQuery;

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
            .WithNone<Picked>()
            .WithStoreEntityQueryInField(ref collidWithPickableweaponQuery)
            .WithChangeFilter<CollidWithPlayer>()
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer collidWithPlayer, in PickableWeapon picked) =>
            {
                Debug.Log($" {e.Index} collid with player {collidWithPlayer.Entity.Index} and pickup Weapon: ${picked.Entity.Index}");
                cbp.AddComponent(entityInQueryIndex, collidWithPlayer.Entity, new Equip { Equipable = picked.Entity, SocketType = picked.SocketType });
                cbp.AddComponent<Picked>(entityInQueryIndex, e);
            }).ScheduleParallel();

            Entities
            .WithAll<Picked, RenderMesh, StatefulTriggerEvent>()
            .ForEach((int entityInQueryIndex, Entity e, in CollidWithPlayer player, in PickableWeapon picked) =>
            {
                Debug.Log($"{e.Index} was picked");
                cbp.RemoveComponent<RenderMesh>(entityInQueryIndex, e);
                cbp.RemoveComponent<StatefulTriggerEvent>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }

    [UpdateInGroup(typeof(CombatSystemGroup))]
    public class EquipPickedWeaponSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery fighterEquipQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(fighterEquipQuery);
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .WithStoreEntityQueryInField(ref fighterEquipQuery)
            .ForEach((int entityInQueryIndex, Entity e, in Equip picked, in EquipableSockets sockets) =>
            {
                var socket = sockets.GetSocketForType(picked.SocketType);
                Debug.Log($"Player {e.Index} equip pickup Weapon: ${picked.Equipable.Index} in socket: {socket.Index}");
                cbp.AddComponent(entityInQueryIndex, picked.Equipable, new EquipInSocket { Socket = socket });
                cbp.RemoveComponent<Equip>(entityInQueryIndex, e);
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }
}
