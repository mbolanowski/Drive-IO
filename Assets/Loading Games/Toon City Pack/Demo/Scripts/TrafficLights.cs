using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightColor { Red, Yellow, Green, None }

public class TrafficLights : MonoBehaviour
{
    public LightColor activeLight;  // This can be set from the Inspector to define the starting light
    private LightColor lastActiveLight;
    private MeshRenderer mr;

    // Expose light durations to the editor
    [Header("Light Durations (seconds)")]
    public float greenLightDuration = 10f;
    public float yellowLightDuration = 2f;
    public float redLightDuration = 10f;

    private void Start()
    {
        mr = GetComponent<MeshRenderer>();
        lastActiveLight = activeLight;

        // Set the initial light based on the selected active light
        SetLight(activeLight);

        // Start the coroutine to cycle lights
        StartCoroutine(TrafficLightCycle());
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

    private IEnumerator TrafficLightCycle()
    {
        while (true)
        {
            switch (activeLight)
            {
                case LightColor.Green:
                    yield return new WaitForSeconds(greenLightDuration);
                    activeLight = LightColor.Yellow;
                    break;
                case LightColor.Yellow:
                    yield return new WaitForSeconds(yellowLightDuration);
                    activeLight = LightColor.Red;
                    break;
                case LightColor.Red:
                    yield return new WaitForSeconds(redLightDuration);
                    activeLight = LightColor.Green;
                    break;
                default:
                    yield return null;
                    break;
            }
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
