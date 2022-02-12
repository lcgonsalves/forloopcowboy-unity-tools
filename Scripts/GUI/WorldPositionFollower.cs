using System;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    [ExecuteAlways]
    public class WorldPositionFollower : MonoBehaviour
    {
        [Header("Tweaks")] 
        public Transform lookAt;
        public Vector3 offset;
        public Canvas canvas;

        // //

        private Camera cam;
        private RectTransform rt;
        
        public bool lookAtIsNotVisible => lookAt && Vector3.Dot(lookAt.position - cam.transform.position, cam.transform.forward) < 0;
        public bool lookAtIsVisible => !lookAtIsNotVisible;

        private void Start()
        {
            cam = canvas.worldCamera ? canvas.worldCamera : Camera.main;
            rt = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (!lookAt) return;

            var cameraTransform = cam.transform;

            if (lookAtIsNotVisible) return;
            
            Vector2 adjustedPosition = cam.WorldToScreenPoint(lookAt.position) + offset;

            var canvasRT = canvas.GetComponent<RectTransform>();
            var rect = canvasRT.rect;
            adjustedPosition.x *= rect.width / (float)cam.pixelWidth;
            adjustedPosition.y *= rect.height / (float)cam.pixelHeight;
 
            // set it
            rt.anchoredPosition = adjustedPosition - canvasRT.sizeDelta / 2f;
        }
    }
}