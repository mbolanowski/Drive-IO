using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class EntranceDirection : MonoBehaviour
{
    public int direction;
    public bool priority;
    public bool horizontal;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AutonomousVehicle")
        {
            other.GetComponent<VehicleAI>().intersectionEntranceDirection = direction;
            other.GetComponent<VehicleAI>().hasPriority = priority;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.GetComponent<VehicleControllerWithGears>().intersectionEntranceDirection = direction;
            GameObject.Find("PlayerManager").GetComponent<PlayerManager>()._hasRightOfWay = priority;
            other.GetComponent<VehicleControllerWithGears>()._isHorizontal = horizontal;
        }
    }
}
