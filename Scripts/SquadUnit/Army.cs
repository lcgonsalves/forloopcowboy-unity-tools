using UnityEngine;

[CreateAssetMenu]
public class Army : ScriptableObject
{
    public string key = "Untitled Army";

    [SerializeField]
    private string layerName;

    public int Layer { get => LayerMask.NameToLayer(layerName); }

    public LayerMask LayerMask { get => 1 << Layer; }

}
