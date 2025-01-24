using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class OvertakingText : MonoBehaviour
{
    public OvertakingAnalyzer overtaking;
    private TextMeshPro textHolder;
    
    // Start is called before the first frame update
    void Start()
    {
        textHolder = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        if (overtaking.Status == OvertakingStatus.StayInLane)
        {
            textHolder.color = overtaking.isSureOvertakingSection ? new Color(1.0f, 165f / 255f, 0.0f, 1f) : Color.red;
            
            var dist = overtaking.DistToNextOvertaking;
            if (dist < 10000)
            {
                textHolder.text = string.Format("{0}m", (int)dist);
                return;
            }

            dist /= 1000;
            if (dist < 100)
            {
                textHolder.text = string.Format("{0}km", (int)dist);
                return;
            }
        }
        textHolder.text = "";
    }
}
