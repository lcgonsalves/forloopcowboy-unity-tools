using System;
using UnityEngine;

namespace ForLoopCowboyCommons.Agent.CustomOrders
{
    /// <summary>
    /// Exposes functions to control the soldier.
    /// </summary>
    [System.Serializable]
    public class SoldierControlStep : Order.SynchronousActionStep
    {
        /// <summary>
        /// Exposes functions to the editor. Each function is a different
        /// action or set of actions to be called on the soldier.
        /// </summary>
        public enum ControlOptions
        {
            Move,
            Idle
        }

        public ControlOptions actionType = ControlOptions.Move;

        public MoveCommandSettings moveSettings = new MoveCommandSettings();

        public SoldierControlStep()
        {
            switch (actionType)
            {
                case ControlOptions.Move:
                    action = agent =>
                    {
                        SoldierBehaviour component = agent.gameObject.GetComponent<SoldierBehaviour>();
                        if (component == null)
                        {
                            Debug.LogError(
                                $"Agent is not a soldier! Please attach the {typeof(SoldierBehaviour)} script.");
                            return;
                        }
                        else
                        {
                            MoveTo(component, Vector3.back);
                        }
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void MoveTo(SoldierBehaviour soldier, Vector3 destination)
        {
            soldier.WalkTo(destination);
        }

    }

    [Serializable]
    public struct MoveCommandSettings
    {
        [SerializeField]
        public Transform destination;
        
        [SerializeField, Range(0f, 5f)]
        public float moveSpeed;
    }
}