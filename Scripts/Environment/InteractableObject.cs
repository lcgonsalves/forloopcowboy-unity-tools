using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    public class InteractableObject : MonoBehaviour
    {

        public void Interact() {
        
            var animator = GetComponent<Animator>();
            var currentAnimatorState = animator?.GetBool("open") ?? false;

            animator?.SetBool("open", !currentAnimatorState);

        } 

    }
}
