using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.Timeline;
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

            FindClosestTarget(gm, self.Value.transform.position, maximumRange.Value, gm.side.GetOpposing(), out var closestTarget);

            nearestLivingTarget?.SetValue(closestTarget?.transform);

            var ragdoll = closestTarget != null ? closestTarget.GetComponent<Ragdoll>() : null;
            
            Transform target = null;

            if (ragdoll) target = (Transform) ragdoll.neck;
            else if (closestTarget != null) target = closestTarget.transform; 

            if (nearestLivingTarget?.Value != null && (target == null || nearestLivingTarget.Value.GetInstanceID() != target.GetInstanceID()))
                gm.StopTargeting(nearestLivingTarget.Value.gameObject);

            if (target != null) 
                gm.Target(target.gameObject);
            
            nearestLivingTarget?.SetValue(target);
            distanceToTarget.SetValue(target != null ? Vector3.Distance(self.Value.transform.position, target.position) : float.PositiveInfinity);

            if (closestTarget != null) return TaskStatus.Success;
            else return TaskStatus.Failure;
        }

        /// <summary>
        /// Returns true if target exists within range.
        /// </summary>
        public static bool FindClosestTarget(GameplayManager gm, Vector3 position, float range, UnitManager.Side side, out GameObject closestTarget)
        {
            var enemies = gm.UnitManager.GetSpawned(side);

            float shortestDistance = float.PositiveInfinity;
            closestTarget = null;

            foreach (var soldier in enemies)
            {
                var hp = soldier.GetComponent<HealthComponent>();
                float distance = Vector3.Distance(position, soldier.transform.position);
                if (
                    hp &&
                    hp.IsAlive &&
                    distance <= range && distance < shortestDistance
                )
                {
                    shortestDistance = distance;
                    closestTarget = soldier;
                }
                // iterate entire list of soldiers to make sure we have the closest one.
            }

            return !(closestTarget == null);

        }

        private void InitializeGM()
        {
            gm = gameplayManager.Value != null ? gameplayManager.Value.GetComponent<GameplayManager>() : null;
        }
    }

    public static class GameplayManagerWithScanForTargets
    {
        public static bool FindClosestTarget(
            this GameplayManager gm,
            Vector3 position,
            float range,
            UnitManager.Side side,
            out GameObject closestTarget
            )
        {

            return ScanForTargets.FindClosestTarget(gm, position, range, side, out closestTarget);
        }
    }
}