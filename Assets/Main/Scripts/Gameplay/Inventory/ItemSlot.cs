

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace RPG.Gameplay.Inventory
{
    public struct ItemSlotDescription
    {
        public string GUID;
        public string FriendlyName;
        public string Description;
        public float Width;
        public float Height;

        public Texture2D Texture;
    }
    public class Telegraph : ItemSlot
    {
        public Telegraph()
        {
            AddToClassList("slot-icon-telegraph");
            UnregisterCallback<MouseUpEvent>(OnMouseEventUp);
        }
        override public bool StartDrag()
        {
            Debug.Log("Start Drag Telegraph");
            style.visibility = Visibility.Visible;
            BringToFront();
            return true;
        }
        override public void StopDrag()
        {
            style.visibility = Visibility.Hidden;
            SetPosition(0f);
        }
    }
    public class ItemSlot : VisualElement
    {
        bool isDragging;
        public ItemSlotDescription ItemSlotDescription { get; private set; }

        protected VisualElement imageBackground;

        public new class UxmlFactory : UxmlFactory<ItemSlot, ItemSlot.UxmlTraits>
        {

        }
        public ItemSlot()
        {
            AddToClassList("slot-icon");
            imageBackground = new VisualElement();
            imageBackground.AddToClassList("slot-background-container");
            Add(imageBackground);
            RegisterCallback<MouseUpEvent>(OnMouseEventUp);
        }

        protected void OnMouseEventUp(MouseUpEvent mouseUpEvent)
        {
            Debug.Log("On Mouse Event Up e");
            if (!isDragging)
            {
                var rect = layout;
                var mousePosition = new float2(rect.xMin, rect.yMin);
                GetGrid().StartDrag(this, mousePosition);
                mouseUpEvent.StopPropagation();
                return;
            }
        }
        virtual public void StopDrag()
        {
            isDragging = false;
            imageBackground.style.visibility = Visibility.Visible;

        }
        virtual public bool StartDrag()
        {
            if (IsEmpty()) { return false; }
            imageBackground.style.visibility = Visibility.Hidden;
            isDragging = true;
            return true;
        }
        public void SetLocalMousePosition(float2 localMousePosition)
        {
            style.top = localMousePosition.y - layout.height / 2;
            style.left = localMousePosition.x - layout.width / 2;
        }
        public void SetLocalMouseEventPosition(MouseMoveEvent mouseMoveEvent)
        {
            SetLocalMousePosition(mouseMoveEvent.localMousePosition);
        }
        public void SetPosition(float2 vector2)
        {
            style.top = vector2.y;
            style.left = vector2.x;
        }
        public void SetItem(ItemSlotDescription newItemSlotDescription)
        {
            this.ItemSlotDescription = newItemSlotDescription;
            imageBackground.style.backgroundImage = new StyleBackground(newItemSlotDescription.Texture);
        }
        public void ClearItem()
        {
            this.ItemSlotDescription = default;
            imageBackground.style.backgroundImage = default;
        }
        public ItemGrid GetGrid()
        {
            return GetFirstAncestorOfType<ItemGrid>();
        }

        public string GetGUID()
        {
            return ItemSlotDescription.GUID;
        }

        public bool IsEmpty()
        {
            return String.IsNullOrEmpty(ItemSlotDescription.GUID) || isDragging;
        }

    }

    public class ItemGrid : VisualElement
    {
        bool isDragging;
        ItemSlot originalSlot;

        Telegraph telegraph;

        ItemSlot draggedItem;

        ItemSlot nextSlot;

        public new class UxmlFactory : UxmlFactory<ItemGrid, ItemGrid.UxmlTraits>
        {

        }
        public ItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            for (int i = 0; i < 8 * 6; i++)
            {
                var itemSlot = new ItemSlot();
                Add(itemSlot);
            }
            telegraph = new Telegraph();
            Add(telegraph);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        void ClearHighlight()
        {
            this.Query<ItemSlot>().Where((e) => e != telegraph).ForEach((i) =>
            {
                i.RemoveFromClassList("slot-highlight-empty");
                i.RemoveFromClassList("slot-highlight");
            });
        }
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (draggedItem != null)
            {
                PlaceInNewslotIfValid();
                StopDrag(draggedItem);
                ClearHighlight();
                nextSlot = null;
            }
        }

        private void PlaceInNewslotIfValid()
        {
            if (nextSlot != null && nextSlot != draggedItem)
            {
                nextSlot.SetItem(draggedItem.ItemSlotDescription);
                draggedItem.ClearItem();
            }
        }

        public void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (draggedItem == null) { return; }
            Debug.Log($"Mouse Event From Grid x: {mouseMoveEvent.localMousePosition.x} y: {mouseMoveEvent.localMousePosition.y} ");
            telegraph.SetLocalMouseEventPosition(mouseMoveEvent);
            var itemSlots = this.Query<ItemSlot>().Where((e) => e != telegraph);
            bool slotFound = false;
            itemSlots.ForEach((itemSlot) =>
            {
                if (slotFound == false && itemSlot.worldBound.Contains(telegraph.worldBound.center))
                {
                    if (itemSlot.IsEmpty())
                    {
                        itemSlot.AddToClassList("slot-highlight-empty");
                        nextSlot = itemSlot;
                        slotFound = true;
                    }
                    else
                    {
                        itemSlot.AddToClassList("slot-highlight");
                    }
                }
                else
                {
                    itemSlot.RemoveFromClassList("slot-highlight-empty");
                    itemSlot.RemoveFromClassList("slot-highlight");
                }
            });

        }

        public void StartDrag(ItemSlot itemSlot, float2 position)
        {
            if (itemSlot.StartDrag())
            {
                draggedItem = itemSlot;
                telegraph.SetItem(itemSlot.ItemSlotDescription);
                telegraph.StartDrag();
                telegraph.SetPosition(position);
            }
        }
        public void StopDrag(ItemSlot itemSlot)
        {

            draggedItem = null;
            itemSlot.StopDrag();
            telegraph.StopDrag();
        }
        public void AddItem(ItemSlotDescription item)
        {
            var slot = this.Query<ItemSlot>().Where((s) => s.IsEmpty()).First();
            slot.SetItem(item);
        }
    }
}