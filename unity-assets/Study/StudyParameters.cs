using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Valve.VR.InteractionSystem;

public enum Modality {
    NoAssistant,
    WarnOnly,
    DistanceOnly,
    WithReason,
    ExplanationRun,
    TrialRunNoCars,
    TrialRunNoAssistant,
    TrialRunWarnOnly,
    TrialRunDistanceOnly,
    TrialRunWithReason
}

public class StudyParameters : MonoBehaviour
{
    public string ParticipantName = "";
    public int ParticipantNumber = -1;
    public Modality Mod = Modality.NoAssistant;
    public int ExplanationShowIcon = -1;
    public bool ExplanationShowDist = false;
    public bool ExplanationShowReason = false;
    

    private LoggingSubject l = new LoggingSubject();
    private List<Renderer> hudRenderers = new List<Renderer>();
    private CarSpawner cars;
    private Transform hudWrapper;
    
    // Start is called before the first frame update
    void Start()
    {
        if (ParticipantName == null || ParticipantName.Trim().Length <= 0)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            throw new Exception("Participant Name not set!");
#endif
        }

        var testruns = new Modality[]
        {
            Modality.TrialRunNoCars, Modality.TrialRunNoAssistant, Modality.TrialRunWarnOnly,
            Modality.TrialRunDistanceOnly, Modality.TrialRunWithReason, Modality.ExplanationRun
        };
        if (ParticipantNumber <= 0 && !testruns.Contains(Mod))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            throw new Exception("Participant Number not set!");
#endif
        }

        l.ParticipantName = ParticipantName;
        l.Modality = Mod;
        l.ParticipantNumber = ParticipantNumber;
        
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "StudyParams");

        switch (Mod)
        {
            case Modality.TrialRunDistanceOnly:
            case Modality.DistanceOnly:
                foreach (Transform child in GameObject.Find("HUD").transform)
                {
                    if (child.name != "WarnImg" && child.name != "NoOvertakingDistance" && child.name != "CarsTooClose")
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                // FindObjectOfType<OvertakingAnalyzer>().onlyConsiderOneCar = true;
                break;
            case Modality.TrialRunWarnOnly:
            case Modality.WarnOnly:
                foreach (Transform child in GameObject.Find("HUD").transform)
                {
                    if (child.name != "WarnImg")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                break;
            case Modality.TrialRunNoAssistant:
            case Modality.NoAssistant:
                GameObject.Find("HUD").SetActive(false);
                GameObject.Find("OvertakingLine").SetActive(false);
                break;
            case Modality.WithReason:
            case Modality.TrialRunWithReason:
                // dont disable anything
                break;
            case Modality.ExplanationRun:
                hudWrapper = GameObject.Find("HUD").transform;
                foreach (Transform child in hudWrapper)
                {
                    if (child.name != "WarnImg" && child.name != "NoOvertakingDistance" && child.gameObject.activeSelf)
                    {
                        hudRenderers.Add(child.GetComponent<Renderer>());
                    }
                }
                hudWrapper.GetComponentsInChildren<ShowWarnImg>().ForEach(s => s.enabled = false);
                hudWrapper.GetComponentsInChildren<ShowWarnReason>().ForEach(s => s.enabled = false);
                hudWrapper.GetComponentsInChildren<OvertakingText>().ForEach(s => s.enabled = false);
                cars = FindObjectOfType<CarSpawner>();

                var mytransform = transform;
                var egoCarTransform = cars.egoCar.transform;
                egoCarTransform.position = mytransform.position;
                var fwd = mytransform.forward;
                egoCarTransform.forward = fwd;
                
                var firstCar = cars.carPrototypesHolder.GetComponentInChildren<AutoSteer>().GetComponent<Rigidbody>();
                firstCar.position = egoCarTransform.position + fwd * 20;
                firstCar.transform.forward = fwd;
                firstCar.GetComponent<Rigidbody>().velocity = Vector3.zero;
                
                egoCarTransform.GetComponent<RCC_CarControllerV3>().enabled = false;
                egoCarTransform.GetComponent<Rigidbody>().isKinematic = true;
                FindObjectsByType<AutoSteer>(FindObjectsSortMode.None).ForEach(a => a.enabled = false);
                Destroy(cars);
                break;
            case Modality.TrialRunNoCars:
                foreach (Transform child in FindObjectOfType<CarSpawner>().carPrototypesHolder.transform)
                {
                    child.gameObject.SetActive(false);
                }
                break;
        }
    }

    private void Update()
    {
        if (Mod != Modality.ExplanationRun)
        {
            return;
        }

        
        int secs = (int)Time.time;
        var showIcon = ExplanationShowIcon < 0 || ExplanationShowIcon >= hudRenderers.Count ? (secs / 2) % hudRenderers.Count : ExplanationShowIcon;
        hudRenderers.ForEach(r => r.enabled = false);
        hudRenderers[showIcon].enabled = ExplanationShowReason;

        hudWrapper.GetComponentsInChildren<TextMeshPro>().ForEach(s =>
        {
            s.text = ExplanationShowDist ? "342m" : "";
            s.color = (secs + 3) % 5 < 2 ? new Color(1.0f, 165f / 255f, 0.0f, 1f) : Color.red;
        });
        
        var mytransform = transform;
        var egoCarTransform = cars.egoCar.transform;
        egoCarTransform.position = mytransform.position;
        egoCarTransform.forward = mytransform.forward;
    }

    private class LoggingSubject
    {
        public string ParticipantName;
        public Modality Modality;
        public int ParticipantNumber;
    }
}
