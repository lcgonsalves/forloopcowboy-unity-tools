using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class SoldierCardUIComponent : MonoBehaviour
    {
        private TransitionTweener t;
        public InputSystemUIInputModule ui;

        private void Start()
        {
            t = GetComponent<TransitionTweener>();
            t = t ? t : transform.parent.GetComponent<TransitionTweener>();
            
            ui = GameObject.FindObjectOfType<InputSystemUIInputModule>();

            RectTransform rt = GetComponent<RectTransform>();
            Camera main = Camera.main;
            
            // ui.point.action.performed += context =>
            // {
            //     var pos = context.ReadValue<Vector2>();
            //     rt.position = pos;
            // };

        }

        private bool _isHovering = false;

        private bool isHovering
        {
            get => _isHovering;
            set
            {

                if (value != _isHovering)
                {
                    if (value) OnMouseOver();
                    else OnMouseExit();
                }

                _isHovering = value;
            }
        }

        private void OnMouseOver()
        {
            Debug.Log(" hello entere");
            t.Tween(true);
        }

        private void OnMouseExit()
        {
            Debug.Log(" hello exit");
            t.Untween(true);
        }

        private void OnDrawGizmos()
        {
        }
    }
}