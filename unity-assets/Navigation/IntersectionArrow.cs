using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class IntersectionArrow : MonoBehaviour
{
    public Navigation pathPlanner;
    public Transform userCar;
    public float maxRenderDist = 300;
    public float horizontalOffset = 10;
    public float verticalOffset = 10;

    private float time = 9.9f;
    private Renderer _renderer;
    private NavigationPath navPath;
    
    // Start is called before the first frame update
    void Start()
    {
        pathPlanner.AddChangeListener(Pathchange);
        
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time < 5 || navPath == null || navPath.Count < 2) return;
        time -= 5;

        var section = navPath[0];
        var road = section.Road;
        var prg = section.EndProgress;
        var endPos = road.Pos(prg);

        if ((userCar.position - endPos).sqrMagnitude > maxRenderDist * maxRenderDist)
        {
            _renderer.enabled = false;
            return;
        }

        var endDir = road.CenterSpline.GetDirection(prg);
        var nextSection = navPath[1];
        float angle = Vector3.SignedAngle(endDir, nextSection.Road.Pos(nextSection.StartProgress + 10) - endPos,
            Vector3.up);
        bool toRight = angle > 0;

        if (Mathf.Abs(angle) < 30)
        {
            _renderer.enabled = false;
            return;
        }
        
        _renderer.enabled = true;
        var t = transform;
        t.position = endPos + endDir * horizontalOffset + new Vector3(0, verticalOffset, 0);
        t.rotation = Quaternion.LookRotation(-1 * endDir);
        t.Rotate(toRight ? -90 : 90, 90, 90);
    }

    void Pathchange(NavigationPath path)
    {
        navPath = path;
    }
}
