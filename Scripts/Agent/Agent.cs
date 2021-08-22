using System;
using System.Collections;
using System.Collections.Generic;
using ForLoopCowboyCommons.Agent;
using ForLoopCowboyCommons.EditorHelpers;
using Gemini;
using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine.Events;

namespace ForLoopCowboyCommons.Agent
{
    
    /// <summary>
    /// Uses animator state machine to execute actions sequentially.
    /// Implements a basic state machine that executes actions in order.
    /// <list type="bullet">
    /// <item>Starts ready</item>
    /// <item>When order is enqueued, executes next if ready</item>
    /// <item>When order begins to be executed, agent is busy</item>
    /// <item>Order can change agent state to waiting for given periods of time</item>
    /// <item>Waiting always goes back to busy</item>
    /// <item>busy only goes back to ready when order triggers finish function</item>
    /// <item>going back to ready triggers next order if any</item>
    /// <item>no orders means agent is idle (and ready for next order)</item>
    /// </list>
    /// </summary>
    public class Agent : MonoBehaviour
    {
        protected static readonly int DefaultOrderCapacity = 15;
        
        [ReadOnly, SerializeField]
        private AgentState _currentState = AgentState.Ready;
        public AgentState currentState
        {
            get => _currentState;
            private set
            {
                // if transitioning to ready, call on event
                if (value == AgentState.Ready && _currentState != value)
                {
                    onReady?.Invoke();
                }

                _currentState = value;
            }
            
        }
        public int currentOrderID { get; private set; } = 0;

        [Tooltip("Maximum number of orders that fit in the queue.")]
        public int orderCapacity = DefaultOrderCapacity;

        public enum AgentState
        {
            Ready,
            Busy,
            Waiting
        }
        
        // events
        protected event Action onReady;

        [SerializeField]
        private protected List<Order> _orders = new List<Order>(DefaultOrderCapacity);

        private protected readonly IndexedPriorityQueue<Order> orderQueue = new IndexedPriorityQueue<Order>(DefaultOrderCapacity);

        private void Start()
        {
            orderQueue.Resize(_orders.Count);
            // initialize queue with serialized orders with a bit of delay
            this.RunAsyncWithDelay(0.25f, () =>
            {
                foreach (var order in _orders)
                {
                    Enqueue(order);
                }
            });
        }

        /// <summary>
        /// Adds order to the queue. Orders are executed as soon as the agent is Ready.
        /// </summary>
        /// <param name="order">Order to be executed.</param>
        public void Enqueue(Order order)
        {
            orderQueue.Insert(order);
            if (currentState == AgentState.Ready) EventHandlerExecuteNext();
        }
        
        /// <summary>
        /// Pops order from queue if available and executes it.
        /// </summary>
        /// <returns>true if the order is executed, false if no orders are available</returns>
        private bool TryExecuteNext()
        {
            if (orderQueue.IsEmpty()) return false;

            Order nextOrder = orderQueue.Pop();

            currentState = AgentState.Busy;
            nextOrder.Execute(this, WaitForAndThen, FinishOrderAndTransitionToReadyState(currentOrderID));

            return true;
        }
        
        /// <summary>
        /// Pops order from queue if available and executes it.
        /// fucking events require a void-returning function but it wanna unsub at the end.
        /// </summary>
        private void EventHandlerExecuteNext() { TryExecuteNext(); }
        
        private void WaitForAndThen(float seconds, Action onFinishWaiting)
        {
            currentState = AgentState.Waiting;
            this.RunAsyncWithDelay(seconds, () =>
            {
                currentState = AgentState.Busy;
                onFinishWaiting();
            });
        }

        /// <summary>
        /// Sets state to ready and executes next order if there is a next order in the queue.
        /// </summary>
        /// <param name="oid"></param>
        private Action FinishOrderAndTransitionToReadyState(int oid)
        {
            return () =>
            {
                if (oid == currentOrderID)
                {
                    currentOrderID++;
                    currentState = AgentState.Ready;
                }
                else Debug.LogError($"Order ID is invalid. Current order is {currentOrderID} but order {oid} attempted to terminate.");
            };
        }

        // Unity events
        private void OnEnable()
        {
            onReady += EventHandlerExecuteNext;
        }

        private void OnDisable()
        {
            onReady -= EventHandlerExecuteNext;
        }

    }
}
