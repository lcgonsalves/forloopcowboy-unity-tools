using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.HUD;
using UnityEngine;
using UnityEngine.Assertions;

public class HealthTracker : MonoBehaviour
{
    public GameplayManager unitManager;
    public ProgressBar progressBarPrefab;

    private void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
    }

    private void Start()
    {
        if (unitManager == null)
            unitManager = FindObjectOfType<GameplayManager>();

        if (unitManager == null) throw new NullReferenceException($"[{name}] No Unit Manager set. Please set one.");
    }

    public ProgressBar playerProgressBar; 
    private Dictionary<int, ProgressBar> progressBars = new Dictionary<int, ProgressBar>();

    public static SingletonHelper<HealthTracker> singletonHelper = new SingletonHelper<HealthTracker>();

    /// <summary>
    /// Updates heath bar on damage and death.
    /// </summary>
    public static void AssociateReactiveUpdate(HealthComponent healthComponent, bool isPlayer = false)
    {
        void DoUpdate() {
            if (isPlayer)
                UpdatePlayerProgressBar(healthComponent);
            else
                UpdateProgressbar(healthComponent);
        }

        healthComponent.onDamage += (dmg, _) => DoUpdate();
        healthComponent.onDeath += DoUpdate;

        // initialize it
        DoUpdate();
    }

    /// <summary>
    /// Updates heath bar on damage and death, and follows it's position.
    /// </summary>
    public static void AssociateReactiveUpdateAndTrack(HealthComponent healthComponent, Transform lookAt)
    {
        UpdateAndTrackProgressbar(healthComponent, lookAt);
        AssociateReactiveUpdate(healthComponent);
    }
    
    
    public static void UpdatePlayerProgressBar(HealthComponent playerHealthComponent)
    {
        var ppb = singletonHelper.Singleton.playerProgressBar;
        Assert.IsNotNull(ppb, "Trying to update player progress bar, but none has been specified.");
        
        UpdateProgressbarFromHealthComponent(playerHealthComponent, ppb);
    }
    
    public static void UpdateProgressbar(HealthComponent component)
    {
        singletonHelper.Singleton.UpdateValues(component);
    }

    public static void UpdateAndTrackProgressbar(HealthComponent component, Transform lookAt)
    {
        singletonHelper.Singleton.UpdateAndTrack(healthComponent: component, lookAt);
    }

    public void UpdateValues(HealthComponent healthComponent)
    {
        GetOrCreateHealthBar(healthComponent, (progressBar, _) => UpdateProgressbarFromHealthComponent(healthComponent, progressBar));
    }
    
    /// <summary>
    /// Gets or creates healthbar.
    /// </summary>
    /// <param name="healthComponent"></param>
    /// <param name="update">Update closure to be done to the progress bar. First param is the progress bar game object component, second is true if the progress bar is a fresh instance.</param>
    private void GetOrCreateHealthBar(HealthComponent healthComponent, Action<ProgressBar, bool> update)
    {
        var id = healthComponent.gameObject.GetInstanceID();
        bool isNewInstance = false;
        
        if (healthComponent.IsAlive)
        {
            if (!progressBars.ContainsKey(id))
            {
                progressBars.Add(id, Instantiate(progressBarPrefab, transform));
                isNewInstance = true;
            }

            // update position to follow above head.
            if (progressBars.TryGetValue(id, out var progressBar))
            {
                update(progressBar, isNewInstance);
            }
        }
        else if (progressBars.ContainsKey(id)) // && component is dead
        {

            if (progressBars.TryGetValue(id, out var progressBar))
            {
                update(progressBar, false); // expression is always false
                Destroy(progressBar.gameObject, 5f);
            }

            progressBars.Remove(id);
        }
    }
    
    private void UpdateAndTrack(HealthComponent healthComponent, Transform lookAt)
    {
        GetOrCreateHealthBar(
            healthComponent,
            (progressBar, isNewInstance) =>
            {
                // only needs to be done once
                if (isNewInstance) TrackPosition(lookAt, progressBar);
                UpdateProgressbarFromHealthComponent(healthComponent, progressBar);
            }
        );
    }

    private void TrackPosition(Transform lookAt, ProgressBar progressBar)
    {
        WorldPositionFollower follower =
            progressBar.gameObject.GetOrElseAddComponent<WorldPositionFollower>();
        
        HideBarIfTrackedTargetIsInvisible(progressBar, follower); // Coroutine

        if (follower.canvas == null) 
            follower.canvas = GetComponent<Canvas>();
        
        follower.lookAt = lookAt;
    }

    private static Coroutine HideBarIfTrackedTargetIsInvisible(ProgressBar progressBar, WorldPositionFollower follower)
    {
        return follower.RunAsync(
            () =>
            {
                if (follower.lookAtIsNotVisible) progressBar.Hide();
                else progressBar.Show();
            },
            () => progressBar == null,
            GameObjectHelpers.RoutineTypes.TimeInterval,
            0.1f // not that often but fast enough to disappear when needed without much delay.
        );
    }

    private static void UpdateProgressbarFromHealthComponent(HealthComponent healthComponent, ProgressBar progressBar)
    {
        progressBar.max = healthComponent.MaxHealth;
        progressBar.current = healthComponent.Health;
    }
}
