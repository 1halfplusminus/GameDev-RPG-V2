using UnityEngine;

namespace RPG.Core
{
    public interface IDrawGizmo
    {

        public void OnDrawGizmosSelected(Transform transform);
        public void OnDrawGizmosSelected();
    }
}