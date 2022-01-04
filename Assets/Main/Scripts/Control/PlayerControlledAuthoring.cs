using UnityEngine;
using RPG.Core;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

namespace RPG.Control
{
    public struct Texture
    {

        public BlobArray<Color32> Data;
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

                var cursorBuffer = DstEntityManager.AddBuffer<PlayerCursors>(entity);
                // var cursors = playerControlled.Cursors.ToDictionary((c) => (int)c.Type);
                // foreach (var cursorType in Enum.GetValues(typeof(CursorType)).Cast<CursorType>())
                // {
                //     var blobBuilder = new BlobBuilder(Allocator.Temp);
                //     ref var root = ref blobBuilder.ConstructRoot<GameCursor>();
                //     var index = (int)cursorType;
                //     root.Type = cursorType;
                //     if (cursors.ContainsKey(index))
                //     {
                //         var cursor = cursors[index];
                //         var rawData = cursor.Texture.GetPixelData<Color32>(0);
                //         unsafe
                //         {
                //             blobBuilder.Construct(ref root.Texture.Data, rawData.ToArray());
                //         }

                //         root.Texture.MipmapCount = cursor.Texture.mipmapCount;
                //         root.Texture.Height = cursor.Texture.height;
                //         root.Texture.Width = cursor.Texture.width;
                //         root.Texture.Format = cursor.Texture.format;
                //         root.HotSpot = cursor.HotSpot;
                //         Debug.Log($"Find cursor overwrite for {cursorType}");
                //     }
                //     var blobAssetRef = blobBuilder.CreateBlobAssetReference<GameCursor>(Allocator.Persistent);
                //     cursorBuffer.Add(new PlayerCursors { Cursor = blobAssetRef });
                //     Debug.Log($"Add cursor {cursorType}");
                //     blobBuilder.Dispose();
                // }
                AddHybridComponent(playerControlled);
                DstEntityManager.AddComponentData(entity, new VisibleCursor { Cursor = CursorType.Movement });

            });
        }
    }
}
