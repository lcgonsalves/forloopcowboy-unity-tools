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

        // //

        private Camera cam;
        
        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            var pos = cam.WorldToScreenPoint(lookAt.position + offset);
            if (transform.position != pos)
                transform.position = pos;
        }
    }
}