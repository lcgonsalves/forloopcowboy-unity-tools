using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class TweenOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TransitionTweener t;

        [SerializeField] private bool UseTweenPreset;
        [SerializeField] private string TweenPresetName;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (UseTweenPreset) t.Tween(TweenPresetName, true);
            else t.Tween(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (UseTweenPreset) t.Untween(TweenPresetName, true);
            else t.Untween(true);
        }
    }
}