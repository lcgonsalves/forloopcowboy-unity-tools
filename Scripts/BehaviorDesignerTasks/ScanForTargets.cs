using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks
{
    /// <summary>
    /// Scan for targets is successful when there is an enemy soldier
    /// who is within 'maximumRange' and is 'alive'. If no GameplayManager is found,
    /// or no soldiers are found, task Fails.
    /// </summary>
    [TaskCategory("Custom")]
    public class ScanForTargets : Action
    {
        public SharedGameObject self;
        public SharedGameObject gameplayManager;
        public SharedTransform nearestLivingTarget;
        public SharedFloat maximumRange = 10f;
        public SharedFloat distanceToTarget;
        private GameplayManager gm;

        public override void OnAwake()
        {
            InitializeGM();
        }

        public override void OnStart()
        {
            InitializeGM();
        }

        public override TaskStatus OnUpdate()
        {
            if (gm == null) return TaskStatus.Failure;
            var enemies = gm.GetUntargetedEnemies();

            float shortestDistance = float.PositiveInfinity;
            GameObject closestTarget = null;
            
            foreach (var soldier in enemies)
            {
                var hp = soldier.GetComponent<HealthComponent>();
                float distance = Vector3.Distance(self.Value.transform.position, soldier.transform.position);
                if (
                    hp &&
                    hp.IsAlive &&
                    distance <= maximumRange.Value && distance < shortestDistance
                )
                {
                    shortestDistance = distance;
                    closestTarget = soldier;
                }
                // iterate entire list of soldiers to make sure we have the closest one.
            }
            
            var ragdoll = closestTarget != null ? closestTarget.GetComponent<Ragdoll>() : null;
            
            Transform target = null;

            if (ragdoll) target = ragdoll.neck;
            else if (closestTarget != null) target = closestTarget.transform; 

            if (nearestLivingTarget.Value != null && (target == null || nearestLivingTarget.Value.GetInstanceID() != target.GetInstanceID()))
                gm.StopTargeting(nearestLivingTarget.Value.gameObject);

            if (target != null) 
                gm.Target(target.gameObject);
            
            nearestLivingTarget.SetValue(target);
            distanceToTarget.SetValue(shortestDistance);

            if (closestTarget != null) return TaskStatus.Success;
            else return TaskStatus.Failure;
        }

        private void InitializeGM()
        {
            gm = gameplayManager.Value != null ? gameplayManager.Value.GetComponent<GameplayManager>() : null;
        }
    }
}