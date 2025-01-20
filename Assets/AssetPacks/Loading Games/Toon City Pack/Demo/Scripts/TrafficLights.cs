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

        // Create or reuse a MaterialPropertyBlock
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        // Set the color for the respective lights
        Color greenColor = color == LightColor.Green ? Color.green : Color.black;
        Color yellowColor = color == LightColor.Yellow ? Color.yellow : Color.black;
        Color redColor = color == LightColor.Red ? Color.red : Color.black;

        // Apply the color to the appropriate material slots
        mr.GetPropertyBlock(block, 1); // Green light material
        block.SetColor("_Color", greenColor);
        mr.SetPropertyBlock(block, 1);

        mr.GetPropertyBlock(block, 2); // Yellow light material
        block.SetColor("_Color", yellowColor);
        mr.SetPropertyBlock(block, 2);

        mr.GetPropertyBlock(block, 3); // Red light material
        block.SetColor("_Color", redColor);
        mr.SetPropertyBlock(block, 3);

        // Update the current active color
        currentColor = color == LightColor.Green ? Color.green :
                       color == LightColor.Yellow ? Color.yellow :
                       color == LightColor.Red ? Color.red : Color.black;
    }
}
