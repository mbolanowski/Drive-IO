using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrafficLights : MonoBehaviour
{
    private MeshRenderer mr;
    public LightColor currentLight { get; private set; }

    [SerializeField]
    private TrafficLightGroup group; // Set this in the Inspector

    public Color currentColor { get; private set; } // New variable to track current color

    private void Start()
    {
        mr = GetComponent<MeshRenderer>();
        TrafficLightManager.Instance.RegisterTrafficLight(this, group);
    }

    private void OnDestroy()
    {
        if (TrafficLightManager.Instance != null)
        {
            TrafficLightManager.Instance.UnregisterTrafficLight(this, group);
        }
    }

    public void UpdateLight(LightColor color)
    {
        currentLight = color;

        // Set colors for active and inactive states
        Color greenColor = color == LightColor.Green ? Color.green : Color.black;
        Color yellowColor = color == LightColor.Yellow ? Color.yellow : Color.black;
        Color redColor = color == LightColor.Red ? Color.red : Color.black;

        // Store the current active color
        if (color == LightColor.Green)
            currentColor = Color.green;
        else if (color == LightColor.Yellow)
            currentColor = Color.yellow;
        else if (color == LightColor.Red)
            currentColor = Color.red;
        else
            currentColor = Color.black;

        // Apply colors to the corresponding materials
        mr.materials[1].color = greenColor;   // Green light material
        mr.materials[2].color = yellowColor;  // Yellow light material
        mr.materials[3].color = redColor;     // Red light material
    }
}
