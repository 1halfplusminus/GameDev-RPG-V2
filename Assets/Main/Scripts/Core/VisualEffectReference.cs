
using System;
using RPG.Core;
using UnityEngine.VFX;

namespace RPG.Core
{
    [Serializable]
    public class VisualEffectReference : ComponentReference<VisualEffect>
    {
        public VisualEffectReference(string guid) : base(guid)
        {
        }
    }
}
