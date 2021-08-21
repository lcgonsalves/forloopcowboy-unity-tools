using System;
using UnityEngine;

namespace ForLoopCowboyCommons.Agent.CustomOrders
{
    /// <summary>
    /// Exposes functions to control the soldier.
    /// </summary>
    [System.Serializable]
    public class SoldierControlStep : Order.AsyncActionStep
    {
        /// <summary>
        /// Exposes functions to the editor. Each function is a different
        /// action or set of actions to be called on the soldier.
        /// </summary>
        public enum ControlOptions
        {
            FollowNearestPath,
            Idle
        }

        public ControlOptions actionType = ControlOptions.FollowNearestPath;

        public FollowPathCommandSettings followPathSettings = new FollowPathCommandSettings();

        private void DoIfAgentIsSoldier(Agent agent, Action<SoldierBehaviour> componentCallback)
        {
            SoldierBehaviour component = agent.gameObject.GetComponent<SoldierBehaviour>();
            if (component == null)
            {
                Debug.LogError(
                    $"Agent is not a soldier! Please attach the {typeof(SoldierBehaviour)} script.");
                return;
            }

            componentCallback(component);
        }
        
        public SoldierControlStep()
        {
            switch (actionType)
            {
                case ControlOptions.FollowNearestPath:
                    callbackWithTerminator = (agent, terminate) => 
                        DoIfAgentIsSoldier(agent, c => FindNearestPathAndFollowIt(c, terminate));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void FindNearestPathAndFollowIt(SoldierBehaviour soldier, Action terminate)
        {
            var nodes = soldier.navigation.GetNearbyWaypointNodes(100f, 1);
            var node = nodes.Length > 0 ? nodes[0] : null;
            if (node != null)
            {
                if (followPathSettings.followUntilEnd) soldier.navigation.FollowWaypoint(node, followPathSettings.moveSpeed, terminate);
                else soldier.navigation.FollowWaypointUntil(node, followPathSettings.moveSpeed, followPathSettings.neighborsToVisit, terminate);
            }
        }

    }

    [Serializable]
    public struct FollowPathCommandSettings
    {
        [SerializeField, Range(0f, 5f)]
        public float moveSpeed;

        [SerializeField, Tooltip("When true, waypoint node is followed to completion.")] public bool followUntilEnd;
        
        [Range(0, 1000)]
        [SerializeField, Tooltip("This number is ignored if follow until end is checked.")] public int neighborsToVisit;
    }
}