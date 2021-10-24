using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
public class PlayersMoveSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    EntityQuery worldClickQueries;

    protected override void OnCreate()
    {
        base.OnCreate(); 
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
       
        worldClickQueries = GetEntityQuery(new ComponentType[] { 
            ComponentType.ReadOnly<WorldClick>()
        });
        NativeArray<WorldClick> worldClicks = worldClickQueries.ToComponentDataArray<WorldClick>(Allocator.TempJob);
        var commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.WithAll<PlayerControlled>().ForEach((Entity player,int entityInQueryIndex)=>{
            for(int i = 0; i < worldClicks.Length; i++) {
                commandBuffer.AddComponent(entityInQueryIndex, player, new MoveTo() {Position = worldClicks[i].WorldPosition});
            }
        }).WithDisposeOnCompletion(worldClicks).Schedule();  
    
          
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
