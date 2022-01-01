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

    private void Start()
    {
        if (unitManager == null)
            unitManager = FindObjectOfType<GameplayManager>();

        if (unitManager == null) throw new NullReferenceException($"[{name}] No Unit Manager set. Please set one.");
    }

    public ProgressBar playerProgressBar; 
    private Dictionary<int, ProgressBar> progressBars = new Dictionary<int, ProgressBar>();

    private void Update()
    {
        foreach (var ally in unitManager.GetAllies())
        {
            if (ally.TryGetComponent(out HealthComponent healthComponent))
            {
                UpdateAndTrack(healthComponent);
            }
        }
    }

    public static SingletonHelper<HealthTracker> singletonHelper = new SingletonHelper<HealthTracker>();

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

    public void UpdateValues(HealthComponent healthComponent)
    {
        Update(healthComponent, progressBar => UpdateProgressbarFromHealthComponent(healthComponent, progressBar));
    }
    
    public void Update(HealthComponent healthComponent, Action<ProgressBar> update)
    {
        var id = healthComponent.gameObject.GetInstanceID();
        if (healthComponent.IsAlive)
        {
            if (!progressBars.ContainsKey(id))
            {
                progressBars.Add(id, Instantiate(progressBarPrefab, transform));
            }

            // update position to follow above head.
            if (progressBars.TryGetValue(id, out var progressBar))
            {
                update(progressBar);
            }
        }
        else if (progressBars.ContainsKey(id)) // && component is dead
        {
            if (progressBars.TryGetValue(id, out var progressBar))
            {
                Destroy(progressBar.gameObject, 5f);
            }

            progressBars.Remove(id);
        }
    }
    
    private void UpdateAndTrack(HealthComponent healthComponent)
    {
        var id = healthComponent.gameObject.GetInstanceID();
        if (healthComponent.IsAlive)
        {
            if (!progressBars.ContainsKey(id))
            {
                progressBars.Add(id, Instantiate(progressBarPrefab, transform));
            }

            // update position to follow above head.
            if (progressBars.TryGetValue(id, out var progressBar))
            {
                UpdateProgressbarFromHealthComponent(healthComponent, progressBar);
                TrackPosition(healthComponent, progressBar);
            }
        }
        else if (progressBars.ContainsKey(id)) // && component is dead
        {
            if (progressBars.TryGetValue(id, out var progressBar))
            {
                Destroy(progressBar.gameObject, 5f);
            }

            progressBars.Remove(id);
        }
    }

    private void TrackPosition(HealthComponent healthComponent, ProgressBar progressBar)
    {
        WorldPositionFollower follower =
            progressBar.gameObject.GetOrElseAddComponent<WorldPositionFollower>();

        if (follower.canvas == null) 
            follower.canvas = GetComponent<Canvas>();
        
        if (follower.lookAt.GetInstanceID() != healthComponent.gameObject.GetInstanceID()) 
            follower.lookAt = healthComponent.transform;
    }

    private static void UpdateProgressbarFromHealthComponent(HealthComponent healthComponent, ProgressBar progressBar)
    {
        progressBar.max = healthComponent.MaxHealth;
        progressBar.current = healthComponent.Health;
    }
}
