using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class EntranceDirection : MonoBehaviour
{
    public int direction;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AutonomousVehicle")
        {
            other.GetComponent<VehicleAI>().intersectionEntranceDirection = direction;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.GetComponent<VehicleControllerWithGears>().intersectionEntranceDirection = direction;
        }
    }
}
