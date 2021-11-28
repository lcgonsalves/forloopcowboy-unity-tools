using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [CreateAssetMenu(fileName = "Untitled NPC Settings", menuName = "Settings/NPC/New NPC Settings...", order = 0)]
    public class NPCSettings : SerializedScriptableObject
    {
        public Dictionary<int, AccuracyProcessor> accuracySettingsPerNumberOfStars;
        
        [InlineEditor(InlineEditorModes.FullEditor)]
        public ArmorSettings armorSettingsPerNumberOfStars;
    }
}