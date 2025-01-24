using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudyDisplay : MonoBehaviour
{
    private TMP_Text text;
    private SectionHandler sections;
    private StudyParameters param;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
        sections = FindObjectOfType<SectionHandler>();
        param = FindObjectOfType<StudyParameters>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = DateTime.Now.ToString() + " sec: " + sections.currentActiveSection + " part: " + param.ParticipantNumber;
    }
}
