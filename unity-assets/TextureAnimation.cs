using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureAnimation : MonoBehaviour
{
    public float ScrollSpeedX = 1;
    public float ScrollSpeedY = 0;
    
    
    private Renderer renderer;
    
    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        renderer.material.mainTextureOffset = new Vector2(Time.time * ScrollSpeedX, Time.time * ScrollSpeedY);
    }
}
