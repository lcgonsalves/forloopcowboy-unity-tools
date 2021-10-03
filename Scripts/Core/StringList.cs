using System.Collections.Generic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    [CreateAssetMenu(fileName = "New List of Strings", menuName = "Util/New List of Strings...", order = 0)]
    public class StringList : ScriptableObject
    {
        public List<string> strings;

        public string GetRandom()
        {
            return strings.Count > 0 ? strings[Random.Range(0, strings.Count)] : "";
        }
    }
}