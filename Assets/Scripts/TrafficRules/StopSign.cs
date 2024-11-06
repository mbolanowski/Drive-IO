using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopSign : MonoBehaviour
{
    // Flag to check if the object is inside the stop sign collider and should stop.
    private bool isInside = false;

    // Time threshold for checking if the object stopped.
    public float stopThreshold = 0.1f;

    // Reference to the object inside the collider
    private Rigidbody objectRigidbody;

    void OnTriggerEnter(Collider other)
    {
        // Check if the other object has a Rigidbody (i.e., a movable object).
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) // Assuming "Player" is the tag for the object
        {
            objectRigidbody = other.gameObject.GetComponent<Rigidbody>();
            isInside = true;

            // Start checking if the object stops
            StartCoroutine(CheckIfStopped());
        }
    }

    void OnTriggerExit(Collider other)
    {
        // If the object exits the collider, reset the flag.
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isInside = false;
        }
    }

    // Coroutine to check if the object has stopped inside the collider
    private IEnumerator CheckIfStopped()
    {
        // Wait for a moment to see if the object stops inside
        float timePassed = 0f;

        while (isInside && timePassed < stopThreshold)
        {
            if (objectRigidbody.velocity.magnitude < 0.1f) // If the object has stopped moving
            {
                timePassed += Time.deltaTime;
            }
            else
            {
                timePassed = 0f; // Reset if the object starts moving again
            }
            yield return null;
        }

        if (timePassed >= stopThreshold)
        {
            // Object has stopped inside the collider
            Debug.Log("Object stopped inside the stop sign!");
            // You can add logic here to make the object continue or perform actions
        }
        else
        {
            // Object never stopped inside the collider
            Debug.Log("Object didn't stop inside the stop sign!");
        }
    }
}
