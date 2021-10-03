using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.HUD;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Gameplay loop:
    /// - (every x seconds) Player receives y cards (number increases as they gain resources)
    /// - each card is a soldier
    /// - player can select soldiers to draft
    /// - clicking draft button will spawn soldiers and discard cards.
    /// - soldiers will automatically attack the closest threat and march towards the objective
    /// </summary>
    [RequireComponent(typeof(UnitManager))]
    public class GameplayManager : MonoBehaviour
    {
        public UnitManager.Side side = UnitManager.Side.Attacker;
        
        public int maxAvailableCards;
        public SoldierRandomizer soldierRandomizer;
        public ScreenRecorder screenRecorder;
        public Transform photoBoothAnchor;
        public GameObject cardPrefab;
        public HorizontalLayoutGroup cardPanel;

        private UnitManager _unitManager;
        public UnitManager UnitManager => _unitManager ? _unitManager : _unitManager = GetComponent<UnitManager>();

        /// <summary>
        /// Discards previous cards and shuffles new ones.
        /// </summary>
        [Button] public void ShuffleCards()
        {
            // generate character
            var character = soldierRandomizer.Instantiate(photoBoothAnchor, reparent: true);
            
            // instantiate the card
            var cardContainer = new GameObject("ProfileCardLayoutItem");
            cardContainer.AddComponent<LayoutElement>();
            cardContainer.transform.SetParent(cardPanel.transform);
            
            var card = Instantiate(cardPrefab, cardContainer.transform);
            var tt = card.GetComponent<TransitionTweener>();
            var cardComponent = card.GetComponent<SoldierCard>();

            cardComponent.SetSoldier(character);

            cardComponent.onClick = () =>
            {
                UnitManager.Spawn(side, character.gameObject);
            };

            // start card where the tween should start
            card.transform.localPosition = tt.useLocalPosition ? tt.@from : Vector3.zero;
            card.transform.localScale = new Vector3(1, 1, 1);
            card.transform.localRotation = Quaternion.identity;

            cardContainer.transform.localPosition = Vector3.zero;
            cardContainer.transform.localScale = new Vector3(1, 1, 1);
            cardContainer.transform.localRotation = Quaternion.identity;
            
            // todo: re-enable once we finish debugging
            // card.SetActive(false);

            // take a nice picture for the character's id
            character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("HUD"));
            screenRecorder.targetImageObject = card.transform.FindRecursively(_ => _.name == "SoldierAvatar")?.gameObject;
            screenRecorder.CreateTextureAndApplyToTargetImage();
            character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Default"));
            character.gameObject.SetActive(false);

        }
    }
}