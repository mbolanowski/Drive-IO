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

    private bool transitionPhase = false; // Tracks whether we are in the yellow phase

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
        if (!transitionPhase)
        {
            // Change all green lights to yellow, keep red lights as red
            foreach (var group in trafficLightGroups.Keys)
            {
                if (groupStates[group] == LightColor.Green)
                {
                    SetGroupLights(group, LightColor.Yellow);
                }
            }
            transitionPhase = true;
        }
        else
        {
            // Change yellow lights to red, and red lights to green
            foreach (var group in trafficLightGroups.Keys)
            {
                if (groupStates[group] == LightColor.Yellow)
                {
                    SetGroupLights(group, LightColor.Red);
                }
                else if (groupStates[group] == LightColor.Red)
                {
                    SetGroupLights(group, LightColor.Green);
                }
            }
            transitionPhase = false;
        }
    }

    public void SetGroupLights(TrafficLightGroup group, LightColor color)
    {
        groupStates[group] = color;
        foreach (var light in trafficLightGroups[group])
        {
            light.UpdateLight(color);
        }
    }

    public void CommunicateWithServer(int currentGroup, int previousGroup)
    {

        if (currentGroup == 1 && previousGroup == 3)
        {
            SetGroupLights(TrafficLightGroup.Group1, LightColor.Yellow);
            SetGroupLights(TrafficLightGroup.Group2, LightColor.Red);
        }
        else if (currentGroup == 2 & previousGroup == 1)
        {
            SetGroupLights(TrafficLightGroup.Group1, LightColor.Red);
            SetGroupLights(TrafficLightGroup.Group2, LightColor.Green);
        }
        else if (currentGroup == 2 && previousGroup == 3)
        {
            SetGroupLights(TrafficLightGroup.Group1, LightColor.Red);
            SetGroupLights(TrafficLightGroup.Group2, LightColor.Yellow);
        }
        else if (currentGroup == 1 && previousGroup == 2)
        {
            SetGroupLights(TrafficLightGroup.Group1, LightColor.Green);
            SetGroupLights(TrafficLightGroup.Group2, LightColor.Red);
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
