using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    [CreateAssetMenu(fileName = "Untitled Health Settings", menuName = "Settings.../Health Settings", order = 0)]
    public class HealthSettings : SerializedScriptableObject, IHealth
    {
        [SerializeField] private int max;
        [SerializeField] private int starting;
        
        public int Max => max; 
        public int Current => starting;
        
        /// <summary>
        /// always returns false because this is not an object
        /// </summary>
        public bool IsAlive => false;
        
        /// <summary>
        /// always returns false because this is not an object
        /// </summary>
        public bool IsDead => false;
    }

    public interface IHealth
    {
        public int Max { get; }
        public int Current { get; }
        
        public bool IsAlive { get; }
        public bool IsDead { get; }
        
    }
}