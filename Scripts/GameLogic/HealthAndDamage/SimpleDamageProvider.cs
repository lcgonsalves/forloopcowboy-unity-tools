using System;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

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