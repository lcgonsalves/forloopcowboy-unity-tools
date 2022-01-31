using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace forloopcowboy_unity_tools.Scripts.Core
{

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

}