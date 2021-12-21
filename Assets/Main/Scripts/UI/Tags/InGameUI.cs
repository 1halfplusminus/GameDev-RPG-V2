
using RPG.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.UI
{
    [GenerateAuthoringComponent]
    public struct InGameUI : IComponentData { }


    public class InGameUIController : Object, IComponentData
    {
        private Label Health;

        public void Init(VisualElement root)
        {
            Health = root.Q<Label>("Health");
        }

        public void SetHealth(Health health)
        {
            Debug.Log($"Set Health {health.Value} {health.MaxHealth} {health.GetPercent()}");
            Health.Clear();
            Health.text = (health.Value / health.MaxHealth).ToString("P");
        }

    }

    public interface IUIController
    {
        void Init(VisualElement root);
    }

}