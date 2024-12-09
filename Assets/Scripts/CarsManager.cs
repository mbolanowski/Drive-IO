using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarsManager : MonoBehaviour
{
    public PlayerManager pm;
    public VechicleManager vm;

    private void AddIncident()
    {
        pm.AddIncident();
    }
}
