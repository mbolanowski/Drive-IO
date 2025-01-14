using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class AIIntent : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AutonomousVehicle")
        {
            if (other.gameObject.GetComponent<VehicleAI>().futureSteering > 0.3)
            {
                //GetComponentInParent<Intersection>().vehiclesTurningRight.Add(other.gameObject);
                other.gameObject.GetComponent<VehicleAI>()._TurningRight = true;
            }
            else if (other.gameObject.GetComponent<VehicleAI>().futureSteering < -0.3)
            {
                //GetComponentInParent<Intersection>().vehiclesTurningLeft.Add(other.gameObject);
                other.gameObject.GetComponent<VehicleAI>()._TurningLeft = true;
            }
            else
            {
                //GetComponentInParent<Intersection>().vehiclesTurningStraight.Add(other.gameObject);
                other.gameObject.GetComponent<VehicleAI>()._TurningStraight = true;
            }
        }
    }
}
