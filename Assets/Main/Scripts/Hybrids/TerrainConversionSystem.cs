using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class TerrainConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Terrain terrain)=>{
            var terrainCollider = terrain.GetComponent<TerrainCollider>();
            AddHybridComponent(terrain);
            AddHybridComponent(terrainCollider);
            DeclareAssetDependency(terrain.gameObject,terrain.terrainData);
            DeclareAssetDependency(terrain.gameObject, terrainCollider );
            var entity = GetPrimaryEntity(terrain);
            DstEntityManager.AddComponentObject(entity,terrainCollider  );
        });
    }
}
