using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.HUD;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
        public List<SoldierRandomizer> soldierRandomizers;
        public ScreenRecorder screenRecorder;
        public Transform photoBoothAnchor;
        public GameObject cardPrefab;
        public HorizontalLayoutGroup cardPanel;
        public float delayBetweenShuffle = 1.2f;

        private UnitManager _unitManager;
        public UnitManager UnitManager => _unitManager ? _unitManager : _unitManager = GetComponent<UnitManager>();

        public SoldierCard RandomizeCard()
        {
            // generate character
            var character = soldierRandomizers[Random.Range(0, soldierRandomizers.Count)].Instantiate(photoBoothAnchor, reparent: true);
            
            // instantiate the card
            var cardContainer = new GameObject("ProfileCardLayoutItem");
            cardContainer.AddComponent<LayoutElement>();
            cardContainer.transform.SetParent(cardPanel.transform);
            
            var card = Instantiate(cardPrefab, cardContainer.transform);
            var tt = card.GetComponent<TransitionTweener>();
            var cardComponent = card.GetComponent<SoldierCard>();

            cardComponent.SetSoldier(character);

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

            return cardComponent;
        }

        private List<SoldierCard> activeCards = new List<SoldierCard>();

        public SoldierCard[] draftedCards => activeCards.Where(_ => _.IsDrafted).ToArray();
        
        /// <summary>
        /// Discards previous cards and shuffles new ones.
        /// </summary>
        [Button] public void ShuffleCards()
        {
            // todo: deactivate cards properly
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    var cardTransform = card.transform.localPosition; // because i configured the prefab to slerp locally
                    if (card.tweener is { })
                    {
                        card.tweener.SlerpTo(new Vector3(cardTransform.x, -300f, cardTransform.z), 0.5f);
                        this.RunAsyncWithDelay(0.7f, () => SoldierCard.SafeDestroy(card));
                    }
                }
            }
            
            activeCards.Clear();

            // wait until cards have been yeeted
            this.RunAsyncWithDelay(0.8f, () =>
            {

                for (int i = 0; i < maxAvailableCards; i++)
                {
                    this.RunAsyncWithDelay(delayBetweenShuffle * i, () =>
                    {
                        // todo: tween cards in
                        var card = RandomizeCard();
                        if (card is { } && card.tweener is { })
                        {
                            card.tweener.Tween("OnCreation", true);
                            activeCards.Add(card);
                        }
                    });
                }
            });

        }
    }
}