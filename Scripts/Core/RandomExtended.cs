using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public static class RandomExtended
    {
        public static bool Boolean() => Random.value > .5;
    }
}