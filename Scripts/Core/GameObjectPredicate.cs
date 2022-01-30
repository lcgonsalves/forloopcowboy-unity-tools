using System;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    
    [CreateAssetMenu(fileName = "Predicate", menuName = "Game Object Predicate", order = 0)]
    public class GameObjectPredicate : SerializedScriptableObject, IPredicate<GameObject>
    {
        public enum Operation
        {
            IS_PLAYER,
            IS_BULLET
        }
        
        public enum BooleanOperation
        {
            AND,
            OR
        }

        [InfoBox("bool op is applied to all operations.")]
        public BooleanOperation booleanOperation;
        public Operation[] operations;

        [Tooltip("If no operations are defined, this value will be used")]
        public bool defaultValue;
        
        public bool Apply(GameObject t)
        {
            if (operations == null || operations.Length == 0) return defaultValue;

            bool result = booleanOperation == BooleanOperation.AND; // start at true if AND and false if OR

            foreach (var operation in operations)
            {
                switch (booleanOperation)
                {
                    case BooleanOperation.AND:
                        result = result && ApplyOp(t, operation);
                        break;
                    case BooleanOperation.OR:
                        result = result || ApplyOp(t, operation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        public bool ApplyNot(GameObject t) => !Apply(t);

        private bool ApplyOp(GameObject gameObject, Operation op)
        {
            switch (op)
            {
                case Operation.IS_PLAYER:
                    return gameObject.HasComponent<PlayerComponent>();
                case Operation.IS_BULLET:
                    return gameObject.HasComponent<BulletController>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }
    }

    public interface IPredicate<T>
    {
        bool Apply(T t);
    }
    
}