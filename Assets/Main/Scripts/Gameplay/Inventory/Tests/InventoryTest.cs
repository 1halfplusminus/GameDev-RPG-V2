using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;
using RPG.Gameplay.Inventory;
using Unity.Physics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.TestTools;

namespace RPG.Test
{

    public class InventorySystem : SystemBase
    {
        protected override void OnUpdate()
        {

        }
    }

    public class InventoryTest
    {

        [Test]
        public void TestInventoryItemAabb()
        {

            var inventory = new Inventory { Height = 8, Width = 8 };
            var item = new InventoryItem { Position = new float2(0, 0), Size = new int2(2, 2) };
            var itemAabb = item.GetAabb();
            var item2 = new InventoryItem { Position = new float2(1, 0), Size = new int2(1, 1) }.GetAabb();
            var item3 = new InventoryItem { Position = new float2(0.1f, 0.9f), Size = new int2(1, 1) }.GetAabb();

            Assert.IsFalse(itemAabb.Overlaps(item2));
            Assert.IsFalse(itemAabb.Overlaps(itemAabb));
            Assert.AreEqual(inventory.GetSlotForItem(item), new int[] { 0, 7, 1, 8 });
        }
        [Test]
        public void TestNeighborIndex()
        {

            var inventory = new Inventory { Height = 8, Width = 8 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150);
            using var index0Neightbor = inventoryGUI.GetNeighborsIndex(0);
            Assert.AreEqual(index0Neightbor.ToArray(), new int[] { 1, 8, 9 });
            inventoryGUI.ResizeSlot(0, 2);
            using var index0NeightborResized = inventoryGUI.GetNeighborsIndex(0);
            Assert.AreEqual(index0NeightborResized.ToArray(), new int[] { 1, 8, 9 });
            using var index11Neightbor = inventoryGUI.GetNeighborsIndex(new int2(1, 1));
            Assert.IsTrue(index11Neightbor.Length == 8);
        }
        [Test]
        public void TestIsVisible()
        {

            var inventory = new Inventory { Height = 1, Width = 3 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150f);
            var visibilities = inventoryGUI.CalculeOverlapse();
            for (int i = 0; i < visibilities.Length; i++)
            {
                Assert.IsFalse(visibilities[i]);
            }
            visibilities.Dispose();
            inventoryGUI.ResizeSlot(0, 2);
            visibilities = inventoryGUI.CalculeOverlapse();
            Assert.IsTrue(visibilities[1]);
            visibilities.Dispose();
            Assert.IsFalse(visibilities[2]);
            visibilities.Dispose();
            inventoryGUI.ResizeSlot(0, 3);
            visibilities = inventoryGUI.CalculeOverlapse();
            Assert.IsTrue(visibilities[1]);
            Assert.IsTrue(visibilities[2]);
            inventoryGUI.Dispose();
            // inventoryGUI.ResizeSlot(0, 3);
            // visibilities = inventoryGUI.CalculeOverlapse();
            // Assert.IsTrue(visibilities[1]);
            // Assert.IsTrue(visibilities[2]);
            // Assert.IsFalse(visibilities[3]);
            // visibilities.Dispose();
            // inventoryGUI.Dispose();
        }
        [Test]
        public void TestPhysicWorld()
        {

            var inventory = new Inventory { Height = 8, Width = 8 };
            var inventoryGUI = new InventoryGUI { };
            inventoryGUI.Init(inventory, 150f);
            inventoryGUI.ResizeSlot(0, 1);
            // Create the world
            // world = new PhysicsWorld(inventory.Size, 0, 0);

            // Create bodies
            // NativeArray<RigidBody> bodies = world.StaticBodies;
            // for (int i = 0; i < inventory.Size; i++)
            // {
            //     var slot = inventoryGUI.GetSlot(i);
            //     bodies[i] = new RigidBody
            //     {
            //         WorldFromBody = slot.GetRigidTransform(),
            //         Collider = slot.GetCollider(),
            //         Entity = Entity.Null,
            //         CustomTags = 0
            //     };
            // }
            var gravity = -9.81f * math.up();
            inventoryGUI.ResizeSlot(0, 2);
            // var handle = new JobHandle();
            // world.CollisionWorld.ScheduleUpdateDynamicTree(ref world, 1.0f, gravity, handle, false);
            // handle.Complete();

            // unsafe
            // {
            //     var hits = new NativeList<ColliderCastHit>(Allocator.Temp);
            //     var collidWith = world.CastCollider(new ColliderCastInput { Collider = (Unity.Physics.Collider*)inventoryGUI.GetSlot(0).GetCollider().GetUnsafePtr() }, ref hits);
            //     for (int i = 0; i < hits.Length; i++)
            //     {
            //         Debug.Log($"slot 0 collid with " + hits[i].RigidBodyIndex);
            //     }
            //     hits.Dispose();
            //     Assert.IsTrue(collidWith);
            // }
            // world.Dispose();
        }
    }

}
