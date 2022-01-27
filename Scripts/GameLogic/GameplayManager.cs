using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.HUD;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    public class GameplayManager : SerializedMonoBehaviour
    {
        public UnitManager.Side side = UnitManager.Side.Attacker;
        
        [SerializeField]
        private UnitManager _unitManager;

        [TabGroup("Player Settings"), Tooltip("Where player 1 is player uhh 1")]
        public PlayerComponent[] players;

        [TabGroup("Player Settings"), Tooltip("Which camera follows each player. 1-1 Match based on index.")] 
        public CinemachineVirtualCamera[] virtualCamera;
        
        [TabGroup("Soldier Spawn Cards")]
        public bool spawnCardsEnabled = false;
        
        [TabGroup("Soldier Spawn Cards")]
        public int maxAvailableCards;
        
        [TabGroup("Soldier Spawn Cards")]
        public List<SoldierRandomizer> soldierRandomizers;
        
        [TabGroup("Soldier Spawn Cards")]
        public ScreenRecorder screenRecorder;
        
        [TabGroup("Soldier Spawn Cards")]
        public Transform photoBoothAnchor;
        
        [TabGroup("Soldier Spawn Cards")]
        public GameObject cardPrefab;
        
        [TabGroup("Soldier Spawn Cards")]
        public HorizontalLayoutGroup cardPanel;
        
        [TabGroup("Soldier Spawn Cards")]
        public float delayBetweenShuffle = 1.2f;
        
        public UnitManager UnitManager => _unitManager ? _unitManager : _unitManager = GetComponent<UnitManager>();
        
        /// <returns>All objects spawned on the opposing side.</returns>
        public GameObject[] GetEnemies() => UnitManager.GetSpawned(UnitManager.GetOpposing(side));
        
        /// <returns>All objects spawned on the friendly side.</returns>
        public GameObject[] GetAllies() => UnitManager.GetSpawned(side);

        /// <summary>
        /// Tracks game objects that are being targeted.
        /// </summary>
        private HashSet<int> _targetedGameObjectIDs = new HashSet<int>();

        public GameObject[] GetUntargetedEnemies()
        {
            return GetEnemies().Where(_ => !_targetedGameObjectIDs.Contains(_.GetInstanceID())).ToArray();
        }

        public void Target(GameObject enemy) => _targetedGameObjectIDs.Add(enemy.GetInstanceID());
        public void StopTargeting(GameObject enemy) => _targetedGameObjectIDs.Remove(enemy.GetInstanceID());

        private void Awake()
        {
            if (spawnCardsEnabled) ShuffleCards();
        }

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
            var hasActiveCards = activeCards.Count > 0;
            if (hasActiveCards) DisposeCards(activeCards, disposeOfSoldiers: true);
            
            activeCards.Clear();

            // wait until cards have been yeeted
            this.RunAsyncWithDelay(hasActiveCards ? 0.8f : 0, () =>
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

        [Button(ButtonStyle.CompactBox), TabGroup("Player Settings")]
        public void SpawnPlayer(int index, UnitManager.Side side)
        {
            var obj = UnitManager.SpawnCopy(side, players[index].gameObject);
            var vcam = virtualCamera[index];
            var playerComponent = obj.gameObject.GetOrElseAddComponent<PlayerComponent>();
            var playerHealth = playerComponent.healthComponent;

            this.RunAsyncWithDelay(0.1f, () => HealthTracker.UpdatePlayerProgressBar(playerHealth));
            playerHealth.onDamage += dmg => HealthTracker.UpdatePlayerProgressBar(playerHealth);
            playerComponent.side = obj.side;

            // virtual head is what rotates vertically for the camera
            vcam.Follow = obj.gameObject.transform.GetChild(0);
            
            // TODO: camera should follow local player only (when this is networked)
            var mainCam = Camera.main.GetUniversalAdditionalCameraData();
            var overlayCam = obj.gameObject.GetComponentInChildren<Camera>();

            mainCam.cameraStack.Add(overlayCam);
        }
        
        /// <summary>
        /// Lerps card and destroys card/associated soldier.
        /// </summary>
        /// <param name="cardsToDispose"></param>
        /// <param name="disposeOfSoldiers"></param>
        public void DisposeCards(IEnumerable<SoldierCard> cardsToDispose, bool disposeOfSoldiers = false)
        {
            foreach (var card in cardsToDispose)
            {
                if (card != null)
                {
                    if (activeCards.Contains(card)) activeCards.Remove(card);
                    
                    var cardTransform = card.transform.localPosition; // because i configured the prefab to slerp locally
                    if (card.tweener is { })
                    {
                        card.tweener.SlerpTo(new Vector3(cardTransform.x, -300f, cardTransform.z), 0.5f);
                        this.RunAsyncWithDelay(0.7f, () => SoldierCard.SafeDestroy(card, disposeOfSoldiers));
                    }
                }
            }
        }
        
        public void DisposeCards(SoldierCard card, bool disposeOfSoldiers = false) { DisposeCards(new []{card}, disposeOfSoldiers); }
    }
}