using System;
using Unity.Entities;

namespace RPG.Core
{
    [Serializable]
    public struct GameSettings : IComponentData
    {
        public Unity.Entities.Hash128 NewGameScene;

        public Unity.Entities.Hash128 PlayerScene;
    }
}