using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    [ExecuteAlways]
    public class ProgressBar : MonoBehaviour
    {
        public Image mask;
        [SerializeField] private TextMeshProUGUI currentUI;
        [SerializeField] private TextMeshProUGUI maxUI;

        public int current;
        public int max;

        public bool isFull => current >= max;
        public bool isEmpty => current <= 0;
        public int unitsLeft => max - current;
        
        public float fill => (float) Mathf.Clamp(current, 0, max) / (float) max;

        private void Update()
        {
            UpdateFill();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (currentUI)
            {
                currentUI.text = current.ToString();
            }

            if (maxUI)
            {
                maxUI.text = max.ToString();
            }
        }

        private void UpdateFill()
        {
            mask.fillAmount = fill;
        }
    }
}