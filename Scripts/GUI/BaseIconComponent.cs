using System;
using System.Collections;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.HUD;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(WorldPositionFollower)), RequireComponent(typeof(ScaleTweener)), ExecuteAlways]
public class BaseIconComponent : SerializedMonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI baseName, basePopulation, baseCapacity;
    public DeployableBuilding building;
    public GameplayManager gameplayManager;
    
    [FoldoutGroup("Grow On Hover")]
    public Transition scaleTweenTransition;

    [FoldoutGroup("Grow On Hover")]
    public Vector3 initialScale = new Vector3(1, 1, 1);
    
    [FoldoutGroup("Grow On Hover")]
    public Vector3 hoverScale = new Vector3(1, 1, 1);

    [FoldoutGroup("Grow On Hover")] public float lerpDuration = 0.3f;

    [FoldoutGroup("Grow On Hover")]
    public Transform scaleTarget;

    private SpamProtection sp = new SpamProtection(0.5f);
    
    public void Start()
    {
        gameplayManager = GameObject.FindObjectOfType<GameplayManager>();
        
        baseName.text = building.BuildingName;
        basePopulation.text = building.CurrentOccupants.ToString();
        baseCapacity.text = building.MaximumOccupants.ToString();

        var positionFollower = GetComponent<WorldPositionFollower>();
        positionFollower.lookAt = building.transform;

        building.occupantsChanged += newNumberOfOCuupants =>
        {
            basePopulation.text = newNumberOfOCuupants.ToString();
            baseCapacity.text = building.MaximumOccupants.ToString();
        };

        var scaleTweener = this.GetOrElseAddComponent<ScaleTweener>();
        scaleTweener.hoverScale = hoverScale;
        scaleTweener.initialScale = initialScale;
        scaleTweener.lerpDuration = lerpDuration;
        scaleTweener.scaleTarget = scaleTarget;
        scaleTweener.scaleTweenTransition = scaleTweenTransition;
        scaleTweener.enableOnHover = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // todo: show hint that house is full
        sp.SafeExecute(() =>
        {
            Debug.Log("function ran!");
            if (building.MaximumOccupants > building.CurrentOccupants)
            {
                var draftedSoldiers = gameplayManager.draftedCards;
                if (draftedSoldiers.Length == 0) return;
                
                var availableSpawns = building.GetAvailableSpawnPoints();

                var spawnedSoldierCards = new SoldierCard[Mathf.Min(draftedSoldiers.Length, availableSpawns.Count)];

                for (int i = 0; i < Mathf.Min(draftedSoldiers.Length, availableSpawns.Count); i++)
                {
                    var card = draftedSoldiers[i];
                    var spawn = availableSpawns[i];

                    gameplayManager.UnitManager.Spawn(card.soldier.gameObject, spawn, gameplayManager.side);
                    
                    spawnedSoldierCards[i] = card;
                }

                gameplayManager.DisposeCards(spawnedSoldierCards);

            }
        });
    }

}
