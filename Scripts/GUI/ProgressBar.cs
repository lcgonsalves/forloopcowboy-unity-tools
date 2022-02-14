using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
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

        private Cache<WorldPositionFollower> _worldPositionFollower = null;
        
        [CanBeNull]
        public WorldPositionFollower GetPositionFollower
        {
            get
            {
                // Cached getter for position follower, if one exists. If none exists,
                // get component is called every time.
                // AKA efficient for getting as long as you expect it to be there, otherwise same
                // efficiency as GetComponent. 
                if (_worldPositionFollower == null)
                    _worldPositionFollower = new Cache<WorldPositionFollower>(GetComponent<WorldPositionFollower>);

                return _worldPositionFollower.Get;
            }
        }

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

        public IEnumerable<Image> GetImages
        {
            get
            {
                var images = new List<Image>();
                if (TryGetComponent(out Image imageThis)) images.Add(imageThis);
                images.AddRange(
                    GetComponentsInChildren<Image>()
                );

                return images;
            }
        }

        public void Toggle(bool value)
        {
            foreach (var image in GetImages) image.enabled = value;
            
            if (currentUI) currentUI.gameObject.SetActive(value);
            if (maxUI) maxUI.gameObject.SetActive(value);
        }

        public void Show() => Toggle(true);
        public void Hide() => Toggle(false);
    }
}