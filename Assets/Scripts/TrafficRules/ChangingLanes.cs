using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangingLanes : MonoBehaviour
{

    public bool isLeft = false;
    public VehicleControllerWithGears vc; // Reference to the vehicle controller
    public PlayerManager playerManager;     // Reference to the player manager

    private void OnTriggerEnter(Collider other)
    {
        if (isLeft)
        {
            if (!vc.GetIsLeftBlinkerOn() && !playerManager.GetIsInLeftLane())
            {
                playerManager.AddIncident();
                Debug.Log("Left Blinker isnt on!");
            }
            playerManager.SetIsInLeftLane(true);
        }
        else
        {
            if (!vc.GetIsRightBlinkerOn() && playerManager.GetIsInLeftLane())
            {
                playerManager.AddIncident();
                Debug.Log("Right Blinker isnt on!");
            }
            playerManager.SetIsInLeftLane(false);
        }
    }
}
