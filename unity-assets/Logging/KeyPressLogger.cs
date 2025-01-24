using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyPressLogger : MonoBehaviour
{
    public string name;
    public KeyCode kk;

    private LoggingSubject l = new();
    
    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, name);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(kk))
        {
            l.val++;
        }
    }

    private class LoggingSubject
    {
        public int val = 0;
    }
}
