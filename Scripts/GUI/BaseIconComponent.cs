using System;
using System.Collections;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.HUD;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(WorldPositionFollower)), ExecuteAlways]
public class BaseIconComponent : SerializedMonoBehaviour
{
    public TextMeshProUGUI baseName, basePopulation;
    public DeployableBuilding building;

    public void Start()
    {
        baseName.text = building.BuildingName;
        basePopulation.text = $"{building.CurrentOccupants}/{building.MaximumOccupants}";

        var positionFollower = GetComponent<WorldPositionFollower>();
        positionFollower.lookAt = building.transform;

        building.occupantsChanged += newNumberOfOCuupants => basePopulation.text = $"{newNumberOfOCuupants}/{building.MaximumOccupants}";
    }
}
