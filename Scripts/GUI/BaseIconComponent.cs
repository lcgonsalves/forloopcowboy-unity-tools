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

        if (building.MaximumOccupants > building.CurrentOccupants)
        {
            var draftedSoldiers = gameplayManager.draftedCards;
            var availableSpawns = building.GetAvailableSpawnPoints();

            for (int i = 0; i < Mathf.Min(draftedSoldiers.Length, availableSpawns.Length); i++)
            {
                var soldier = draftedSoldiers[i];
                var spawn = availableSpawns[i];
                
                gameplayManager.UnitManager.Spawn(soldier.soldier.gameObject, spawn, gameplayManager.side);
            }
            
        }
    }

}
