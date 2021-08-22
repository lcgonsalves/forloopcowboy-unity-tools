using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class DebugUtil : MonoBehaviour
    {
        public InputActionReference reloadLevel;

        private void OnEnable()
        {
            reloadLevel.action.Enable();
            reloadLevel.action.performed += ctx =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
        }
    }
}