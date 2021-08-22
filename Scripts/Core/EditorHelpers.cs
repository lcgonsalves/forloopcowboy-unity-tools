using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace ForLoopCowboyCommons.EditorHelpers
{

    public class ReadOnlyAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    public static class PropertyDrawerUtil
    {
        /// <summary>
        /// Deserializes property's parent class.
        /// </summary>
        /// <param name="prop">Serialized Property</param>
        /// <typeparam name="T">Type of serialized property (cast)</typeparam>
        /// <returns></returns>
        public static T GetParent<T>(SerializedProperty prop) where T : class
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach(var element in path.Split('.').Take(elements.Length-1))
            {
                if(element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[","").Replace("]",""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj as T;
        }   
        
        /// <summary>
        /// Deserializes property.
        /// </summary>
        /// <param name="prop">Serialized Property</param>
        /// <typeparam name="T">Type of serialized property (cast)</typeparam>
        /// <returns></returns>
        static public T GetValue<T>(SerializedProperty prop) where T : class
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');
 
            foreach (string element in elements.Take(elements.Length))
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
 
            return obj as T;
        }
	
        public static object GetValue(object source, string name)
        {
            if(source == null)  
                return null;
            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if(f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if(p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }
	
        public static object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while(index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
    }
    
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
                var l = this.l;
                if (l != null) return l.content;
            }

            if (side is Right<T>) {
                var r = this.r;
                if (r != null) return r.content;
            }

            return null;
        }

        public void Set(Side<T1> s) => this.leftValue = s.content;
        public void Set(Side<T2> s) => this.rightValue = s.content;
        public Tuple() {}

    }    

    public interface Side<T> { T content { get; } }
    public sealed class Left<T> : Side<T> { public T content { get; } public Left(T content) { this.content = content; } }
    public sealed class Right<T> : Side<T> { public T content { get; } public Right(T content) { this.content = content; } }

    [Serializable]
    public class LayerHelper : IHasLayer
    {
        [SerializeField]
        private string layerName;

        public LayerHelper(string layerName)
        {
            this.layerName = layerName;
        }

        public int Layer { get => LayerMask.NameToLayer(layerName); }

        public LayerMask LayerMask { get => 1 << Layer; }
        
    }

    public interface IHasLayer
    {
        int Layer { get; }
        LayerMask LayerMask { get; }
        
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

        public static void SetLayerRecursively(this GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
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
            return m.StartCoroutine(Action(delayInSeconds, callback));
        }

        public static Coroutine RunAsync(
            this MonoBehaviour m, 
            Action callback,
            Func<bool> shouldStop, 
            RoutineTypes type = RoutineTypes.Update,
            float delay = 0f
        ){
            return m.StartCoroutine(Routine(type, callback, shouldStop, delay));
        }

    	// Runs coroutine forever
        public static Coroutine RunAsync(this MonoBehaviour m, Action callback)
        {
            return m.RunAsync(callback, () => false);
        }

        // Runs coroutine on fixed update
        public static Coroutine RunAsyncFixed(this MonoBehaviour m, Action callback, Func<bool> shouldStop)
        {
            return m.RunAsync(callback, shouldStop, RoutineTypes.FixedUpdate);
        }

        // Runs coroutine on fixed update forever
        public static Coroutine RunAsyncFixed(this MonoBehaviour m, Action callback)
        {
            return m.RunAsync(callback, () => false, RoutineTypes.FixedUpdate);
        }

        private static IEnumerator Action(float delay, Action callback)
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

        private static IEnumerator Routine(RoutineTypes type, Action callback, Func<bool> stopCondition, float delay = 0f)
        {
            while(stopCondition() == false)
            {
                callback();

                switch(type)
                {
                    case RoutineTypes.TimeInterval:
                        yield return new WaitForSeconds(delay);
                        break;

                    case RoutineTypes.Update:
                        yield return null;
                        break;

                    case RoutineTypes.FixedUpdate:
                        yield return new WaitForFixedUpdate();
                        break;
                }
            }
        }

    }

}