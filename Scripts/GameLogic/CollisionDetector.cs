using System;
using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// This class exposes events for handling collisions from
    /// outside of the context of the mono behaviour.
    /// </summary>
    public class CollisionDetector : SerializedMonoBehaviour
    {
        [CanBeNull] public Collider c = null;
        public CollisionDetector[] children;
        
        [CanBeNull] public CollisionDetector master;

        /// <summary> Includes collisions from this object and any objects in its downline. </summary>
        public event Action<CollisionDetector, Collision> onCollision;
        [SerializeField] private UnityEvent onCollisionUE;
        public event Action<CollisionDetector, Collision> onCollisionChild;
        public event Action<Collision> onCollisionThis;
        
        /// <summary> Event is triggered when this or a child's collider fires the on trigger enter event. </summary>
        public event Action<CollisionDetector, Collider> onTriggerEnter;
        
        /// <summary> Event is triggered when this or a child's collider fires the on trigger stay event. </summary>
        public event Action<CollisionDetector, Collider> onTriggerStay;
        
        /// <summary> Event is triggered when this or a child's collider fires the on trigger exit event. </summary>
        public event Action<CollisionDetector, Collider> onTriggerExit;

        /**
         * When initializing, we clear the
         * previous children. So be sure to attach
         * explicitly the component to the components you
         * want to detect collision for.
         *
         * Once on an object, them and their children will detect collisions.
         */
        [Button]
        public void Initialize([CanBeNull] CollisionDetector newMaster = null)
        {
            master = newMaster;
            c = GetComponent<Collider>();

            if (children != null && children.Length > 0)
            {
                foreach (var child in children)
                {
                    child.enabled = false;
                    child.master = null;
                }
            }

            // only direct children.
            var colliders = GetComponentsInChildren <Collider>(includeInactive: true);
            children = new CollisionDetector[colliders.Length];

            int i = 0;
            foreach (var coll in colliders)
            {
                var childSlave = coll.gameObject.GetOrElseAddComponent<CollisionDetector>();
                childSlave.c = coll;
                childSlave.master = this;

                children[i] = childSlave;
                i++;
            }

        }

        private void OnCollisionEnter(Collision other)
        {
            if (master != null) master.OnChildCollision(this, other);
            OnOnCollisionThis(other);
        }

        private void OnTriggerEnter(Collider other) => OnOnTriggerEnter(this, other);
        private void OnTriggerStay(Collider other) => OnOnTriggerStay(this, other);

        private void OnTriggerExit(Collider other) => OnOnTriggerExit(this, other);

        private void OnChildCollision(CollisionDetector child, Collision childCollision)
        {
            if (master != null) master.OnChildCollision(child, childCollision);
            OnOnCollisionChild(child, childCollision);
        }

        protected virtual void OnOnCollision(CollisionDetector source, Collision other)
        {
            onCollision?.Invoke(source, other);
            onCollisionUE?.Invoke();
        }
        
        
        protected virtual void OnOnCollisionThis(Collision obj)
        {
            onCollisionThis?.Invoke(obj);
            OnOnCollision(this, obj);
        }

        protected virtual void OnOnCollisionChild(CollisionDetector child, Collision obj)
        {
            onCollisionChild?.Invoke(child, obj);
            OnOnCollision(child, obj);
        }

        protected virtual void OnOnTriggerEnter(CollisionDetector arg1, Collider arg2)
        {
            if (master != null) master.OnOnTriggerEnter(arg1, arg2);
            onTriggerEnter?.Invoke(arg1, arg2);
        }

        protected virtual void OnOnTriggerStay(CollisionDetector arg1, Collider arg2)
        {
            if (master != null) master.OnOnTriggerStay(arg1, arg2);
            onTriggerStay?.Invoke(arg1, arg2);
        }

        protected virtual void OnOnTriggerExit(CollisionDetector arg1, Collider arg2)
        {
            if (master != null) master.OnOnTriggerExit(arg1, arg2);
            onTriggerExit?.Invoke(arg1, arg2);
        }
        
    }
}