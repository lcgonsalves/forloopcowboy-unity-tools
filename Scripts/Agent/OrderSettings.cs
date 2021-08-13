using System;
using System.Collections.Generic;
using Gemini;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace ForLoopCowboyCommons.Agent
{
    // todo: implement ICollection
    [CreateAssetMenu(fileName = "Untitled Order Settings", menuName = "Order", order = 0), Serializable]
    public class OrderSettings : ScriptableObject
    {
        // Each kind of order is stored in a separate list for serialization
        // custom editor puts it together for ease of use
        // public api methods expose a generic interface
        
        [SerializeField]
        private List<ExecuteUnityEventsStep> actionSteps = new List<ExecuteUnityEventsStep>(1);

        [SerializeField] 
        private List<WaitStep> waitSteps = new List<WaitStep>(1);

        // cached iterator
        private IList<OrderStep> _cachedIterator = null;
        // true when new items are added, forcing iterator to regenerate
        private bool _dirty = false;

        /// <summary>
        /// This getter collects all steps in all serialized lists and
        /// aggregates them in this indexed priority queue, updating their global indices.
        /// This operation can be quite expensive, but it only
        /// needs to be performed when new elements are added to the collection. If no
        /// new elements have been added, a cached list is returned.
        /// </summary>
        public IList<OrderStep> iterator
        {
            get
            {
                if (_dirty || _cachedIterator == null)
                {
                    int totalNumberOfSteps = actionSteps.Count + waitSteps.Count;

                    var q = new IndexedPriorityQueue<OrderStep>(totalNumberOfSteps);
                    var list = new List<OrderStep>(totalNumberOfSteps);
                    var globalIndex = 0;
                    
                    // inserting in queue orders the list
                    foreach (var action in actionSteps)
                    {
                        q.Insert(globalIndex++, action);
                    }

                    foreach (var waitAction in waitSteps)
                    {
                        q.Insert(globalIndex++, waitAction);
                    }

                    // now we convert it to a c# iterable
                    // this ensures that there are no duplicate indices
                    for (int i = 0; i < totalNumberOfSteps; i++)
                    {
                        var correctedGlobalIndex = i;
                        var step = q.Pop();
                        step.globalIndex = i;
                        list.Add(step);
                    }

                    _cachedIterator = list;
                    _dirty = false;
                    
                    return list;
                }
                else return _cachedIterator;
            }
        }

        /// <summary>
        /// Adds step to its serialized list, ordered by its globalIndex member variable.
        /// </summary>
        /// <param name="step">Step instance</param>
        public void Add(OrderStep step)
        {
            _dirty = true;
            AddAndUpdateIndices(step);
        }

        /// <summary>
        /// Removes step from its serialized list.
        /// </summary>
        /// <param name="step">Step instance</param>
        public void Remove(OrderStep step)
        {
            _dirty = true;
            RemoveAndUpdateIndices(step);
        }

        /// <summary>
        /// Updates the global index of a given step. If step is in the list,
        /// the list will be reordered.
        /// </summary>
        /// <param name="step">Step</param>
        /// <param name="newGlobalIndex"></param>
        public void UpdateGlobalIndexOf(OrderStep step, int newGlobalIndex)
        {
            bool isMoveDown = step.globalIndex > newGlobalIndex;
            bool isMoveUp = step.globalIndex < newGlobalIndex;
            
            // update global indices to accommodate new global index
            foreach (var s in iterator)
            {
                if (isMoveDown && s.globalIndex >= newGlobalIndex && s != step) s.globalIndex++;
                if (isMoveUp && s.globalIndex <= newGlobalIndex && s != step) s.globalIndex--;
            }
            
            switch (step)
            {
                case ExecuteUnityEventsStep _:
                    actionSteps[step.localIndex].globalIndex = newGlobalIndex;
                    break;
                case WaitStep _:
                    waitSteps[step.localIndex].globalIndex = newGlobalIndex;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step));
            }
            
            _dirty = true;
            
        }

        private void AddAndUpdateIndices(OrderStep step)
        {

            if (step is ExecuteUnityEventsStep es) actionSteps.Add(es);
            else if (step is WaitStep ws) waitSteps.Add(ws);
            else Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
            
            UpdateLocalIndices(step);
        }
        
        private void RemoveAndUpdateIndices(OrderStep step)
        {
            if (step is ExecuteUnityEventsStep es) actionSteps.Remove(es);
            else if (step is WaitStep ws) waitSteps.Remove(ws);
            else Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
            
            UpdateLocalIndices(step);
        }

        /// <summary>
        /// Iterates over step's list and sets local index to the
        /// real index.
        /// </summary>
        /// <param name="step">decides which serializable collection to update</param>
        private void UpdateLocalIndices(OrderStep step)
        {
            if (step is ExecuteUnityEventsStep)
            {
                for (int i = 0; i < actionSteps.Count; i++)
                {
                    actionSteps[i].localIndex = i;
                }
            }
            else if (step is WaitStep)
            {
                for (int i = 0; i < waitSteps.Count; i++)
                {
                    waitSteps[i].localIndex = i;
                }
            }
            else Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
        }
        
        private void OnEnable()
        {
            if (actionSteps == null) actionSteps = new List<ExecuteUnityEventsStep>(1);
        }
    }
}