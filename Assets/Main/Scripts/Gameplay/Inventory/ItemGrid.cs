

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using System.Collections.Generic;
using RPG.UI;

namespace RPG.Gameplay.Inventory
{

    public class InventoryRootController : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InventoryRootController, InventoryRootController.UxmlTraits>
        {
            public override string uxmlName => base.uxmlName;

            public override string uxmlNamespace => base.uxmlNamespace;

            public override string uxmlQualifiedName => base.uxmlQualifiedName;

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription => base.uxmlAttributesDescription;

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription => base.uxmlChildElementsDescription;

            public override string substituteForTypeName => base.substituteForTypeName;

            public override string substituteForTypeNamespace => base.substituteForTypeNamespace;

            public override string substituteForTypeQualifiedName => base.substituteForTypeQualifiedName;

            public override bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
            {
                return base.AcceptsAttributeBag(bag, cc);
            }

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var result = base.Create(bag, cc);
                cc.slotInsertionPoints.TryAdd("Container", result);
                return result;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }
        }
    }
    public static class InventoryExtensions
    {
        public static void CreateInventorySlots(EntityManager em, Entity e, Inventory inventory)
        {
            var buffer = em.AddBuffer<InventorySlot>(e);
            buffer.Capacity = inventory.Size;
        }
        public static void MoveItemInInventory(
           Inventory inventory,
           DynamicBuffer<InventorySlot> inventorySlots,
           ref InventoryItem item,
           Entity itemEntity,
           int2 newPosition)
        {
            RemoveItemInInventory(inventory, inventorySlots, item);
            item.Coordinate = newPosition;
            AddItemInInventory(inventory, inventorySlots, item, itemEntity);
        }
        public static void RemoveItemInInventory(
            Inventory inventory,
            DynamicBuffer<InventorySlot> inventorySlots,
            InventoryItem item)
        {
            // var itemSlots = inventory.GetSlotForItem(item);
            // for (int i = 0; i < inventorySlots.Length; i++)
            // {
            //     inventorySlots[i] = new InventorySlot { Item = Entity.Null };
            // }
        }
        public static void AddItemInInventory(
            Inventory inventory,
            DynamicBuffer<InventorySlot> inventorySlots,
            InventoryItem item,
            Entity itemEntity
        )
        {
            // var itemSlots = inventory.GetSlotForItem(item);
            // for (int i = 0; i < inventorySlots.Length; i++)
            // {
            //     inventorySlots[i] = new InventorySlot { Item = itemEntity };
            // }
        }
    }


    public struct Inventory : IComponentData
    {
        public int Height;
        public int Width;

        public int Size
        {
            get
            {
                return Height * Width;
            }
        }
        public int GetIndex(int x, int y)
        {
            return (y * Width) + x;

        }
        public int GetIndex(int2 coordinate)
        {
            return GetIndex(coordinate.x, coordinate.y);

        }

    }
    public struct ItemSlotDescription
    {
        public string GUID;
        public string FriendlyName;
        public string Description;

        public int2 Dimension;

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

        public int _index;

        public int Index { get => _index; set { _index = value; OnChange(); } }

        event Action OnChange;
        float height;
        float width;
        bool isDragging;
        Label indexLabel;
        public ItemSlotDescription ItemSlotDescription { get; private set; }

        protected VisualElement imageBackground;

        public new class UxmlFactory : UxmlFactory<ItemSlot, ItemSlot.UxmlTraits>
        {

        }
        public ItemSlot()
        {

            AddToClassList("slot-icon");
            height = resolvedStyle.height;
            width = resolvedStyle.width;
            imageBackground = new VisualElement();
            imageBackground.AddToClassList("slot-background-container");
            Add(imageBackground);
            RegisterCallback<MouseUpEvent>(OnMouseEventUp);
            //DEBUG TEXT
            var text = new Label();
            text.style.flexGrow = 1;
            OnChange += () =>
            {
                text.text = $"{Index}";
            };
            Add(text);
        }
        public void SetSize(float Width, float Height)
        {
            height = Height;
            width = Width;
        }
        public void SetSize(float2 dimension)
        {
            SetSize(dimension.x, dimension.y);
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
        public void Resize(int2 dimension)
        {
            style.height = dimension.y * height;
            style.width = dimension.x * width;
        }
        public void SetItem(ItemSlotDescription newItemSlotDescription)
        {
            this.ItemSlotDescription = newItemSlotDescription;
            Resize(newItemSlotDescription.Dimension);
            imageBackground.style.backgroundImage = new StyleBackground(newItemSlotDescription.Texture);
        }
        public void ClearItem()
        {
            this.ItemSlotDescription = default;
            imageBackground.style.backgroundImage = default;
            style.height = height;
            style.width = width;
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

        public int2 GetCoordinate()
        {
            return new int2((int)(layout.x / width), (int)(layout.y / height));
        }

    }

    public class ItemGrid : VisualElement
    {
        ItemSlot[] items;

        public InventoryGUI inventoryGUI;
        public float2 ItemSize = new float2(150, 150);
        bool isDragging;
        ItemSlot originalSlot;

        Telegraph telegraph;

        ItemSlot draggedItem;

        ItemSlot nextSlot;

        Inventory inventory;
        public new class UxmlFactory : UxmlFactory<ItemGrid, ItemGrid.UxmlTraits>
        {

        }
        public ItemGrid()
        {

            AddToClassList("inventory-grid");
            name = "Grid";
            telegraph = new Telegraph();
            telegraph.SetSize(ItemSize);
            Add(telegraph);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        void ClearHighlight()
        {
            GetItemSlotsQuery().ForEach((i) =>
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
                ReDraw();
            }
        }

        private void PlaceInNewslotIfValid()
        {
            if (nextSlot != null && !IsSameSlot(nextSlot.Index, draggedItem.Index) && !nextSlot.ClassListContains("slot-highlight"))
            {
                RemoveItemSlot(draggedItem.Index);
                SetItemAtSlot(nextSlot.Index, telegraph.ItemSlotDescription);
            }
            else
            {
                // RemoveItemSlot(draggedItem.Index);
                SetItemAtSlot(draggedItem.Index, telegraph.ItemSlotDescription);
            }
        }
        private bool IsSameSlot(int firstSlotIndex, int secondSlotIndex)
        {
            if (firstSlotIndex == secondSlotIndex)
            {
                return true;
            }
            using var firstSlots = inventoryGUI.GetSlots(firstSlotIndex);
            using var secondSlots = inventoryGUI.GetSlots(secondSlotIndex);
            firstSlots.Sort();
            secondSlots.Sort();
            for (int i = 1; i < firstSlots.Length; i++)
            {
                for (int j = 0; j < secondSlots.Length; j++)
                {
                    if (firstSlots[i] == secondSlots[j])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        void SetItemAtSlot(int index, ItemSlotDescription itemSlotDescription)
        {
            var slots = GetItemSlotsQuery();
            inventoryGUI.ResizeSlot(index, itemSlotDescription.Dimension);
            using var hits = inventoryGUI.GetSlots(index);
            hits.Sort();
            for (var i = 0; i < hits.Length; i++)
            {
                Debug.Log($"item {index} take slot {hits[i]} ");
                var overlapsSlot = slots.AtIndex(hits[i]);
                overlapsSlot.SetItem(itemSlotDescription);
                if (i != 0)
                {
                    inventoryGUI.ResizeSlot(hits[i], 1);
                }

            }
        }
        void RemoveItemSlot(int index)
        {
            var slots = GetItemSlotsQuery();
            using var hits = inventoryGUI.GetSlots(index);
            for (var i = 0; i < hits.Length; i++)
            {
                var slotFound = slots.AtIndex(hits[i]);
                inventoryGUI.ResizeSlot(hits[i], 1);
                slotFound.ClearItem();
            }

        }
        public bool IsEmpty(int slotIndex)
        {
            var uiSlots = GetItemSlotsQuery();
            var slots = inventoryGUI.GetSlots(slotIndex);
            for (int i = 0; i < slots.Length; i++)
            {
                if (!uiSlots.AtIndex(slots[i]).IsEmpty())
                {
                    return false;
                }
            }
            return true;
        }
        public void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (draggedItem == null) { return; }
            if (nextSlot != null)
            {
                if (nextSlot.ClassListContains("slot-highlight-empty"))
                {
                    inventoryGUI.ResizeSlot(nextSlot.Index, 1);
                    nextSlot.RemoveFromClassList("slot-highlight-empty");
                }
                else
                {
                    nextSlot.RemoveFromClassList("slot-highlight");
                }
            }
            // Debug.Log($"Mouse Event From Grid x: {(int)(mouseMoveEvent.localMousePosition.x / ItemSize.x) } y: {(int)(mouseMoveEvent.localMousePosition.y / ItemSize.y)} ");
            var slots = GetItemSlotsQuery();
            var coordinate = new int2
            {
                x = (int)(mouseMoveEvent.localMousePosition.x / ItemSize.x),
                y = (int)(mouseMoveEvent.localMousePosition.y / ItemSize.y),
            };
            telegraph.SetLocalMouseEventPosition(mouseMoveEvent);
            using var result = inventoryGUI.ColliderCast(coordinate, telegraph.ItemSlotDescription.Dimension);
            bool isEmpty = true;
            for (int i = 0; i < result.Length; i++)
            {
                if (!slots.AtIndex(result[i]).IsEmpty())
                {

                    isEmpty = false;
                    break;
                }
            }
            nextSlot = slots.AtIndex(result[0]);
            if (!isEmpty)
            {
                nextSlot.AddToClassList("slot-highlight");
            }
            else
            {
                nextSlot.AddToClassList("slot-highlight-empty");
            }

        }

        public void ReDraw()
        {
            inventoryGUI.ScheduleCalculeOverlapse();
            var slots = GetItemSlotsQuery();
            var overlapses = inventoryGUI.Overlapses;
            for (int i = 0; i < inventory.Size; i++)
            {
                var uiElementSlot = slots.AtIndex(i);
                var currentSlot = inventoryGUI.GetSlot(i);
                // var currentSlot = inventoryGUI.GetSlot(i);
                // var collider = inventoryGUI.GetAabb(i);
                // uiElementSlot.SetPosition(new float2(collider.Min.x, collider.Min.y));
                // uiElementSlot.style.position = Position.Absolute;
                // uiElementSlot.style.width = collider.Extents.x;
                // uiElementSlot.style.height = collider.Extents.y;
                // uiElementSlot.style.minWidth = collider.Extents.x;
                // uiElementSlot.style.minHeight = collider.Extents.y;
                if (overlapses[i])
                {
                    uiElementSlot.style.display = DisplayStyle.None;
                }
                else
                {
                    uiElementSlot.style.display = DisplayStyle.Flex;
                    uiElementSlot.Resize(currentSlot.Scale);
                }
            }
        }

        public void StartDrag(ItemSlot itemSlot, float2 position)
        {
            if (itemSlot.StartDrag())
            {

                telegraph.SetItem(itemSlot.ItemSlotDescription);
                telegraph.StartDrag();
                telegraph.SetPosition(position);
                RemoveItemSlot(itemSlot.Index);
                draggedItem = itemSlot;

                // ReDraw();

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
            var itemSlot = GetItemSlotsQuery();
            var slot = GetItemSlotsQuery().Where((i) => i.IsEmpty()).First();
            if (slot != null)
            {
                SetItemAtSlot(slot.Index, item);
            }
            // ReDraw();
        }
        public void DrawItems(ItemSlotDescription[] items)
        {
            var itemSlot = GetItemSlotsQuery();
            itemSlot.ForEach((i) =>
            {
                i.ClearItem();
            });
            for (int i = 0; i < items.Length; i++)
            {
                SetItemAtSlot(i, items[i]);
            }
        }
        public void InitInventory(Inventory inventory)
        {
            this.style.minWidth = inventory.Width * ItemSize.x;
            this.style.minHeight = inventory.Height * ItemSize.y;
            this.inventory = inventory;
            inventoryGUI = new InventoryGUI();
            inventoryGUI.Init(inventory, ItemSize);
            for (int j = 0; j < inventory.Height; j++)
            {
                for (int i = 0; i < inventory.Width; i++)
                {
                    var itemSlot = new ItemSlot();
                    itemSlot.SetSize(ItemSize);
                    itemSlot.Index = inventory.GetIndex(i, j);
                    Add(itemSlot);
                }
            }

        }
        public UQueryBuilder<ItemSlot> GetItemSlotsQuery()
        {
            return this.Query<ItemSlot>().Where((e) => e != telegraph);
        }
        public ItemSlot GetItemSlotAtIndex(int index)
        {
            return GetItemSlotsQuery().AtIndex(index);
        }

    }
}