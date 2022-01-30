using System;
using Sirenix.OdinInspector;
using UnityEngine;


namespace forloopcowboy_unity_tools.Scripts.Environment
{
    [CreateAssetMenu(fileName = "Destructable Object Setting", menuName = "Settings/Destruction/Destructable Object Settings...", order = 0)]
    public class DestructableObjectSettings : SerializedScriptableObject, IDestructableObjectSettings
    {
        public static readonly int DEFAULT_HITS_UNTIL_DESTRUCTION = 0;
        
        [Tooltip("Number of hits before object gets swapped to its shattered counterpart.")]
        [SerializeField] private int hitsUntilDestruction = DEFAULT_HITS_UNTIL_DESTRUCTION;
        public int HitsUntilDestruction => this.hitsUntilDestruction;
    }
    
    public interface IDestructableObjectSettings
    {
        int HitsUntilDestruction { get; }
        
    }

    // little hack so I don't have to define the function twice
    // and can share some default stuff
    public static class DestructableObjectExtended
    {
        /// <returns>True if object has been hit enough times.</returns>
        public static bool IsReadyToDestroy(this IDestructableObjectSettings settings, int hitsSoFar) =>
            hitsSoFar >= settings.HitsUntilDestruction;
    }

    [Serializable]
    public struct SimpleDestructableObjectSettings : IDestructableObjectSettings
    {
        [SerializeField]
        private int hitsUntilDestruction;

        public SimpleDestructableObjectSettings(int hitsUntilDestruction)
        {
            this.hitsUntilDestruction = hitsUntilDestruction;
        }
        
        public int HitsUntilDestruction => hitsUntilDestruction;
    }
    
}