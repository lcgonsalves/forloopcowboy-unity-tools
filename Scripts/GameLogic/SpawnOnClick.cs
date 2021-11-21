using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Environment;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class SpawnOnClick : SerializedMonoBehaviour, IPointerClickHandler
    {

        public GameplayManager gm;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Spawn();
        }

        [Button]
        public void Spawn()
        {
            var cards = gm.draftedCards;
            var card = cards.IsNullOrEmpty() ? null : gm.draftedCards[0];
            if (card)
            {
                gm.UnitManager.Spawn(gm.side, card.soldier.gameObject);
                gm.DisposeCards(card);
            }
            
        }
        
    }
}