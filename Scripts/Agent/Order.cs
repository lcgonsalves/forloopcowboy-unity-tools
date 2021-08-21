using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace ForLoopCowboyCommons.Agent
{
    [Serializable]
    public class Order : IComparable
    {
        public readonly int priority;

        public OrderSettings settings;

        /// <summary>
        /// List of steps comes from settings if settings are defined,
        /// otherwise from local variable.
        /// Meaning that order does not need predefined settings to work, it can be instantiated
        /// through code without any serialization needed.
        /// </summary>
        public IEnumerable<Step> steps
        {
            get { return settings != null ? settings.iterator : _steps; }
        }

        private IEnumerable<Step> _steps;

        public Order(int priority, IEnumerable<Step> steps)
        {
            this.priority = priority;
            _steps = steps;
        }

        public Order(int priority, OrderSettings settings)
        {
            this.priority = priority;
            this.settings = settings;
        }

        public Order(int priority)
        {
            this.priority = priority;
        }

        /// <summary>
        /// Executes the order.
        /// </summary>
        /// <param name="agent">The game object executing this order.</param>
        /// <param name="waitForSecondsAndThen">Function to allow Order to pause execution routine of Actor momentarily. Second argument is a callback that runs when waiting is finished.</param>
        /// <param name="finish">Function to be run when execution is done. This triggers the Actor to execute next Order! Careful with leaks here.</param>
        public void Execute(
            Agent agent,
            Action<float, Action> waitForSecondsAndThen,
            Action finish
        )
        {
            var enumerator = steps.GetEnumerator();
            enumerator.MoveNext();
            
            ExecuteStepRecursively(agent, enumerator, waitForSecondsAndThen, finish);
        }   

        private void ExecuteStepRecursively(
            Agent agent,
            IEnumerator<Step> stepEnumerator,
            Action<float, Action> waitForSecondsAndThen,
            Action finish
        )
        {
            var current = stepEnumerator.Current;
            if (current == null) return; // be sure to initialize the enumerator when instantiating it

            var hasNext = stepEnumerator.MoveNext();

            switch (current)
            {
                // special case - nothing to execute, we just wait.
                case WaitStep waitStep:
                    waitForSecondsAndThen(waitStep.waitTimeInSeconds, () => EndStepAndIterateOrFinish(agent, stepEnumerator, waitForSecondsAndThen, finish, hasNext));
                    break;
                default:
                    current.Execute(agent, () => EndStepAndIterateOrFinish(agent, stepEnumerator, waitForSecondsAndThen, finish, hasNext));
                    break;
            }
        }

        private void EndStepAndIterateOrFinish(
            Agent agent,
            IEnumerator<Step> stepEnumerator,
            Action<float, Action> waitForSecondsAndThen,
            Action finish,
            bool hasNext
        ) {
            // only iterate on recursion on callback
            if (hasNext) ExecuteStepRecursively(agent, stepEnumerator, waitForSecondsAndThen, finish);
            else finish();
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
        
        /// <summary>
        /// Represents a step for executing a function.
        /// </summary>
        public class SynchronousActionStep : Step
        {
            protected Action<Agent> action;

            public SynchronousActionStep(Action<Agent> action)
            {
                this.action = action;
            }

            public SynchronousActionStep(Action fromAction)
            {
                // if we want to ignore the context, we can.
                this.action = _ => fromAction();
            }

            // initializes step without callback, to be overridden by children.
            public SynchronousActionStep()
            {
                action = null; // not defaulting here because an action step without an action makes no sense and we should get an error for it
            }

            public override void Execute(Agent context, Action endStep)
            {
                action(context);
                endStep();
            }
        }
        
        /// <summary>
        /// Represents a step for asynchronously executing a function. Step ends when function calls a terminator action.
        /// </summary>
        public class AsyncActionStep : Step
        {
            protected Action<Agent, Action> callbackWithTerminator;

            /// <summary>
            /// Instantiates an async step.
            /// </summary>
            /// <param name="callbackWithTerminator">Action that calls it's parameter when termination is complete.</param>
            public AsyncActionStep(Action<Agent, Action> callbackWithTerminator)
            {
                this.callbackWithTerminator = callbackWithTerminator;
            }

            // for internally defining terminator
            protected AsyncActionStep()
            {
                this.callbackWithTerminator = null;
            }

            /// <summary>
            /// Instantiates an async step, ignoring context.
            /// </summary>
            /// <param name="fromAction">Action that calls it's parameter when termination is complete.</param>
            public AsyncActionStep(Action<Action> fromAction)
            {
                callbackWithTerminator = (_, terminateStep) => fromAction(terminateStep);
            }

            public override void Execute(Agent context, Action endStep)
            {
                callbackWithTerminator(context, endStep);
            }
        }

        /// <summary>
        /// Encapsulates the part of an order.
        /// </summary>
        public abstract class Step : IComparable
        {
            [SerializeField] protected string name;

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
                if (obj is Step step)
                {
                    return this.globalIndex - step.globalIndex;
                }
                else return -1;
            }

            /// <summary>
            /// Logic for the given step. Implementations must define this.
            /// </summary>
            /// <returns></returns>
            public abstract void Execute(Agent context, Action endStep);

            /// <summary>
            /// Returns a step that executes synchronously
            /// and ends when the action returns.
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            public static SynchronousActionStep Sync(Action action)
            {
                return new SynchronousActionStep(action);
            }

            /// <summary>
            /// Returns a step that terminates when the action
            /// calls its terminator argument.
            /// </summary>
            /// <param name="actionWithTerminator">Action that signals termination by calling its only argument.</param>
            /// <returns></returns>
            public static AsyncActionStep Async(Action<Action> actionWithTerminator)
            {
                return new AsyncActionStep(actionWithTerminator);
            }

        }

        /// <summary>
        /// Order step that makes actor wait for the given amount of seconds.
        /// This is a special case as the execute function is never to be called.
        /// </summary>
        [Serializable]
        public class WaitStep : Step
        {
            public float waitTimeInSeconds;

            public WaitStep(float waitTime)
            {
                name = "Wait";  
                waitTimeInSeconds = waitTime;
            }

            /// <summary>
            /// This is a special case - if it's a wait order then this is executed within the
            /// order execution, by pattern matching the type.
            /// </summary>
            public override void Execute(Agent context, Action endStep)
            {
                Debug.LogError("Wait step shouldn't execute!");
                endStep();
            }
        }

        /// <summary>
        /// Order step that executes the selected function in chosen Component.
        /// </summary>
        [Serializable]
        public class ExecuteUnityEventsStep : SynchronousActionStep
        {
            public ExecuteUnityEventsStep()
            {
                name = "Do something...";
                action = _ => actions?.Invoke();
            }
            
            public UnityEvent actions;

        }
    }
}