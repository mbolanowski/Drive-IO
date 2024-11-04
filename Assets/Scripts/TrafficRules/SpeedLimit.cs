using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedLimit : MonoBehaviour
{
    public VehicleControllerWithGears vc; // Reference to the vehicle controller
    public PlayerManager playerManager;     // Reference to the player manager
    public float _speedLimit = 50f;        // Speed limit in your desired unit (e.g., km/h)

    private float timeSinceLastCheck = 0f; // Timer to track elapsed time

    private void OnTriggerStay(Collider other)
    {
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player"))
        {
            // Increment the timer by the time that has passed since the last frame
            timeSinceLastCheck += Time.deltaTime;

            // Check if 5 seconds have passed
            if (timeSinceLastCheck >= 2f)
            {
                // Check the vehicle's current speed against the speed limit
                if ((vc.GetCurrentSpeed() * playerManager._speedMultiplier) > _speedLimit)
                {
                    playerManager.AddIncident(); // Add incident if speed exceeds limit
                    Debug.Log("You've exceeded the speed limit!");
                }

                // Reset the timer after checking
                timeSinceLastCheck = 0f;
            }
        }
    }
}