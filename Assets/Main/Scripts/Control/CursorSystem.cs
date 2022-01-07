using RPG.Combat;
using RPG.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
namespace RPG.Control
{
    public struct InGameCursor : IComponentData
    {
        public float2 HotSpot;
        public CursorType Type;
    }
    public enum CursorType : int
    {
        Combat = 1,
        Movement = 2,
        None = 0,
        Pickup = 3
    }

    public struct SharedGameCursorType : ISharedComponentData
    {

        public CursorType Type;
    }
    public struct VisibleCursor : IComponentData
    {
        public CursorType Cursor;
        public CursorType CurrentCursor;
    }
    [UpdateBefore(typeof(PlayersMoveSystem))]
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class CursorExtensionsSystem : SystemBase
    {

        protected override void OnUpdate()
        {

            Entities
            .WithNone<InteractWithUI>()
            .WithAll<PlayerControlled>()
            .ForEach((Entity e, ref VisibleCursor cursor, ref WorldClick worldClick, in Raycast raycast, in LocalToWorld localToWorld) =>
            {
                NavMesh.SamplePosition(worldClick.WorldPosition, out var hit, raycast.MaxNavMeshProjectionDistance, NavMesh.AllAreas);
                var shouldMove = false;
                if (hit.hit)
                {
                    var nashMeshPath = new NavMeshPath();
                    NavMesh.CalculatePath(localToWorld.Position, hit.position, NavMesh.AllAreas, nashMeshPath);

                    if (nashMeshPath.status == NavMeshPathStatus.PathComplete && CalculeDistance(nashMeshPath) <= raycast.MaxNavPathLength)
                    {
                        worldClick.WorldPosition = hit.position;
                        shouldMove = true;
                    }
                }
                if (!shouldMove)
                {
                    EntityManager.RemoveComponent<WorldClick>(e);
                    cursor.Cursor = CursorType.None;
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();
            Entities
            .WithNone<InteractWithUI>()
            .ForEach((ref VisibleCursor cursor, in DynamicBuffer<HittedByRaycastEvent> rayHits) =>
            {
                foreach (var rayHit in rayHits)
                {
                    if (rayHit.Hitted != Entity.Null)
                    {
                        if (HasComponent<PickableWeapon>(rayHit.Hitted))
                        {
                            cursor.Cursor = CursorType.Pickup;
                        }
                    }
                }
            }).ScheduleParallel();
        }

        private static float CalculeDistance(NavMeshPath navMeshPath)
        {
            var distance = 0f;
            if (navMeshPath.corners.Length < 2) return distance;
            for (int i = 0; i < navMeshPath.corners.Length - 1; i++)
            {
                distance += math.abs(math.distance(navMeshPath.corners[i], navMeshPath.corners[i + 1]));
            }
            return distance;
        }
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]
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
            .WithNone<InteractWithUI>()
            .ForEach((ref VisibleCursor visibleCursor) =>
            {
                query.SetSharedComponentFilter(new SharedGameCursorType { Type = visibleCursor.Cursor });
                if (query.CalculateEntityCount() > 0)
                {
                    var cursor = query.GetSingletonEntity();
                    var gameCursor = em.GetComponentData<InGameCursor>(cursor);
                    var sharedTexture = em.GetComponentObject<Texture2D>(cursor);
                    Cursor.SetCursor(sharedTexture, gameCursor.HotSpot, CursorMode.Auto);
                }

            })
            .WithoutBurst()
            .Run();

        }
    }

    //TODO: Move to control
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public class PlayerControlledCombatTargettingSystem : SystemBase
    {


        protected override void OnUpdate()
        {
            //FIXME: Should be in another system

            Entities
            .ForEach((Entity e, ref Fighter fighter, in DynamicBuffer<HittedByRaycastEvent> rayHits, in MouseClick mouseClick) =>
            {
                fighter.TargetFoundThisFrame = 0;
                foreach (var rayHit in rayHits)
                {
                    if (HasComponent<Hittable>(rayHit.Hitted))
                    {
                        if (rayHit.Hitted != e)
                        {
                            if (mouseClick.CapturedThisFrame)
                            {
                                fighter.Target = rayHit.Hitted;
                            }
                            fighter.TargetFoundThisFrame += 1;
                        }

                    }

                }
            }).ScheduleParallel();
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