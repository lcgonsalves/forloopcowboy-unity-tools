using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace ForLoopCowboyCommons.Agent
{
    [Serializable]
    public class Order : IComparable
    {
        public readonly int priority;
        
        // todo: encapsulate order settings in a SO
        // todo: also allow order settings to be passed in constructor so orders don't need an asset to work

        public Order(int priority)
        {
            this.priority = priority;
        }

        /// <summary>
        /// Executes the order.
        /// </summary>
        /// <param name="waitForSecondsAndThen">Function to allow Order to pause execution routine of Actor momentarily. Second argument is a callback that runs when waiting is finished.</param>
        /// <param name="finish">Function to be run when execution is done. This triggers the Actor to execute next Order! Careful with leaks here.</param>
        public void Execute(
            Action<float, Action> waitForSecondsAndThen,
            Action finish
            ) {
            // todo: expose this to editor
        }

        // based on priority of order
        public int CompareTo(object obj)
        {
            if (obj is Order otherOrder)
            {
                return otherOrder.priority - priority;
            }
            else return -1; // if not an order, prioritize lower
        }
    }

    /// <summary>
    /// Encapsulates the part of an order.
    /// </summary>
    public abstract class OrderStep : IComparable
    {
        [SerializeField] public string name;
        
        /// <summary>
        /// Represents the position in the aggregated list of orders.
        /// </summary>
        public int globalIndex = 0;
        
        /// <summary>
        /// Represents the position in the serialized list of orders.
        /// </summary>
        public int localIndex = 0;

        /// <summary>
        /// Comparison basis is on global index.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Negative if this step is of lower index.</returns>
        public int CompareTo(object obj)
        {
            if (obj is OrderStep step)
            {
                return this.globalIndex - step.globalIndex;
            }
            else return -1;
        }
    }

    /// <summary>
    /// Order step that makes actor wait for the given amount of seconds.
    /// </summary>
    [Serializable]
    public class WaitStep : OrderStep
    {
        public float waitTimeInSeconds;

        public WaitStep(float waitTime)
        {
            name = "Wait";
            waitTimeInSeconds = waitTime;
        }
    }

    /// <summary>
    /// Order step that executes the selected function in chosen Component.
    /// </summary>
    [Serializable]
    public class ExecuteUnityEventsStep : OrderStep
    {
        public ExecuteUnityEventsStep()
        {
            name = "Do something...";
        }
        
        public UnityEvent actions;
    }

}