using forloopcowboy_unity_tools.Scripts.GameLogic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    [RequireComponent(typeof(DeprecatedAdvancedPlayerMovementBehaviour))]
    public class PlayerComponent : MonoBehaviour
    {
        public UnitManager.Side side;
        
        public DeprecatedAdvancedPlayerMovementBehaviour movement => _movement == null ? _movement = GetComponent<DeprecatedAdvancedPlayerMovementBehaviour>() : _movement;
        private DeprecatedAdvancedPlayerMovementBehaviour _movement;

        public HealthComponent healthComponent => _healthComponent == null ? _healthComponent = GetComponent<HealthComponent>() : _healthComponent;
        private HealthComponent _healthComponent;
    }
}