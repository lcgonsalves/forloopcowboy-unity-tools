using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.HUD;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScaleTweener)), ExecuteAlways]
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

        if (baseCapacity) baseCapacity.text = building.MaximumOccupants.ToString();
        if (baseName) baseName.text = building.BuildingName;
        if (basePopulation) basePopulation.text = building.CurrentOccupants.ToString();

        var positionFollower = GetComponent<WorldPositionFollower>();
        if (positionFollower) positionFollower.lookAt = building.transform;

        building.occupantsChanged += _ =>
        {
            // use current occupants instead of event value to preserve continuity
            if (basePopulation) basePopulation.text = building.CurrentOccupants.ToString();
            if (baseCapacity) baseCapacity.text = building.MaximumOccupants.ToString();
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
            if (building.MaximumOccupants > building.CurrentOccupants)
            {
                var draftedSoldiers = gameplayManager.draftedCards;
                if (draftedSoldiers.Length == 0) return;
                
                var availableSpawns = building.GetAvailableSpawnPoints();

                // if singleton send back to the list.
                availableSpawns.Sort((a, b) =>
                {
                    if (a.singleton && b.singleton) return 0;
                    if (a.singleton) return 1;
                    else return -1;
                });
                
                bool hasANonSingleton = availableSpawns.Exists(_ => !_.singleton);
                
                // basically we only care about the soldiers selected if we don't have singleton limitations.
                var numberOfSoldiersToSelect = hasANonSingleton
                    ? draftedSoldiers.Length
                    : Mathf.Min(draftedSoldiers.Length, availableSpawns.Count);
                
                var spawnedSoldierCards = new SoldierCard[numberOfSoldiersToSelect];

                int j = 0;
                for (
                    int i = 0;
                    i < numberOfSoldiersToSelect;
                    i++
                ) {
                    var card = draftedSoldiers[i];
                    var spawn = availableSpawns[j];

                    // if non singleton we can spawn as many as we want. hence why they're sorted to the end. 
                    // want two singletons and choose between them instead of the first one blocking out the second?
                    // make two buildings. sorry
                    if (spawn.singleton) j++;

                    gameplayManager.UnitManager.Spawn(card.soldier.gameObject, spawn.node, gameplayManager.side);
                    
                    spawnedSoldierCards[i] = card;
                }

                gameplayManager.DisposeCards(spawnedSoldierCards);

            }
        });
    }

}
