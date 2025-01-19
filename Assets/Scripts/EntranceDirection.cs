using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class EntranceDirection : MonoBehaviour
{
    public int direction;
    public bool priority;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AutonomousVehicle")
        {
            other.GetComponent<VehicleAI>().intersectionEntranceDirection = direction;
            other.GetComponent<VehicleAI>().hasPriority = priority;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
        }
    }
}
