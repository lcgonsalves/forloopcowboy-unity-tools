using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public static class ExtendedDebug
    {
        //Draws just the box at where it is currently hitting.
        public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color)
        {
            origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
            DrawBox(origin, halfExtents, orientation, color);
        }

        //Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
        public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color)
        {
            direction.Normalize();
            Box bottomBox = new Box(origin, halfExtents, orientation);
            Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

            Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
            Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
            Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
            Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
            Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
            Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
            Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
            Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

            DrawBox(bottomBox, color);
            DrawBox(topBox, color);
        }

        public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
        {
            DrawBox(new Box(origin, halfExtents, orientation), color);
        }
        public static void DrawBox(Box box, Color color)
        {
            Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color);
            Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color);
            Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
            Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color);

            Debug.DrawLine(box.backTopLeft, box.backTopRight, color);
            Debug.DrawLine(box.backTopRight, box.backBottomRight, color);
            Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color);
            Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color);

            Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color);
            Debug.DrawLine(box.frontTopRight, box.backTopRight, color);
            Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
            Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color);
        }

        public struct Box
        {
            public Vector3 localFrontTopLeft { get; private set; }
            public Vector3 localFrontTopRight { get; private set; }
            public Vector3 localFrontBottomLeft { get; private set; }
            public Vector3 localFrontBottomRight { get; private set; }
            public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
            public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
            public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
            public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

            public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
            public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
            public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
            public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
            public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
            public Vector3 backTopRight { get { return localBackTopRight + origin; } }
            public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
            public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

            public Vector3 origin { get; private set; }

            public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
            {
                Rotate(orientation);
            }
            public Box(Vector3 origin, Vector3 halfExtents)
            {
                this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
                this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
                this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
                this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

                this.origin = origin;
            }


            public void Rotate(Quaternion orientation)
            {
                localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
                localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
                localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
                localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
            }
        }

        //This should work for all cast types
        static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
        {
            return origin + (direction.normalized * hitInfoDistance);
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }

    }

    public static class StringUtil
    {
        /// <summary>
        /// Turns AStringWithCamelCase into A String With Camel Case.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToHumanReadable(this string s)
        {
            return Regex.Replace(s, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
        }

        public static string Capitalize(this string s)
        {
            if (s.Length == 0) return s;
            var firstCap = char.ToUpper(s[0]);

            if (s.Length > 1) return firstCap + s.Substring(1);
            else return $"{firstCap}";
        }
    }

    public interface IKSettings
    {
        public AvatarIKGoal limb { get; }
        public string path { get; }
        public IKWeightSettings<Vector3> translation { get; }
        public IKWeightSettings<Vector3> rotation { get; }
    }
    
    [Serializable]
    public class SimpleIKSettings : IKSettings
    {
        [SerializeField] protected AvatarIKGoal _limb;
        [SerializeField] protected Transform _target;
        [SerializeField] protected IKWeightSettings<Vector3> _translation;
        [SerializeField] protected IKWeightSettings<Vector3> _rotation;

        public SimpleIKSettings(IKWeightSettings<Vector3> translation, IKWeightSettings<Vector3> rotation)
        {
            _translation = translation;
            _rotation = rotation;
        }

        public AvatarIKGoal limb
        {
            get => _limb;
            set => _limb = value;
        }
        public string path => target.GetPathFrom();

        public Transform target
        {
            get => _target;
            set => _target = value;
        }
        public IKWeightSettings<Vector3> translation => _translation;
        public IKWeightSettings<Vector3> rotation => _rotation;
    }

    [Serializable]
    public class IKWeightSettings<T>
    {
        public T value;
        [Range(0f, 1f)] public float weight = 0f;

        public void SetWeight(float newWeight)
        {
            weight = newWeight;
        }
    }
    
    public static class EnumUtil {
        public static IEnumerable<T> GetValues<T>() {
            return (T[])Enum.GetValues(typeof(T));
        }
    }

    #nullable enable
    [System.Serializable]
    public class Tuple<T1, T2>
    {

        [SerializeField]
        private T1 leftValue;

        [SerializeField]
        private T2 rightValue;

        public Left<T1>? l { get => leftValue != null ? new Left<T1>(leftValue) : null; set => leftValue = value != null ? value.content : default(T1); }
        public Right<T2>? r { get => rightValue != null ? new Right<T2>(rightValue) : null; set => rightValue = value != null ? value.content : default(T2); }

        public T1 Left { get => leftValue; }
        public T2 Right { get => rightValue; }

        public Tuple(T1 leftValue, T2 rightValue)
        {
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public Tuple(T1 leftValue)
        {
            this.leftValue = leftValue;
        }

        public Tuple(T2 rightValue)
        {
            this.rightValue = rightValue;
        }

        // So we can get a right from another anonymous right, left from another anonymous left, etc
        public dynamic Get<T>(Side<T> side)
        {
            if (side is Left<T>) {
                var left = this.l;
                if (left != null) return left.content!;
            }

            if (side is Right<T>) {
                var right = this.r;
                if (right != null) return right.content!;
            }

            return null!;
        }

        public void Set(Left<T1> s) => this.leftValue = s.content;
        public void Set(Right<T2> s) => this.rightValue = s.content;
        public Tuple() {}

    }    

    public interface Side<T> { T content { get; } }
    public sealed class Left<T> : Side<T> { public T content { get; } public Left(T content) { this.content = content; } }
    public sealed class Right<T> : Side<T> { public T content { get; } public Right(T content) { this.content = content; } }

    [Serializable]
    public class LayerHelper
    {
        
        public LayerHelper() { }

        public static int Layer(string layerName)
        {
            return LayerMask.NameToLayer(layerName);
        }

        public static int LayerFromMask(LayerMask mask)
        {
            // todo: cache this, potential bottleneck
            int layerNumber = 0;
            int layer = mask.value;
            while(layer > 0)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber;
        }
        
        public static LayerMask LayerMaskFor(string layerName) { return 1 << Layer(layerName); }
        public static LayerMask LayerMaskFor(int layer) { return 1 << layer; }
        
    }

    public interface IHasLayer
    {
        string LayerName { get; }
        
        int Layer { get; }
        LayerMask LayerMask { get; }
        
    }

    public static class TransformHelpers
    {
        public static Transform? FindRecursively(this Transform t, Func<Transform, bool> predicate)
        {
            if (t.childCount == 0) return null;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child)
                {
                    if (predicate(child)) return child;
                    var found = child.FindRecursively(predicate);
                    if (found != null) return found;
                }
            }

            return null;
        }

        public static void FindAllRecursively(this Transform t, Func<Transform, bool> predicate, List<Transform> result)
        {
            if (t.childCount == 0) return;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child)
                {
                    if (predicate(child))
                    {
                        result.Add(child);
                    }
                    else
                    {
                        child.FindAllRecursively(predicate, result);
                    }
                }
            }
        }

        public static string GetPathFrom(this Transform current, string stopAt = "Root")
        {
            string path = current.name;
            while (current.parent)
            {
                current = current.parent;
                if (current.name == stopAt) break;

                path = $"{current.name}/{path}";
            }

            return path;
        }
    }

    /// <summary>
    /// It's a cache.
    /// </summary>
    /// <typeparam name="T">The type of value to be cached.</typeparam>
    [Serializable]
    public class Cache<T> where T : Component
    {
        [SerializeField]
        private T? reference;
        
        private Func<T> _getter;
        
        /// <summary>
        /// If this is the first time the value is called, the getter function
        /// is called, and its value saved. All subsequent calls will return the cached reference.
        /// </summary>
        public T Get => reference != null ? reference : reference = _getter();

        /// <summary>
        /// Nice little trick to allow you to pass in the
        /// cache as the value.
        /// </summary>
        public static explicit operator T(Cache<T> cache) => cache.Get;

        public Cache(Func<T> getFunction)
        {
            _getter = getFunction;
        }
    }
    
    public static class GameObjectHelpers
    {
        public static T CreateDeepCopy<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(ms);
            }
        }

        public static bool IsNull<T>(this T? obj) where T : class => obj == null;
        public static bool IsNotNull<T>(this T? obj) where T : class => !obj.IsNull();

        // NOTE: these extensions use generic types, which are considerably more taxing on
        // unity's performance. Considering using a normal null check as it has lower overhead.
        public static void IfNotNull<T>(this T? obj, Action<T> doThis, string? elseWarn = null) where T : class
        {
            if (obj.IsNotNull())
                doThis(obj!);
            else if (elseWarn != null)
                obj.WarnIfIsNull(elseWarn);
        }
        
        public static void IfNull<T>(this T? obj, Action doThis) where T : class
        {
            if (obj.IsNull())
                doThis();
        }

        public static void WarnIfIsNull<T>(this T? obj, string message = "") where T : class
        {
            if (obj == null) Debug.LogWarning($"{nameof(obj)} should not be null! {message}");
        }
        
        public static bool HasComponent<T> (this GameObject obj) where T : Component
        {
            return obj.TryGetComponent(typeof(T), out var _);
        }
        
        public static bool HasComponent<T> (this Component obj)
        {
            return (obj.GetComponent<T>() as Component) != null;
        }

        public static void SetLayerRecursively(this GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        /// <summary>
        /// Gets collider center, corrected from its transform.
        /// </summary>
        /// <returns>Collider's center in world coordinates.</returns>
        public static Vector3 GetWorldPosition(this Collider collider)
        {
            Vector3 colliderCenter;
            
            switch (collider)
            {
                case BoxCollider boxCollider:
                    colliderCenter = boxCollider.center;
                    break;
                case CapsuleCollider capsuleCollider:
                    colliderCenter = capsuleCollider.center;
                    break;
                case CharacterController characterController:
                    colliderCenter = characterController.center;
                    break;
                case MeshCollider meshCollider:
                    colliderCenter = meshCollider.bounds.center;
                    break;
                case SphereCollider sphereCollider:
                    colliderCenter = sphereCollider.center;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(collider));
            }

            return collider.transform.TransformPoint(colliderCenter);
        }

        public static Coroutine ScaleToTarget(this MonoBehaviour mono, Vector3 targetScale, float duration)
        {
            IEnumerator ScaleToTargetCoroutine()
            {
                Vector3 startScale = mono.transform.localScale;
                float timer = 0.0f;
 
                while(timer < duration)
                {
                    timer += Time.deltaTime;
                    float t = timer / duration;
                    //smoother step algorithm
                    t = t * t * t * (t * (6f * t - 15f) + 10f);
                    mono.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    yield return null;
                }
 
                yield return null;
            }
            
            return mono.StartCoroutine(ScaleToTargetCoroutine());
        }

        public static Component GetOrElseAddComponent<Component> (this GameObject gObj)
            where Component : UnityEngine.Component
        {
            Component c = gObj.GetComponent<Component>();
            if (c == null) c = gObj.gameObject.AddComponent<Component>();

            return c;
        }

        public static Component GetOrElseAddComponent<Component> (this MonoBehaviour mono)
            where Component : UnityEngine.Component
        {
            return GameObjectHelpers.GetOrElseAddComponent<Component>(mono.gameObject);
        }

        // Executes function using coroutine after given delay
        public static Coroutine RunAsyncWithDelay(this MonoBehaviour m, float delayInSeconds, Action callback)
        {
            return m.StartCoroutine(DelayedAction(delayInSeconds, callback));
        }

        public static Coroutine RunAsync(
            this MonoBehaviour m, 
            Action callback,
            Func<bool> shouldStop, 
            RoutineTypes type = RoutineTypes.Update,
            float delay = 0f
        ){
            return m.StartCoroutine(GenericCoroutineWithStopCondition(type, callback, shouldStop, delay));
        }

    	// Runs coroutine forever
        public static Coroutine RunAsync(this MonoBehaviour m, Action callback)
        {
            return m.RunAsync(callback, () => false);
        }
        
        public static Coroutine RunAsync(this MonoBehaviour m, Func<bool> returnTrueToStop)
        {
            return m.RunAsync(() => { }, returnTrueToStop);
        }

        // Runs coroutine on fixed update
        public static Coroutine RunAsyncFixed(this MonoBehaviour m, Action callback, Func<bool> shouldStop)
        {
            return m.RunAsync(callback, shouldStop, RoutineTypes.FixedUpdate);
        }
        
        /// <summary>
        /// Runs with only one looper, which returns true if should stop.
        /// </summary>
        /// <param name="shouldStop">looper function, which returns true if should stop.</param>
        /// <returns></returns>
        public static Coroutine RunAsyncFixed(this MonoBehaviour m, Func<bool> shouldStop)
        {
            return m.RunAsync(() => { }, shouldStop, RoutineTypes.FixedUpdate);
        }

        // Runs coroutine on fixed update forever
        public static Coroutine RunAsyncFixed(this MonoBehaviour m, Action callback)
        {
            return m.RunAsync(callback, () => false, RoutineTypes.FixedUpdate);
        }

        private static IEnumerator DelayedAction(float delay, Action callback)
        {
                yield return new WaitForSeconds(delay);
                callback();
        }

        public enum RoutineTypes
        {
            TimeInterval,
            Update,
            FixedUpdate
        }

        private static IEnumerator GenericCoroutineWithStopCondition(RoutineTypes type, Action callback, Func<bool> stopCondition, float delay = 0f)
        {
            var wfs = new WaitForSeconds(delay);
            var wffu = new WaitForFixedUpdate();
            
            while(stopCondition() == false)
            {
                callback();

                switch(type)
                {
                    case RoutineTypes.TimeInterval:
                        yield return wfs;
                        break;

                    case RoutineTypes.Update:
                        yield return null;
                        break;

                    case RoutineTypes.FixedUpdate:
                        yield return wffu;
                        break;
                }
            }
        }

    }

    public static class InputSystemExtended
    {

        /// <summary>
        /// Checks if it's float value is positive. True if it is.
        /// You can control the tolerance, if this is a soft button (or your floats are behaving weirdly and buttons never unpress).
        /// </summary>
        public static bool WasPressedThisFrame(this InputActionReference reference, float tolerance = 0.0f)
        {
            if (reference == null) return false;
            return reference.action.ReadValue<float>() > 0 + tolerance;
        }

        /// <summary>
        /// Gets the value this frame.
        /// </summary>
        public static T ValueThisFrame<T>(this InputActionReference reference) where T : struct
        {
            // always referencing action is cumbersome to me, sorryu
            return reference.action.ReadValue<T>();
        }

    }

    /// <summary>
    /// Just exposes a MonoBehaviour to run coroutines from.
    /// </summary>
    public class AsyncRunner : MonoBehaviour { }
}