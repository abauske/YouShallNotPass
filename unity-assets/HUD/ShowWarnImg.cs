using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ShowWarnImg : MonoBehaviour
{
    private OvertakingAnalyzer overtaking;
    private SpriteRenderer renderer;
    public List<OvertakingStatus> showCondition = new List<OvertakingStatus> {OvertakingStatus.StayInLane};
    
    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        overtaking = FindObjectOfType<OvertakingAnalyzer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (showCondition.Any(c => c == overtaking.Status))
        {
            renderer.enabled = true;
        }
        else
        {
            renderer.enabled = false;
        }
    }
}
