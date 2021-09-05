using UnityEngine;
using UnityEngine.InputSystem;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    public class InteractionBehaviour : MonoBehaviour
    {
        // inputs

        public LayerMask interactLayer;

        public float interactDistance;

        // cached references
        PlayerControls controls;

        Camera mainCamera;

        private void OnEnable() {
        
            controls = new PlayerControls();

            controls.Default.Interact.Enable();
            controls.Default.Interact.performed += TryInteraction;

        }

        private void Start() {
            mainCamera = Camera.main;
        }

        private void TryInteraction(InputAction.CallbackContext ctx) {

            RaycastHit hit;
            Ray rayFromCenterOfScreen = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

            // raycast outwards from screen
            if (Physics.Raycast(rayFromCenterOfScreen, out hit, interactDistance, interactLayer)) {

                hit.transform.gameObject.GetComponent<InteractableObject>()?.Interact();

            }

        }

        private void OnGUI() {
        
        }

    }
}
