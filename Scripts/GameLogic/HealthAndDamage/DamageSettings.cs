using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    [CreateAssetMenu(fileName = "Untitled Damage Settings", menuName = "Settings/Damage Settings...", order = 0)]
    public class DamageSettings : SerializedScriptableObject, IDamageProvider
    {
        [SerializeField, LabelText("Damage range"), MinMaxSlider(0, 100)]
        private Vector2 minMaxValueSlider = new Vector2(-7, -2);

        public bool useStaticDamageAmount = false;

        [ShowIf("useStaticDamageAmount")]
        public int staticDamageAmount;

        public int Min => Mathf.RoundToInt(minMaxValueSlider.x);
        public int Max => Mathf.RoundToInt(minMaxValueSlider.y);

        public int GetDamageAmount()
        {
            return useStaticDamageAmount ?
                staticDamageAmount :
                Random.Range(Min, Max);
        }
    }
}