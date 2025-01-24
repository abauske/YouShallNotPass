using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpposingSpeedup : MonoBehaviour
{
    public float speedupDist = 2000;
    public float duration = 10;
    public float speedKmh = 105;
    
    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<Navigation>().AddChangeListener(path =>
        {
            if(path == null) return;
            float handled = 0;
            for (int i = 0; i < path.Count && handled < speedupDist; handled += path[i++].Length)
            {
                path[i].Road.oppositeDirection.cars.ForEach(a => a.ModifySpeed(speedKmh / 3.6f, duration));
            }
        });
    }
}
