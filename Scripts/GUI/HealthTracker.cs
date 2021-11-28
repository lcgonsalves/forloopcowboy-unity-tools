using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.HUD;
using UnityEngine;

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

    private Dictionary<int, ProgressBar> progressBars = new Dictionary<int, ProgressBar>();

    private void Update()
    {
        foreach (var ally in unitManager.GetAllies())
        {
            var id = ally.GetInstanceID();
            if (ally.TryGetComponent(out HealthComponent healthComponent))
            {
                
                if (healthComponent.IsAlive)
                {
                    if (!progressBars.ContainsKey(id))
                    {
                        progressBars.Add(id, Instantiate(progressBarPrefab, transform));
                    }
                    
                    // update position to follow above head.
                    if (progressBars.TryGetValue(id, out var progressBar))
                    {
                        progressBar.max = healthComponent.MaxHealth;
                        progressBar.current = healthComponent.Health;

                        WorldPositionFollower follower =
                            progressBar.gameObject.GetOrElseAddComponent<WorldPositionFollower>();

                        if (follower.canvas == null) follower.canvas = GetComponent<Canvas>();
                        
                        if (follower.lookAt.GetInstanceID() != id) follower.lookAt = ally.transform;
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
        }
    }
}
