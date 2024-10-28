using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDirectionCheck : MonoBehaviour
{
    private float timeSinceLastCheck = 0f; // Timer to track elapsed time

    public float checkTime;

    public PlayerManager playerManager;

    private void OnTriggerStay(Collider other)
    {
        // Increment the timer by the time that has passed since the last frame
        timeSinceLastCheck += Time.deltaTime;

        // Check if 5 seconds have passed
        if (timeSinceLastCheck >= checkTime)
        {
            // Ensure the object inside the trigger has a Transform (it should by default)
            if (other.transform != null)
            {
                // Get the forward vector of the object inside the trigger
                Vector3 objectForward = other.transform.forward;

                // Get the forward vector of the trigger object (this script's object)
                Vector3 triggerForward = transform.forward;

                // Use Vector3.Dot to compare their forward directions
                float dotProduct = Vector3.Dot(objectForward.normalized, triggerForward);

                // Check if the object's forward vector is aligned with the trigger's forward vector
                if (dotProduct > 0.7f)  // Same direction
                {

                }
                else if (dotProduct < -0.7f)  // Opposite direction
                {
                    Debug.Log("You're going the wrong way!");
                    playerManager.AddIncident();
                }
                else
                {
                    Debug.Log("You're sideways!");
                }
            }
            else
            {
                Debug.Log("No valid transform for " + other.gameObject.name);
            }

            // Reset the timer after checking
            timeSinceLastCheck = 0f;
        }
    }
}