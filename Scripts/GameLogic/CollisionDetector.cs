using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class CollisionDetector : SerializedMonoBehaviour
    {
        [CanBeNull] public Collider c = null;
        public CollisionDetector[] children;
        
        [CanBeNull] public CollisionDetector master;

        /// <summary> Includes collisions from this object and any objects in its downline. </summary>
        public event Action<CollisionDetector, Collision> onCollision;

        public event Action<CollisionDetector, Collision> onCollisionChild;
        public event Action<Collision> onCollisionThis;

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

        private void OnChildCollision(CollisionDetector child, Collision childCollision)
        {
            if (master != null) master.OnChildCollision(child, childCollision);
            OnOnCollisionChild(child, childCollision);
        }

        protected virtual void OnOnCollisionThis(Collision obj)
        {
            onCollisionThis?.Invoke(obj);
            onCollision?.Invoke(this, obj);
        }

        protected virtual void OnOnCollisionChild(CollisionDetector child, Collision obj)
        {
            onCollisionChild?.Invoke(child, obj);
            onCollision?.Invoke(child, obj);
        }
    }
}