using UnityEngine;
using System.Collections.Generic;

public enum TrafficLightGroup { Group1, Group2 }
public enum LightColor { Red, Yellow, Green }

public class TrafficLightManager : MonoBehaviour
{
    private static TrafficLightManager instance;
    public static TrafficLightManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TrafficLightManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TrafficLightManager");
                    instance = go.AddComponent<TrafficLightManager>();
                }
            }
            return instance;
        }
    }

    private Dictionary<TrafficLightGroup, List<TrafficLights>> trafficLightGroups = new Dictionary<TrafficLightGroup, List<TrafficLights>>();
    private Dictionary<TrafficLightGroup, LightColor> groupStates = new Dictionary<TrafficLightGroup, LightColor>();

    [Header("Light Durations (seconds)")]
    public float greenLightDuration = 6f;
    public float yellowLightDuration = 2f;
    public float redLightDuration = 8f;

    private void Awake()
    {
        // Initialize dictionaries
        trafficLightGroups[TrafficLightGroup.Group1] = new List<TrafficLights>();
        trafficLightGroups[TrafficLightGroup.Group2] = new List<TrafficLights>();

        // Set initial states - Group1 starts with Green, Group2 with Red
        groupStates[TrafficLightGroup.Group1] = LightColor.Green;
        groupStates[TrafficLightGroup.Group2] = LightColor.Red;
    }

    public void RegisterTrafficLight(TrafficLights light, TrafficLightGroup group)
    {
        if (!trafficLightGroups[group].Contains(light))
        {
            trafficLightGroups[group].Add(light);
            light.UpdateLight(groupStates[group]);
        }
    }

    public void UnregisterTrafficLight(TrafficLights light, TrafficLightGroup group)
    {
        trafficLightGroups[group].Remove(light);
    }

    /// <summary>
    /// Switches traffic light states based on the current configuration.
    /// </summary>
    public void SwitchLights()
    {
        foreach (var group in trafficLightGroups.Keys)
        {
            LightColor currentState = groupStates[group];
            LightColor nextState = GetNextLightState(currentState);
            SetGroupLights(group, nextState);
        }
    }

    /// <summary>
    /// Returns the next light state based on the current state.
    /// </summary>
    /// <param name="currentState">The current light color.</param>
    /// <returns>The next light color in the cycle.</returns>
    private LightColor GetNextLightState(LightColor currentState)
    {
        return currentState switch
        {
            LightColor.Green => LightColor.Yellow,
            LightColor.Yellow => LightColor.Red,
            LightColor.Red => LightColor.Green,
            _ => LightColor.Red, // Default fallback
        };
    }

    private void SetGroupLights(TrafficLightGroup group, LightColor color)
    {
        groupStates[group] = color;
        foreach (var light in trafficLightGroups[group])
        {
            light.UpdateLight(color);
        }
    }

    /// <summary>
    /// Gets the current light state for a given group.
    /// </summary>
    public LightColor GetLightState(TrafficLightGroup group)
    {
        return groupStates[group];
    }
}
