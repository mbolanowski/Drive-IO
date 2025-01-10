using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangingLanes : MonoBehaviour
{
    public enum LanePosition { Left, Center, Right }

    public LanePosition lanePosition; // Determine the lane position
    public VehicleControllerWithGears vc; // Reference to the vehicle controller
    public PlayerManager playerManager;     // Reference to the player manager
    public WarningSystemController wsc;

    private void Start()
    {
        if (wsc == null)
        {
            wsc = GameObject.Find("Warning System").GetComponent<WarningSystemController>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player"))
        {
            switch (lanePosition)
            {
                case LanePosition.Left:
                    HandleLeftLaneChange();
                    break;
                case LanePosition.Center:
                    HandleCenterLaneChange();
                    break;
                case LanePosition.Right:
                    HandleRightLaneChange();
                    break;
            }
        }
    }

    private void HandleLeftLaneChange()
    {
        if (!vc.GetIsLeftBlinkerOn() && playerManager.lanePosition != "Left")
        {
            playerManager.AddIncident();
            wsc.SetInfoText("Changed Lane Without Blinker");
            wsc.SetPenaltyText("-1 Life");
            Debug.Log("Left Blinker isn't on!");
        }
        playerManager.SetCurrentLane("Left");
    }

    private void HandleCenterLaneChange()
    {
        if(playerManager.lanePosition == "Left")
        {
            if (!vc.GetIsRightBlinkerOn())
            {
                playerManager.AddIncident();
                wsc.SetInfoText("Changed Lane Without Blinker");
                wsc.SetPenaltyText("-1 Life");
                Debug.Log("Right Blinker isn't on!");
            }
        }
        else if(playerManager.lanePosition == "Right")
        {
            if (!vc.GetIsLeftBlinkerOn())
            {
                playerManager.AddIncident();
                wsc.SetInfoText("Changed Lane Without Blinker");
                wsc.SetPenaltyText("-1 Life");
                Debug.Log("Left Blinker isn't on!");
            }
        }
        playerManager.SetCurrentLane("Center");
    }

    private void HandleRightLaneChange()
    {
        if (!vc.GetIsRightBlinkerOn() && playerManager.lanePosition != "Right")
        {
            playerManager.AddIncident();
            wsc.SetInfoText("Changed Lane Without Blinker");
            wsc.SetPenaltyText("-1 Life");
            Debug.Log("Right Blinker isn't on!");
        }
        playerManager.SetCurrentLane("Right");
    }
}