using System;
using System.Collections.Generic;
using ForLoopCowboyCommons.Agent.CustomOrders;
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
        private List<Order.ExecuteUnityEventsStep> unityEventsSteps = new List<Order.ExecuteUnityEventsStep>(1);

        [SerializeField] 
        private List<Order.WaitStep> waitSteps = new List<Order.WaitStep>(1);
        
        [SerializeField] 
        private List<SoldierControlStep> soldierSteps = new List<SoldierControlStep>(1);

        // cached iterator
        private IList<Order.Step> _cachedIterator = null;
        // true when new items are added, forcing iterator to regenerate
        private bool _dirty = false;

        /// <summary>
        /// This getter collects all steps in all serialized lists and
        /// aggregates them in this indexed priority queue, updating their global indices.
        /// This operation can be quite expensive, but it only
        /// needs to be performed when new elements are added to the collection. If no
        /// new elements have been added, a cached list is returned.
        /// </summary>
        public IList<Order.Step> iterator
        {
            get
            {
                if (_dirty || _cachedIterator == null)
                {
                    int totalNumberOfSteps = unityEventsSteps.Count + waitSteps.Count + soldierSteps.Count;

                    var q = new IndexedPriorityQueue<Order.Step>(totalNumberOfSteps);
                    var list = new List<Order.Step>(totalNumberOfSteps);
                    var globalIndex = 0;
                    
                    // inserting in queue orders the list
                    foreach (var action in unityEventsSteps)
                    {
                        q.Insert(globalIndex++, action);
                    }

                    foreach (Order.WaitStep waitAction in waitSteps)
                    {
                        q.Insert(globalIndex++, waitAction);
                    }
                    
                    foreach (var soldierControlStep in soldierSteps)
                    {
                        q.Insert(globalIndex++, soldierControlStep);
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
        public void Add(Order.Step step)
        {
            _dirty = true;
            AddAndUpdateIndices(step);
        }

        /// <summary>
        /// Removes step from its serialized list.
        /// </summary>
        /// <param name="step">Step instance</param>
        public void Remove(Order.Step step)
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
        public void UpdateGlobalIndexOf(Order.Step step, int newGlobalIndex)
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
                case Order.ExecuteUnityEventsStep _:
                    unityEventsSteps[step.localIndex].globalIndex = newGlobalIndex;
                    break;
                case Order.WaitStep _:
                    waitSteps[step.localIndex].globalIndex = newGlobalIndex;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step));
            }
            
            _dirty = true;
            
        }

        private void AddAndUpdateIndices(Order.Step step)
        {
            switch (step)
            {
                case SoldierControlStep scs:
                    soldierSteps.Add(scs);
                    break;
                
                case Order.ExecuteUnityEventsStep es:
                    unityEventsSteps.Add(es);
                    break;
                
                case Order.WaitStep ws:
                    waitSteps.Add(ws);
                    break;


                default:
                {
                    Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
                    break;
                }
            }

            UpdateLocalIndices(step);
        }
        
        private void RemoveAndUpdateIndices(Order.Step step)
        {
            switch (step)
            {
                case SoldierControlStep scs:
                    soldierSteps.Remove(scs);
                    break;
                case Order.ExecuteUnityEventsStep es:
                    unityEventsSteps.Remove(es);
                    break;
                case Order.WaitStep ws:
                    waitSteps.Remove(ws);
                    break;
                default:
                    Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
                    break;
            }

            UpdateLocalIndices(step);
        }

        /// <summary>
        /// Iterates over step's list and sets local index to the
        /// real index.
        /// </summary>
        /// <param name="step">decides which serializable collection to update</param>
        private void UpdateLocalIndices(Order.Step step)
        {
            switch (step)
            {
                case SoldierControlStep _:
                {
                    for (int i = 0; i < soldierSteps.Count; i++)
                    {
                        soldierSteps[i].localIndex = i;
                    }

                    break;
                }
                case Order.ExecuteUnityEventsStep _:
                {
                    for (int i = 0; i < unityEventsSteps.Count; i++)
                    {
                        unityEventsSteps[i].localIndex = i;
                    }

                    break;
                }
                case Order.WaitStep _:
                {
                    for (int i = 0; i < waitSteps.Count; i++)
                    {
                        waitSteps[i].localIndex = i;
                    }

                    break;
                }
                
                default:
                    Debug.LogError($"No serialized collections for step of type ${step.GetType()}");
                    break;
            }
        }
        
        private void OnEnable()
        {
            if (unityEventsSteps == null) unityEventsSteps = new List<Order.ExecuteUnityEventsStep>(1);
        }
    }
}