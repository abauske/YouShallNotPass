using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class CrashText : MonoBehaviour
{
    public float crashWaitTime = 5;
    private Canvas _canvas;
    private Text _textHolder;
    private ResetCar _carReset;
    private DateTime continueTime;
    private CarSpawner _cars;

    // Start is called before the first frame update
    void Start()
    {
        _canvas = GetComponent<Canvas>();
        _textHolder = GetComponentInChildren<Text>();
        _carReset = FindObjectOfType<ResetCar>();
        continueTime = DateTime.Now;
        _cars = FindObjectOfType<CarSpawner>();
    }

    // Update is called once per frame
    void Update()
    {
        var remaining = (continueTime - DateTime.Now).Seconds;
        if(remaining > 0) {
            Time.timeScale = 0;
            _canvas.enabled = true;
            _textHolder.text = "You crashed hard!\nWait " + (int) remaining + " seconds to continue";
        } else if(_canvas.enabled) {
            Time.timeScale = 1;
            _canvas.enabled = false;
            var pos = transform.position;
            _carReset.DoReset();
            _cars.AllCars.Where(c => (c.position - pos).sqrMagnitude < 10 * 10).ForEach(c => c.position += new Vector3(10000, 0, 0));
        }
    }

    public void SetCrashed() {
        continueTime = DateTime.Now.AddSeconds(crashWaitTime);
    }
}
