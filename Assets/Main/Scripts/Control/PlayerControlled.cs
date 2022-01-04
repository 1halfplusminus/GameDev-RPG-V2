using System;
using Unity.Entities;
using UnityEngine;

namespace RPG.Control
{

    public struct PlayerControlled : IComponentData { }

    public struct VisibleCursor : IComponentData
    {
        public BlobAssetReference<GameCursor> Cursor;
    }
    // [Serializable]
    // public struct RenderCursor : ISharedComponentData, IEquatable<RenderCursor>
    // {
    //     public GameCursor GameCursor;

    //     public bool Equals(RenderCursor other)
    //     {
    //         return GameCursor.Texture == other.GameCursor.Texture;
    //     }

    //     public override int GetHashCode()
    //     {
    //         return GameCursor.GetHashCode();
    //     }
    // }


    public class CursorSystem : SystemBase
    {
        EntityQuery cursorQuery;
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var cb = entityCommandBufferSystem.CreateCommandBuffer();
            Entities
            .WithStoreEntityQueryInField(ref cursorQuery).ForEach((in VisibleCursor visibleCursor) =>
            {
                Texture2D texCopy = new Texture2D(
                    visibleCursor.Cursor.Value.Texture.Width,
                    visibleCursor.Cursor.Value.Texture.Height,
                    visibleCursor.Cursor.Value.Texture.Format,
                    visibleCursor.Cursor.Value.Texture.MipmapCount > 1);
                texCopy.LoadRawTextureData(visibleCursor.Cursor.Value.Texture.Data.ToArray());
                texCopy.Apply();
                Cursor.SetCursor(texCopy, visibleCursor.Cursor.Value.HotSpot, CursorMode.Auto);
            }).WithoutBurst().Run();
            EntityManager.RemoveComponent<VisibleCursor>(cursorQuery);
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

    }

}
