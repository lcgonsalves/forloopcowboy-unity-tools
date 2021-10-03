using System;
using forloopcowboy_unity_tools.Scripts.Core;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class SoldierCard : MonoBehaviour
    {
        [Tooltip("This is the soldier instance that controls the stars, trait and name in the card, and is the instance to be deployed when the card is selected. Image must be provided separately.")]
        [OnValueChanged("OnSoldierChanged")]
        public GameLogic.Soldier soldier;

        [ValidateInput("NotNull", "Name label cannot be null.")]
        public TextMeshProUGUI soldierNameLabel;
        
        [ValidateInput("NotNull", "Trait label cannot be null.")]
        public TextMeshProUGUI traitLabel;

        [ValidateInput("NotNull", "Accuracy stars container cannot be null.")]
        [ValidateInput("HasThreeImagesAsChildren", "Accuracy stars must be in container.")]
        public Transform accuracyStarsContainer;
        
        [ValidateInput("NotNull", "Rate of fire stars container cannot be null.")]
        [ValidateInput("HasThreeImagesAsChildren", "Rate of fire stars must be in container.")]
        public Transform rateOfFireStarsContainer;
        
        [ValidateInput("NotNull", "Armor stars container cannot be null.")]
        [ValidateInput("HasThreeImagesAsChildren", "Armor stars must be in container.")]
        public Transform armorStarsContainer;

        protected void OnSoldierChanged()
        {
            // sanity check
            if (soldier.transform == null) throw new NullReferenceException("Soldier has no t defined.");

            var stats = soldier.attributes;

            soldierNameLabel.text = stats.FullName;
            traitLabel.text = stats.trait.ToString().ToHumanReadable();

            for (int i = 0; i < accuracyStarsContainer.childCount; i++)
            {
                // if accuracy is 2, only enable children 0 and 1 (first 2 stars)
                accuracyStarsContainer.GetChild(i).gameObject.SetActive(i < stats.accuracy);
            }
            
            for (int i = 0; i < rateOfFireStarsContainer.childCount; i++)
                rateOfFireStarsContainer.GetChild(i).gameObject.SetActive(i < stats.rateOfFire);

            for (int i = 0; i < armorStarsContainer.childCount; i++)
                armorStarsContainer.GetChild(i).gameObject.SetActive(i < stats.armor);

        }

        private bool NotNull(object obj) { return obj != null; }

        private bool HasThreeImagesAsChildren(Transform t)
        {
            var has3Children = t.childCount == 3;
            bool isImage = true;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                isImage &= (bool) child.GetComponent<Image>();
            }

            return has3Children && isImage;
        }

    }
}