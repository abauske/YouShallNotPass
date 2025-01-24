using System.Linq;
using UnityEngine;

public class OvertakingLine : MonoBehaviour
{
    public OvertakingAnalyzer analyzer;
    private LineRenderer _lineRenderer;
    void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
        analyzer.AddPathListener(Pathchange);
    }

    void Pathchange(OvertakingStartSection section)
    {
        if (section == null)
        {
            _lineRenderer.positionCount = 0;
            return;
        }
        var points = section.Road.CenterSpline.EvenlyDistribute(20f, section.StartProgress, section.EndProgress)
            .Select(x => new Vector3(x.x, transform.position.y, x.z)).ToArray();
        _lineRenderer.positionCount = points.Length;
        _lineRenderer.SetPositions(points);
    }
}
