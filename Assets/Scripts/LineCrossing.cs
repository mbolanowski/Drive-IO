using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineCrossing : MonoBehaviour
{
    private float timeSinceLastCheck = 0f; // Timer to track elapsed time

    public float checkTime;

    public PlayerManager playerManager;
    public WarningSystemController wsc;

    public bool rightOfWay = false;
    private bool eventTriggered = false; // Flag to track if the event has been triggered

    private void Start()
    {
        if (playerManager == null)
        {
            playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        }
        if (wsc == null)
        {
            wsc = GameObject.Find("Warning System").GetComponent<WarningSystemController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!eventTriggered && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("You crossed the line!");
            wsc.SetInfoText("You Crossed Over The Line");
            wsc.SetPenaltyText("-1 Life");
            playerManager.AddIncident();

            // Mark the event as triggered
            eventTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (eventTriggered && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Reset the event when the player exits the trigger area
            eventTriggered = false;
            timeSinceLastCheck = 0f; // Optional: Reset the timer as well
        }
    }
}
