using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using RPG.Mouvement;

public class TerrainConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Terrain terrain) =>
        {
            var terrainCollider = terrain.GetComponent<UnityEngine.TerrainCollider>();
            AddHybridComponent(terrain);
            // AddHybridComponent(terrainCollider);
            var entity = GetPrimaryEntity(terrain);
            // DstEntityManager.AddComponentObject(entity, terrainCollider);

            if (terrainCollider == null || terrainCollider.terrainData == null)
                return;

            var data = terrainCollider.terrainData;
            var size = new int2(data.heightmapResolution, data.heightmapResolution);
            var delta = data.size.x / (size.x - 1);
            var scale = new float3(delta, 1f, delta);

            var heights = new NativeArray<float>(size.x * size.y * UnsafeUtility.SizeOf<float>(), Allocator.Temp);

            var index = 0;
            for (var i = 0; i < size.x; ++i)
            {
                for (var j = 0; j < size.y; ++j)
                {
                    heights[index] = data.GetHeight(j, i);
                    ++index;
                }
            }

            var colliders = Unity.Physics.TerrainCollider.Create(heights, size, scale,
                Unity.Physics.TerrainCollider.CollisionMethod.VertexSamples,
                CollisionFilter.Default);
            DstEntityManager.AddComponent<Navigable>(entity);
            DstEntityManager.AddComponentData(entity, new PhysicsCollider()
            {
                Value = colliders
            });
            heights.Dispose();
            // DeclareAssetDependency(terrain.gameObject, terrain.terrainData);
            // DeclareAssetDependency(terrain.gameObject, terrainCollider);
        });
    }
}
