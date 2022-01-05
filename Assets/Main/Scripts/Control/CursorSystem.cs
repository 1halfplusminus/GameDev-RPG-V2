using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace RPG.Control
{

    public struct VisibleCursor : IComponentData
    {
        public CursorType Cursor;
        public CursorType CurrentCursor;
    }

    public class CursorSystem : SystemBase
    {
        EntityQuery cursorsQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            cursorsQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Texture2D>(),
                ComponentType.ReadOnly<InGameCursor>(),
                ComponentType.ReadOnly<SharedGameCursorType>());
        }
        protected override void OnUpdate()
        {
            var query = cursorsQuery;
            var em = EntityManager;
            Entities
            .ForEach((ref VisibleCursor visibleCursor) =>
            {
                query.SetSharedComponentFilter(new SharedGameCursorType { Type = visibleCursor.Cursor });
                var cursor = query.GetSingletonEntity();
                var gameCursor = em.GetComponentData<InGameCursor>(cursor);
                var sharedTexture = em.GetComponentObject<Texture2D>(cursor);
                Cursor.SetCursor(sharedTexture, gameCursor.HotSpot, CursorMode.Auto);
            })
            .WithoutBurst()
            .Run();
        }
    }
}
// FIXME: To delete
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