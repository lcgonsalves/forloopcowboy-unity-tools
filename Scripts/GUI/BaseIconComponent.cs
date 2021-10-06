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

[RequireComponent(typeof(WorldPositionFollower)), ExecuteAlways]
public class BaseIconComponent : SerializedMonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI baseName, basePopulation;
    public DeployableBuilding building;
    public GameplayManager gameplayManager;
    
    public void Start()
    {
        gameplayManager = GameObject.FindObjectOfType<GameplayManager>();
        
        baseName.text = building.BuildingName;
        basePopulation.text = $"{building.CurrentOccupants}/{building.MaximumOccupants}";

        var positionFollower = GetComponent<WorldPositionFollower>();
        positionFollower.lookAt = building.transform;

        building.occupantsChanged += newNumberOfOCuupants => basePopulation.text = $"{newNumberOfOCuupants}/{building.MaximumOccupants}";
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
