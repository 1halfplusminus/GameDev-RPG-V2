

using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    public class Joystick : VisualElement
    {
        float2 Center;
        Unity.Entities.BlobAssetReference<Unity.Physics.Collider> boxCollider;
        Unity.Entities.BlobAssetReference<Unity.Physics.Collider> circleCollider;
        VisualElement Circle;

        public float2 Mouvement;
        public new class UxmlFactory : UxmlFactory<Joystick, Joystick.UxmlTraits>
        { }
        public Joystick()
        {

            RegisterCallback((EventCallback<DetachFromPanelEvent>)((e) =>
            {
                if (boxCollider.IsCreated)
                {
                    boxCollider.Dispose();
                }
                if (circleCollider.IsCreated)
                {
                    circleCollider.Dispose();
                }
            }));
            RegisterCallback((EventCallback<AttachToPanelEvent>)((e) =>
            {
                Circle = this.Q<VisualElement>("Circle");
                // InitCollider();
            }));
            RegisterCallback((EventCallback<MouseLeaveEvent>)((e) =>
            {
                if (e.clickCount == 0)
                {
                    Reset();
                }
            }));
            RegisterCallback<MouseUpEvent>((e) =>
            {
                Reset();
            });
            RegisterCallback<MouseMoveEvent>((e) =>
            {
                InitCollider();
#if !UNITY_ANDROID
                    if(e.clickCount == 0){
                        break;
                    }
#endif
                if (Circle != null && boxCollider.IsCreated)
                {
                    var transform = new RigidTransform { pos = new float3(Circle.layout.position, 0f), rot = quaternion.identity };
                    if (boxCollider.Value.CalculateAabb().Overlaps(circleCollider.Value.CalculateAabb(transform)))
                    {
                        var unclampledMouvement = (Center - (float2)e.localMousePosition) / (new float2(this.layout.width, this.layout.height) / 2f);
                        Mouvement = math.clamp(unclampledMouvement, -1f, 1f);
                        Circle.style.left = e.localMousePosition.x - Circle.layout.xMax / 2f;
                        Circle.style.top = e.localMousePosition.y - Circle.layout.yMax / 2f;
                        var ignoreValue = 0.4f;
                        if (math.abs(Mouvement.x) < ignoreValue)
                        {
                            Mouvement.x = 0f;
                        }
                        if (math.abs(Mouvement.y) < 0.1f)
                        {
                            Mouvement.y = 0f;
                        }
                        if (Mouvement.y > 0)
                        {
                            Debug.Log($"Direction Haut");
                        }
                        else if (Mouvement.y < 0)
                        {
                            Debug.Log($"Direction Bas");
                        }
                        if (Mouvement.x > 0)
                        {
                            Debug.Log($"Direction Droite");
                        }
                        else
                        {
                            Debug.Log($"Direction Gauche");
                        }
                        // Mouvement

                    }
                }
            });
        }

        private void Reset()
        {
            Mouvement = 0f;
            Circle.style.left = 0;
            Circle.style.top = 0;
        }

        private void InitCollider()
        {
            if (!boxCollider.IsCreated && !float.IsNaN(layout.height))
            {
                var boxSize = new float3(layout.width, layout.height, 1f);
                var boxGeometry = new BoxGeometry { Center = boxSize / 2.0f, Orientation = quaternion.identity, Size = boxSize };
                boxCollider = Unity.Physics.BoxCollider.Create(boxGeometry);
                var circleSize = new float3(Circle.layout.width, Circle.layout.height, 1f);
                var circleGeometry = new BoxGeometry { Center = circleSize / 2.0f, Orientation = quaternion.identity, Size = circleSize };
                Center = Circle.layout.center;
                circleCollider = Unity.Physics.BoxCollider.Create(circleGeometry);
            }

        }
    }
}