using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

/// <summary>
/// Task that plays a given transition, setting a shared variable's value
/// and executing a child task until it finishes the animation.
/// </summary>
[TaskCategory("Common")]
public class TransitionTask : BehaviorDesigner.Runtime.Tasks.Composite
{
    public SharedFloat currentValue = 0f;
    public Transition transition;

    public float updateIntervalInSeconds = 0f;
    public bool useFixedDeltaTime = false;
    public bool useDeltaTime = false;

    private bool transitionIsRunning = false;

    public override void OnStart()
    {
        transitionIsRunning = true;

        float interval;
        if (useFixedDeltaTime) interval = Time.fixedDeltaTime;
        else if (useDeltaTime) interval = Time.deltaTime;
        else interval = updateIntervalInSeconds;
            
        transition.PlayOnce(
            this.Owner,
            onUpdateState =>
            {
                currentValue.SetValue(onUpdateState.Snapshot());
            },
            finishState =>
            {
                currentValue.SetValue(finishState.Snapshot());
                transitionIsRunning = false;
            },
            interval
        );
    }

    public override TaskStatus OverrideStatus(TaskStatus status)
    {
        if (transitionIsRunning) return TaskStatus.Running;
        else return TaskStatus.Success;
    }
}