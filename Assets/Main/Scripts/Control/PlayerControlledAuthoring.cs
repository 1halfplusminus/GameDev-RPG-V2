using UnityEngine;
using RPG.Core;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Control
{


    public class PlayerControlledAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct InGameCursorAuthoring
        {
            public Texture2D Texture;
            public float2 HotSpot;
            public CursorType Type;
        }
        public float RaycastDistance;

        public InGameCursorAuthoring[] Cursors;


        public float MaxNavPathLength;
        public float MaxNavMeshProjectionDistance;

    }
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class PlayerControlledConversionSystemDeclareReferencedObjects : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerControlledAuthoring playerControlled) =>
              {
                  foreach (var cursor in playerControlled.Cursors)
                  {
                      DeclareAssetDependency(playerControlled.gameObject, cursor.Texture);
                      DeclareReferencedAsset(cursor.Texture);
                  }
              });
        }
    }
    public class PlayerControlledConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((PlayerControlledAuthoring playerControlled) =>
            {
                var entity = GetPrimaryEntity(playerControlled);
                DstEntityManager.AddComponent<PlayerControlled>(entity);
                DstEntityManager.AddComponentData(entity, new Raycast { Distance = playerControlled.RaycastDistance, MaxNavMeshProjectionDistance = playerControlled.MaxNavMeshProjectionDistance, MaxNavPathLength = playerControlled.MaxNavPathLength });
                DstEntityManager.AddComponent<MouseClick>(entity);
                foreach (var cursor in playerControlled.Cursors)
                {
                    var iconEntity = GetPrimaryEntity(cursor.Texture);
                    DstEntityManager.AddSharedComponentData(iconEntity, new SharedGameCursorType { Type = cursor.Type });
                    DstEntityManager.AddComponentData(iconEntity, new InGameCursor { HotSpot = cursor.HotSpot });
                    DstEntityManager.AddComponentObject(iconEntity, cursor.Texture);
                }
                DstEntityManager.AddComponentData(entity, new VisibleCursor { Cursor = CursorType.Movement });

            });
        }
    }
}
// TODO: To DELETE
// var cursorBuffer = DstEntityManager.AddBuffer<PlayerCursors>(entity);
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

// public struct Texture
// {
//     public BlobArray<Color32> Data;
//     public int Height;
//     public int Width;
//     public int MipmapCount;
//     public TextureFormat Format;
// }


// public struct PlayerCursors : IBufferElementData
// {
//     public BlobAssetReference<GameCursor> Cursor;
// }