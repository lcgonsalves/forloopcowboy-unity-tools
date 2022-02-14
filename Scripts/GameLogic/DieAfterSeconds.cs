using System;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using UnityEngine;

public class DieAfterSeconds : ManagedMonoBehaviour
{

    public float timer = 5f;
    private DateTime startedAt;

    // Start is called before the first frame update
    void Start()
    {
        startedAt = DateTime.Now;
    }
    
    public override bool ShouldDestroy()
    {
        var b = ((DateTime.Now - startedAt).TotalSeconds >= timer);

        Debug.Log($"{startedAt} -> {DateTime.Now} : {(startedAt - DateTime.Now).TotalSeconds} :: {timer} {b}");  
        
        return b;
    }

}
