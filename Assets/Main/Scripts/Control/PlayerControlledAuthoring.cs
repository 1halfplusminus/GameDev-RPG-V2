using UnityEngine;
using RPG.Core;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace RPG.Control
{
    public struct Texture
    {
        public BlobArray<byte> Data;
        public int Height;

        public int Width;

        public int MipmapCount;

        public TextureFormat Format;
    }
    public struct GameCursor
    {

        public Texture Texture;
        public float2 HotSpot;

        public CursorType Type;

    }
    public struct PlayerCursors : IBufferElementData
    {
        public BlobAssetReference<GameCursor> Cursor;
    }
    public enum CursorType : int
    {
        Combat = 1,
        Movement = 2,
        None = 0
    }
    [Serializable]
    public struct PlayerCursor
    {
        public CursorType Type;
        public Texture2D Texture;

        public float2 HotSpot;
    }
    public class PlayerControlledAuthoring : MonoBehaviour
    {
        public float RaycastDistance;

        public PlayerCursor[] Cursors;

    }

    public class PlayerControlledConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerControlledAuthoring playerControlled) =>
            {
                var entity = GetPrimaryEntity(playerControlled);
                DstEntityManager.AddComponent<PlayerControlled>(entity);
                DstEntityManager.AddComponentData(entity, new Raycast { Distance = playerControlled.RaycastDistance });
                DstEntityManager.AddComponent<MouseClick>(entity);
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                foreach (var cursor in playerControlled.Cursors)
                {

                    ref var root = ref blobBuilder.ConstructRoot<GameCursor>();
                    blobBuilder.Construct(ref root.Texture.Data, cursor.Texture.GetRawTextureData());
                    root.Texture.MipmapCount = cursor.Texture.mipmapCount;
                    root.Texture.Height = cursor.Texture.height;
                    root.Texture.Width = cursor.Texture.width;
                    root.Texture.Format = cursor.Texture.format;
                    root.Type = cursor.Type;
                    root.HotSpot = cursor.HotSpot;
                    var blobAssetRef = blobBuilder.CreateBlobAssetReference<GameCursor>(Allocator.Persistent);
                    DstEntityManager.AddComponentData(entity, new VisibleCursor { Cursor = blobAssetRef });
                }
                blobBuilder.Dispose();
                if (playerControlled.Cursors.Length > 0)
                {
                    // DstEntityManager.AddSharedComponentData(entity, new RenderCursor { GameCursor = playerControlled.Cursors[0].Cursor });
                }
            });
        }
    }
}
