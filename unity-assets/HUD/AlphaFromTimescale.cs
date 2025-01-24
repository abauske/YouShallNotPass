using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AlphaFromTimescale : MonoBehaviour
{
    [Range(0, 1)]
    public float maxAlpha = 0.7f;
    
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    private Vector3 lastPosition;

    // Start is called before the first frame update
    void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponent<Canvas>();
        lastPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        _canvasGroup.alpha = Mathf.Max(0, Mathf.Min(1 - Time.timeScale, maxAlpha));
        _canvas.enabled = _canvasGroup.alpha != 0;
        // transform.position = _canvas.alpha == 0 ? new Vector3(lastPosition.x, lastPosition.y - 1000, lastPosition.z) : lastPosition;
    }
}
