using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RPG.Gameplay;
using Unity.Entities;
using RPG.UI;
using UnityEngine.UIElements;
using Unity.Physics;
using Unity.Mathematics;

namespace RPG.Test
{
    public class InventoryTest
    {
        public struct Item
        {
            public float2 Size;
            public float2 Position;

            public Aabb GetAabb()
            {
                var min = new float3(Position.x, Position.y, 0f);
                var max = new float3(Position.x + Size.x, Position.y + Size.y, 0f);
                var aabb = new Aabb { Min = min + math.EPSILON * math.sign(min), Max = max - math.EPSILON * math.sign(max) };
                return aabb;
            }

        }
        // A Test behaves as an ordinary method
        [Test]
        public void TestAabb()
        {
            var item = new Item { Position = new float2(0, 0), Size = new float2(1, 1) }.GetAabb();
            var item2 = new Item { Position = new float2(1, 0), Size = new float2(1, 1) }.GetAabb();
            var item3 = new Item { Position = new float2(0.1f, 0.9f), Size = new float2(1, 1) }.GetAabb();

            Debug.Log($"Item 1 overlaps Item 2 {item.Overlaps(item2)}");
            Debug.Log($"Item 3 overlaps Item 1 {item3.Overlaps(item)}");
        }

    }

}
