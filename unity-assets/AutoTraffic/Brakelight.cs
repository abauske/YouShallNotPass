using System;
using UnityEditor;
using UnityEngine;

public class Brakelight : MonoBehaviour
{
    private Rigidbody _rb;
    private Renderer _renderer;
    private float[] _lastVelocities = new float[50];
    private int _lastVelIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponentInParent<AutoSteer>().GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var v = _rb.velocity.magnitude;
        var nextVelIndex = (_lastVelIndex + 1) % _lastVelocities.Length;
        var a = (v - _lastVelocities[nextVelIndex]) / (Time.fixedDeltaTime * _lastVelocities.Length);
        _renderer.enabled = a < -1.5f;
        _lastVelIndex = nextVelIndex;
        _lastVelocities[_lastVelIndex] = v;
    }
}
