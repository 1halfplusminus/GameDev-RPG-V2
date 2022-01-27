#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;

namespace RPG.Gameplay
{
    public class DialogGraphNode : Node
    {
        public string GUID;

        public string DialogueText;

        public bool EntryPoint;
    }
}
#endif