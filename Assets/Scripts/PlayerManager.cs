using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int _incidents = 0;
    public TextMeshProUGUI incidentCount;

    public float _speedMultiplier = 12f;

    public bool isInLeftLane = false;

    public void AddIncident()
    {
        _incidents++;
    }

    public int GetIncidents()
    {
    return _incidents; 
    }

    private void Update()
    {
        incidentCount.text = "Wykroczenia: " + _incidents.ToString();
    }

    public bool GetIsInLeftLane()
    {
        return isInLeftLane;
    }

    public void SetIsInLeftLane(bool set)
    {
        isInLeftLane = set;
    }
}
