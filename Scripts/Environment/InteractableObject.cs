using UnityEngine;

public class InteractableObject : MonoBehaviour
{

    public void Interact() {
        
        var animator = GetComponent<Animator>();
        var currentAnimatorState = animator?.GetBool("open") ?? false;

        animator?.SetBool("open", !currentAnimatorState);

    } 

}
