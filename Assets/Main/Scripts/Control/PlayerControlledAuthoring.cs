using UnityEngine;
using RPG.Core;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;

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
        public float RaycastRadius;
        public PhysicsCategoryTags BelongTo;
        public PhysicsCategoryTags CollidWith;

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
                DstEntityManager.AddBuffer<HittedByRaycastEvent>(entity);
                DstEntityManager.AddComponentData(entity, new Raycast
                {
                    Radius = playerControlled.RaycastRadius,
                    Distance = playerControlled.RaycastDistance,
                    MaxNavMeshProjectionDistance = playerControlled.MaxNavMeshProjectionDistance,
                    MaxNavPathLength = playerControlled.MaxNavPathLength,
                    CollisionFilter = new Unity.Physics.CollisionFilter
                    {
                        BelongsTo = playerControlled.BelongTo.Value,
                        CollidesWith = playerControlled.CollidWith.Value
                    }
                });
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
