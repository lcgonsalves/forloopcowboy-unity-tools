using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    public class NPCAttributeComponent : MonoBehaviour
    {
        public enum Traits
        {
            CarefulShooter,
            TriggerHappy,
            DemolitionsExpert
        }

        public string firstName;
        public string lastName;

        public string FullName => $"{firstName.Capitalize()} {lastName.Capitalize()}"; 

        public Traits trait;
        public static Traits[] traits => EnumUtil.GetValues<Traits>().ToArray();
        
        [Range(0, 3)] public int accuracy;
        [Range(0, 3)] public int rateOfFire;
        [Range(0, 3)] public int armor;

        // randomizer inputs
        public StringList possibleFirstNames;
        public StringList possibleLastNames;

        /// <summary>
        /// Distributes points between 1 and 9
        /// between the 3 attributes an npc can have.
        /// Randomizes trait.
        /// Randomizes first and last name.
        /// </summary>
        /// <param name="pointsToDistribute"></param>
        [Button] public void Randomize(int pointsToDistribute = 3)
        {
            // reset
            accuracy = 0;
            rateOfFire = 0;
            armor = 0;

            firstName = possibleFirstNames.GetRandom();
            lastName = possibleLastNames.GetRandom();
            
            pointsToDistribute = Mathf.Clamp(pointsToDistribute, 1, 9);
            var possibleTraits = traits;

            trait = possibleTraits[Random.Range(0, possibleTraits.Length)];
            
            if (pointsToDistribute == 9)
            {
                accuracy = 3;
                rateOfFire = 3;
                armor = 3;
            }
            else
            {
                int pointsLeftToDistribute = pointsToDistribute;
                int attemptsLeft = 30;
                var fullTraits = new HashSet<Traits>();

                while (attemptsLeft > 0 && pointsToDistribute > 0)
                {
                    var selectedAttr = Random.Range(0, 3);
                    switch (selectedAttr)
                    {
                        case 0:
                            if (accuracy < 3)
                            {
                                accuracy++;
                                pointsToDistribute--;
                            }
                            break;
                        case 1:
                            if (rateOfFire < 3)
                            {
                                rateOfFire++;
                                pointsToDistribute--;
                            }
                            break;
                        case 2:
                            if (armor < 3)
                            {
                                armor++;
                                pointsToDistribute--;
                            }
                            break;
                    }

                    attemptsLeft--;
                }
            }
        }

        [Button]
        public void Set(NPCAttributePreset preset)
        {
            firstName = preset.firstName;
            lastName = preset.lastName;
            trait = preset.trait;
            accuracy = preset.accuracy;
            rateOfFire = preset.rateOfFire;
            armor = preset.armor;
        }
    }

    [Serializable]
    public struct NPCAttributePreset
    {
        public string firstName;
        public string lastName;
        public NPCAttributeComponent.Traits trait;
        [Range(0, 3)] public int accuracy;
        [Range(0, 3)] public int rateOfFire;
        [Range(0, 3)] public int armor;
    }
}