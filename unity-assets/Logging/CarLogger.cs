using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logging;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using Object = System.Object;

public class CarLogger : MonoBehaviour
{
    public string outputfileName = "";
    public GameObject car;

    private DataWriter writer;
    private Rigidbody rcar;

    private List<(string, object)> customLogs = new List<(string, object)>();

    private bool initialized = false;

    private List<object> values = new List<object>();
    
    private string now()
    {
        return DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.ffffff");
    }

    public void RegisterLoggedObject(object o, string prefix)
    {
        if (initialized)
        {
            throw new Exception("Cannot add logged object after logging has started!");
        }
        customLogs.Add((prefix, o));
    }
    
    void Start()
    {
        rcar = car.GetComponent<Rigidbody>();
        
        string dir = Application.dataPath + "/logs/car/";
        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch (IOException ex)
        {
            Debug.LogError(ex.Message);
        }

        string path = dir + outputfileName + "_" + now().Replace(':', '_') + ".csv";
        writer = new DataWriter(path);
    }

    void Update()
    {
        if (!initialized)
        {
            Initialize();
        }
        
        var position = car.transform.position;
        var rotation = car.transform.rotation;
        var velocity = rcar.velocity;
        var angularVelocity = rcar.angularVelocity;
        
        values[0] = now();
        values[1] = Time.time;
        values[2] = position.x;
        values[3] = position.y;
        values[4] = position.z;
        values[5] = rotation.x;
        values[6] = rotation.y;
        values[7] = rotation.z;
        values[8] = rcar.mass;
        values[9] = rcar.drag;
        values[10] = rcar.angularDrag;
        values[11] = velocity.x;
        values[12] = velocity.y;
        values[13] = velocity.z;
        values[14] = angularVelocity.x;
        values[15] = angularVelocity.y;
        values[16] = angularVelocity.z;
        values[17] = Input.GetAxis("Horizontal");
        values[18] = Input.GetAxis("Vertical");

        foreach (var (val, i) in customLogs.SelectMany(so => so.Item2.GetType().GetFields().Select(fieldInfo => fieldInfo.GetValue(so.Item2))).WithIndex())
        {
            values[i + 19] = val;
        }
        
        writer.WriteCsv(values);
    }

    private void Initialize()
    {
        List<object> keys = new List<object>{
            "DateTime",
            "t",
            "position.x",
            "position.y",
            "position.z",
            "rotation.x",
            "rotation.y",
            "rotation.z",
            "mass",
            "drag",
            "angularDrag",
            "velocity.x",
            "velocity.y",
            "velocity.z",
            "angularVelocity.x",
            "angularVelocity.y",
            "angularVelocity.z",
            "inputHorizontal",
            "inputVertical"
        };

        foreach (var (s, o) in customLogs)
        {
            foreach (var fieldInfo in o.GetType().GetFields())
            {
                keys.Add(s + "." + fieldInfo.Name);
            }
        }
        
        writer.WriteCsv(keys.ToArray());
        values = keys;
        initialized = true;
    }
}
