using Unity.Netcode;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class SimpleDamageProvider : NetworkBehaviour, IDamageProvider
    {
        [FormerlySerializedAs("settings")] public DamageSettings DamageSettings;

        public int GetDamageAmount()
        {
            if (DamageSettings == null) return 0;
            return DamageSettings.GetDamageAmount();
        }
    }

}