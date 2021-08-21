using ForLoopCowboyCommons.EditorHelpers;
using UnityEngine;

[CreateAssetMenu]
public class Army : ScriptableObject, IHasLayer
{
    public string key = "Untitled Army";

    [SerializeField] private LayerHelper layerConfiguration = new LayerHelper("Everything");
    
    public int Layer => layerConfiguration.Layer;
    public LayerMask LayerMask => layerConfiguration.LayerMask;
}
