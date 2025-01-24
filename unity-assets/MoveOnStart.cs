using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveOnStart : MonoBehaviour
{
    public Vector3 amount;
    public string volatileChildrenName = "BridgeBase";

    // Start is called before the first frame update
    void Start()
    {
        transform.Find(volatileChildrenName).transform.position += amount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
