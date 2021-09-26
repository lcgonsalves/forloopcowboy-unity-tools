using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class TweenOnHover : MonoBehaviour
    {
        [SerializeField] private TransitionTweener t;
        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private List<GameObject> raycastTargets;

        [SerializeField] private bool UseTweenPreset;
        [SerializeField] private string TweenPresetName;

        private PointerEventData data;
        private List<RaycastResult> hoverResults = new List<RaycastResult>(10);
        
        private void Start()
        {
            t = t ? t : GetComponent<TransitionTweener>();
            raycaster = raycaster ? raycaster : GetComponent<GraphicRaycaster>();
            
            data = new PointerEventData(EventSystem.current);
            
            if (EventSystem.current.TryGetComponent<InputSystemUIInputModule>(out var ui))
            {
                Action<InputAction.CallbackContext> onMouseMove = ctx => UpdateIsHovering();
                ui.point.action.performed += onMouseMove;
            }
        }

        void UpdateIsHovering()
        {
            data.position = Mouse.current.position.ReadValue();

            hoverResults.Clear();
            if (raycaster) raycaster.Raycast(data, hoverResults);
            
            var hovering = false;
            var highestIndex = 0;
            RaycastResult? resultWithHighestIndex = null;
            foreach (var raycastResult in hoverResults)
            {
                // result is child of card, which is child of card container, whose index we want to look at
                // HorizontalLayout > CardContainer > Card > RaycastTargets
                var containerIndex = raycastResult.gameObject.transform.parent.parent.GetSiblingIndex();
                if (containerIndex >= highestIndex)
                {
                    highestIndex = containerIndex;
                    resultWithHighestIndex = raycastResult;
                }
            }
            if (resultWithHighestIndex != null && raycastTargets.Contains(resultWithHighestIndex?.gameObject))
            {
                hovering = true;
            }

            isHovering = hovering;
        }

        private bool _isHovering = false;

        private bool isHovering
        {
            get => _isHovering;
            set
            {

                if (value != _isHovering)
                {
                    if (UseTweenPreset)
                    {
                        if (value) t.Tween(TweenPresetName, true);
                        else t.Untween(TweenPresetName, true);
                    }
                    else
                    {
                        if (value) t.Tween(true);
                        else t.Untween(true);
                    }
                }

                _isHovering = value;
            }
        }

    }
}