using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class Notes : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string notes;
    }
}