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
        
        private void Start()
        {
            cam = canvas.worldCamera ?? Camera.main;
            rt = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (!lookAt) return;

            // fuck you unity and your black magic to perform such a basic op using a built in system
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