using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    [RequireComponent(typeof(AdvancedPlayerMovementBehaviour))]
    public class PlayerComponent : MonoBehaviour
    {
        public AdvancedPlayerMovementBehaviour movement => _movement == null ? _movement = GetComponent<AdvancedPlayerMovementBehaviour>() : _movement;
        private AdvancedPlayerMovementBehaviour _movement;
    }
}