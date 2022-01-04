using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace RPG.Control
{

    public struct PlayerControlled : IComponentData { }

    public struct VisibleCursor : IComponentData
    {
        public CursorType Cursor;
        public CursorType CurrentCursor;
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

        Dictionary<CursorType, Texture2D> textures;

        protected override void OnCreate()
        {
            base.OnCreate();
            textures = new Dictionary<CursorType, Texture2D>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

        }
        protected override void OnUpdate()
        {

            Entities
            // .WithChangeFilter<VisibleCursor>()
            .ForEach((PlayerControlledAuthoring playerControlled, ref VisibleCursor visibleCursor) =>
            {
                var managedCursor = playerControlled.Cursors.ToDictionary((c) => c.Type);
                // if (!textures.ContainsKey(visibleCursor.Cursor))
                // {
                //     Texture2D texCopy = new Texture2D(
                //         cursorRef.Cursor.Value.Texture.Width,
                //         cursorRef.Cursor.Value.Texture.Height,
                //         cursorRef.Cursor.Value.Texture.Format,
                //         false
                //     );
                //     texCopy.SetPixelData(cursorRef.Cursor.Value.Texture.Data.ToArray(), 0);
                //     texCopy.Apply();
                //     textures.Add(visibleCursor.Cursor, texCopy);
                // }
                // var text = textures[visibleCursor.Cursor];
                // Cursor.SetCursor(text, cursorRef.Cursor.Value.HotSpot, CursorMode.Auto);
                // visibleCursor.CurrentCursor = visibleCursor.Cursor;
                Cursor.SetCursor(managedCursor[visibleCursor.Cursor].Texture, managedCursor[visibleCursor.Cursor].HotSpot, CursorMode.Auto);
            })
            .WithoutBurst()
            .Run();


        }

    }

}
