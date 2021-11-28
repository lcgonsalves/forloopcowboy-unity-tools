using System;
using System.Collections.Generic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [CreateAssetMenu(fileName = "Armor Settings", menuName = "Settings/NPC/New Armor Settings...", order = 0)]
    public class ArmorSettings : ScriptableObject
    {
        [Serializable]
        public class ArmorRating
        {
            public int numberOfStars;
            public int rating;
        }

        public List<ArmorRating> settings = new List<ArmorRating>();

        /// <summary>
        /// Default is zero, if none is found.
        /// </summary>
        /// <param name="numberOfStars"></param>
        /// <returns></returns>
        public int GetRatingFor(int numberOfStars)
        {
            int rating = 0;

            var r = settings.Find(_ => _.numberOfStars == numberOfStars);
            if (r != null) rating = r.rating;

            return rating;
        }
    }
}