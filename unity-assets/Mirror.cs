using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mirror : MonoBehaviour
{
    [System.Serializable]
    public class MirrorPane
    {
        public Transform transform;
        public float nearClipPane = 0.3f;
    }
    
    public List<MirrorPane> mirrors = new List<MirrorPane>();
    private Transform head;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        head = GameObject.Find("MainCamera").transform;
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        var tr = transform;

        var headPos = head.position;
        MirrorPane mir = mirrors.MinBy(m => Vector3.Angle(head.forward, m.transform.position - headPos));
        var mirTransform = mir.transform;
        var mirPos = mirTransform.position;
        var camToPane = mirPos - headPos;
        
        var normal = mirTransform.forward;
        tr.forward = camToPane - 2 * Vector3.Dot(camToPane, normal) * normal;
        tr.position = mirPos;
        cam.nearClipPlane = mir.nearClipPane;
    }
}
