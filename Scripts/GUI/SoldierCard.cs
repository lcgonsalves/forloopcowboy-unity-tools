using System;
using System.Security.Cryptography;
using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class SoldierCard : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("This is the soldier instance that controls the stars, trait and name in the card, and is the instance to be deployed when the card is selected. Image must be provided separately.")]
        [OnValueChanged("OnSoldierChanged")]
        [SerializeField] public GameLogic.Soldier soldier;

        [ValidateInput("NotNull", "Name label cannot be null.")]
        public TextMeshProUGUI soldierNameLabel;
        
        [ValidateInput("NotNull", "Name label cannot be null.")]
        public TextMeshProUGUI ratingLabel;
        
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

        [ValidateInput("NotNull", "Drafted stamp cannot be null.")]
        public Transform draftedStamp;

        [CanBeNull] public TransitionTweener tweener => GetComponent<TransitionTweener>();
        
        public void SetSoldier(GameLogic.Soldier newSoldier)
        {
            soldier = newSoldier;
            OnSoldierChanged();
        }        
        
        protected void OnSoldierChanged()
        {
            // sanity check
            if (soldier.transform == null) throw new NullReferenceException("Soldier has no t defined.");

            var stats = soldier.attributes;

            soldierNameLabel.text = stats.FullName;
            traitLabel.text = stats.trait.ToString().ToHumanReadable();

            string equivalentRating;
            var maximumPossibleStars = 9;
            var totalStars = stats.accuracy + stats.armor + stats.rateOfFire;
            var highestProficiency = Mathf.Max(stats.accuracy, stats.armor, stats.rateOfFire);

            var score = totalStars + highestProficiency;

            if (score < 3) equivalentRating = "F";
            else if (score < 4) equivalentRating = "E";
            else if (score < 5) equivalentRating = "D";
            else if (score < 6) equivalentRating = "C";
            else if (score < 8) equivalentRating = "B";
            else if (score < 10) equivalentRating = "A";
            else equivalentRating = "S++";

            ratingLabel.text = equivalentRating;

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

        private bool NotNull(object obj) { return obj == null; }

        private bool HasThreeImagesAsChildren(Transform t)
        {
            var has3Children = t.childCount == 3;
            bool isImage = true;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                isImage &= (bool) child.GetComponent<Image>();
            }

            return !(has3Children && isImage);
        }


        public Action onClick = () => Debug.LogWarning("No click handlers assigned.");
        
        public void OnPointerClick(PointerEventData eventData)
        {
            draftedStamp.gameObject.SetActive(!draftedStamp.gameObject.activeInHierarchy);
            onClick();
        }

        public bool IsDrafted => draftedStamp != null && draftedStamp.gameObject.activeInHierarchy;

        /// <summary>
        /// Destroys container, layout element, and object cleanly.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="destroyAssociatedSoldier">When true also destroys the soldier instance. To be used when soldier hasn't been spawned yet.</param>
        public static void SafeDestroy(SoldierCard card, bool destroyAssociatedSoldier = false)
        {
            if (Application.isEditor)
            {
                if (card.transform.parent)
                {
                    var parent = card.transform.parent;
                    DestroyImmediate(parent.GetComponent<LayoutElement>());
                    DestroyImmediate(parent.gameObject);
                    if (destroyAssociatedSoldier && card.soldier.gameObject != null) DestroyImmediate(card.soldier.gameObject);
                }
            }
            else
            {
                if (card.transform.parent)
                {
                    var parent = card.transform.parent;
                    Destroy(parent.GetComponent<LayoutElement>());
                    Destroy(parent.gameObject);
                }
                Destroy(card.gameObject);
                if (destroyAssociatedSoldier && card.soldier.gameObject != null) DestroyImmediate(card.soldier.gameObject);
            }
        }
        
    }
}