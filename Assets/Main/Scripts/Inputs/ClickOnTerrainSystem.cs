using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ExtensionMethods;
using UnityEngine.InputSystem;

public struct WorldClick: IComponentData {
    public float3 WorldPosition;
}
public class ClickOnTerrainSystem : SystemBase
{   
    const float MAX_DISTANCE = 10000000f;
    EntityQuery queryClicks;
    EntityQuery queryTerrains;
    protected override void OnUpdate()
    {
        queryClicks = GetEntityQuery(new ComponentType[] {
            ComponentType.ReadOnly<MouseClick>()
        });
        queryTerrains = GetEntityQuery(new ComponentType[] {
            ComponentType.ReadOnly<TerrainCollider>()
        });
        var clicks = queryClicks.ToComponentDataArray<MouseClick>(Allocator.Temp);
        var terrains = queryTerrains.ToComponentArray<TerrainCollider>();
        var terrainEntities = queryTerrains.ToEntityArray(Allocator.Temp);
        foreach (var click in clicks)
        {
            if(!click.Ray.Displacement.Equals(float3.zero)) {
                 for(int i = 0; i < terrains.Length; i++) {
                    RaycastHit hit;
                    terrains[i].Raycast(click.Ray.ToEngineRay(), out hit,MAX_DISTANCE);
                    if(hit.collider) {
                        var worldClick  = new WorldClick {WorldPosition = hit.point};
                        if(EntityManager.HasComponent<WorldClick>(terrainEntities[i])) {
                            EntityManager.SetComponentData(terrainEntities[i],worldClick);
                        } else {
                            EntityManager.AddComponentData(terrainEntities[i],worldClick);
                        }
                
                        Debug.Log("Clicked on " + hit.collider.name);
                    }
                }
            }
          
        }
        clicks.Dispose();
        terrainEntities.Dispose();
    }
}

public class TestBlendTree1DSystem : SystemBase
{
     protected override void OnUpdate()
    {
        var delta = Keyboard.current.downArrowKey.ReadValue();
        Entities.WithoutBurst().ForEach((ref BlendTree1DData data)=>{
            data.paramX = math.clamp(data.paramX + delta,0.0f,1.0f);
        }).Run();
    }
}