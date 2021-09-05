using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Agent.CustomOrders
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
            FollowLastPath
        }

        [SerializeField] private ControlOptions _actionType;

        public void UpdateActionTypeWithSerializedVersion()
        {
            switch (ActionType)
            {
                case ControlOptions.FollowNearestPath:
                    AssignSoldierCallback(FindNearestPathAndFollowIt);
                    break;
                case ControlOptions.FollowLastPath:
                    AssignSoldierCallback(FollowLastPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // auto-update name- changing action type makes previous name obsolete.
            // only change if current name matches a different state's name - if user overrode the name, keep as is
            bool currentNameIsObsolete = false;

            foreach (var option in EnumUtil.GetValues<ControlOptions>())
            {
                currentNameIsObsolete |= name == null || name.Equals(option.ToString().ToHumanReadable());
            }
            
            if (currentNameIsObsolete) name = ActionType.ToString().ToHumanReadable();

        }

        public ControlOptions ActionType
        {
            get => _actionType;
            private set
            {
                _actionType = value;
                UpdateActionTypeWithSerializedVersion();
            }
        }

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

        public SoldierControlStep() : this(ControlOptions.FollowNearestPath) { }

        public SoldierControlStep(ControlOptions actionType)
        {
            ActionType = actionType;
            name = actionType.ToString().ToHumanReadable();
        }

        private void AssignSoldierCallback(Action<SoldierBehaviour, Action> callback)
        {
            callbackWithTerminator = (agent, terminate) => 
                DoIfAgentIsSoldier(agent, c => callback(c, terminate));
        }

        public void FindNearestPathAndFollowIt(SoldierBehaviour soldier, Action terminate)
        {
            var nodes = soldier.navigation.GetNearbyWaypointNodes(5f, 1);
            var node = nodes.Length > 0 ? nodes[0] : null;
            if (node != null)
            {
                if (followPathSettings.followUntilEnd) soldier.navigation.FollowWaypoint(node, followPathSettings.moveSpeed, terminate);
                else soldier.navigation.FollowWaypointUntil(node, followPathSettings.moveSpeed, followPathSettings.neighborsToVisit, terminate);
            } else Debug.LogWarning("No nodes found nearby.");
        }

        public void FollowLastPath(SoldierBehaviour soldier, Action terminate)
        {
            if (followPathSettings.followUntilEnd)
                soldier.navigation.FollowWaypoint(soldier.navigation.LastWaypointPathStart, followPathSettings.moveSpeed, terminate);
            else soldier.navigation.FollowWaypointUntil(
                soldier.navigation.LastWaypointPathStart, 
                followPathSettings.moveSpeed,
                followPathSettings.neighborsToVisit,
                terminate
            );
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