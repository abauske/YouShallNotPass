using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Blinker : MonoBehaviour
{
    private AutoSteer _ai;
    private bool isLeft;
    private AutoSteer.TurnDirection activeDir;
    private Renderer _renderer;
    private float t;

    // Start is called before the first frame update
    void Start()
    {
        _ai = GetComponentInParent<AutoSteer>();
        _renderer = GetComponent<Renderer>();
        isLeft = gameObject.name.ContainsInsensitive("left");
        activeDir = isLeft ? AutoSteer.TurnDirection.LEFT : AutoSteer.TurnDirection.RIGHT;
    }

    // Update is called once per frame
    void Update()
    {
        if (_ai.nextRoadTurn == activeDir || (!isLeft && _ai.parkForSecs > 0))
        {
            t += Time.deltaTime;
            _renderer.enabled = t < 0.5f;
            if (t > 1) t -= 1;
        }
        else
        {
            _renderer.enabled = false;
        }
    }
}
