using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public interface IDamageProvider
    {
        // Returns the amount of damage the object should do
        int GetDamageAmount();
        
    }

}