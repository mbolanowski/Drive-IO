using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int _incidents = 0;
    public TextMeshProUGUI incidentCount;

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
        incidentCount.text = _incidents.ToString();
    }
}
