using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightColor { Red, Yellow, Green, None }
public class TrafficLights : MonoBehaviour
{

    public LightColor activeLight;
    private LightColor lastActiveLight;
    private MeshRenderer mr;

    private void Start()
    {
        mr = GetComponent<MeshRenderer>();
        lastActiveLight = activeLight;
        SetLight(activeLight);
    }

    private void Update()
    {
        // Check if the active light has changed
        if (activeLight != lastActiveLight)
        {
            SetLight(activeLight);   // Update the light visuals
            lastActiveLight = activeLight; // Update the tracker
        }
    }

    public void SetLight(LightColor color)
    {
        // Set colors for active and inactive states
        Color greenColor = color == LightColor.Green ? Color.green : Color.black;
        Color yellowColor = color == LightColor.Yellow ? Color.yellow : Color.black;
        Color redColor = color == LightColor.Red ? Color.red : Color.black;

        // Apply colors to the corresponding materials
        mr.materials[1].color = greenColor;   // Green light material
        mr.materials[2].color = yellowColor;  // Yellow light material
        mr.materials[3].color = redColor;     // Red light material
    }
}