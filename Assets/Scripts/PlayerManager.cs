using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int _incidents = 0;
    public TextMeshProUGUI incidentCount;

    public float _speedMultiplier = 12f;

    public string lanePosition;

    public bool _hasRightOfWay = false;

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

    public string GetCurrentLane()
    {
        return lanePosition;
    }

    public void SetCurrentLane(string set)
    {
        lanePosition = set;
    }

    public void SetHasRightOfWay(bool set)
    {
        _hasRightOfWay = set;
    }

    public bool GetHasRightOfWay()
    {
        return _hasRightOfWay;
    }
}
