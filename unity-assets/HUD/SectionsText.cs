using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SectionsText : MonoBehaviour
{
    private SectionHandler _sections;
    private Text _textHolder;
    private int _lastHandledSection = -2;

    // Start is called before the first frame update
    void Start()
    {
        _sections = FindObjectOfType<SectionHandler>();
        _textHolder = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        var cur = _sections.handledSectionsCount;
        var max = cur + _sections.unhandledSectionCount;
        if(cur == _lastHandledSection) return;
        _lastHandledSection = cur;
        if (cur >= max)
        {
            _textHolder.text = "Last checkpoint reached!\nThank you for participating!";
        } else if (cur <= 0)
        {
            _textHolder.text = "Thank you for participating!\nOnce you are ready please\npress right or P to continue";
        }
        else
        {
            _textHolder.text = string.Format("Checkpoint {0} of {1} reached!\nPress right or P to continue", cur, max);
        }
    }
}
