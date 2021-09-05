using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// Damage providers are tagged with "IDamaging" in the game
    public class SimpleDamageProvider : MonoBehaviour, IDamaging
    {
        public int max = 2;
        public int min = 1;
        
        public int GetDamageAmount()
        {
            if (max < min)
            {
                Debug.LogWarning("Min and max were inverted. The game fixed it for you, dummy!");
                AdjustMinMax();
            }

            return Random.Range(min, max);
        }

        // correcting function in case the stupid game designmer fumbles the concepts in his feeble mind
        private void AdjustMinMax()
        {
            (max, min) = (min, max);
        }

        private void OnEnable() {
            this.gameObject.tag = DamageSystem.tag;
        }

    }
    
}