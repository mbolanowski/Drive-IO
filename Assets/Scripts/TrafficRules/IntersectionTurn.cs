using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class IntersectionTurn : MonoBehaviour
{
    public VechicleManager vm;
    public PlayerManager pm;

    public bool leftTurnAllowed = true;
    public bool rightTurnAllowed = true;
    public bool straightAllowed = true;
    public bool uTurnAllowed = true;

    private void Start()
    {
        if (pm == null)
        {
            pm = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        }
        if (vm == null)
        {
            vm = GameObject.Find("VechicleManager").GetComponent<VechicleManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player"))
        {
            Vector3 objectForward = other.transform.forward;
            Vector3 triggerForward = transform.forward;

            float dotProduct = Vector3.Dot(objectForward.normalized, triggerForward);

            if (dotProduct < -0.7f)  // Opposite direction
            {
                vm._lastForewardVector = other.transform.forward;
                if (vm.GetIsLeftBlinkerOn()) vm._declaredDirection = "left";
                else if (vm.GetIsRightBlinkerOn()) vm._declaredDirection = "right";
                else vm._declaredDirection = "straight";

                vm.leftAllowed = leftTurnAllowed;
                vm.rightAllowed = rightTurnAllowed;
                vm.straightAllowed = straightAllowed;
                vm.uAllowed = uTurnAllowed;
            }
            if (dotProduct > 0.7f)
            {
                float dotProduct2 = Vector3.Dot(vm._lastForewardVector, triggerForward);
                Vector3 crossProduct = Vector3.Cross(vm._lastForewardVector, other.transform.forward);

                if (dotProduct2 > 0.9f)  // Adjust threshold for "going straight" as needed
                {
                    if (vm._declaredDirection != "straight")
                    {
                        Debug.Log("You went straight despite using a blinker!");
                        pm.AddIncident();
                    }
                    else if (vm._declaredDirection == "straight" && !vm.straightAllowed)
                    {
                        Debug.Log("Going straight is not allowed here!");
                        pm.AddIncident();
                    }
                }
                // Turning Right
                else if (crossProduct.y > 0.7f)  // If cross product's y component is negative, it indicates a right turn
                {
                    if (vm._declaredDirection != "right")
                    {
                        Debug.Log("You turned right without using the correct blinker!");
                        pm.AddIncident();
                    }
                    else if (vm._declaredDirection == "right" && !vm.rightAllowed)
                    {
                        Debug.Log("Going right is not allowed here!");
                        pm.AddIncident();
                    }
                }
                // Turning Left
                else if (crossProduct.y < -0.7f)  // If cross product's y component is positive, it indicates a left turn
                {
                    if (vm._declaredDirection != "left")
                    {
                        Debug.Log("You turned left without using the correct blinker!");
                        pm.AddIncident();
                    }
                    else if (vm._declaredDirection == "left" && !vm.leftAllowed)
                    {
                        Debug.Log("Going left is not allowed here!");
                        pm.AddIncident();
                    }
                }
                vm._lastForewardVector = Vector3.zero;
                vm._declaredDirection = "empty";

                vm.leftAllowed = false;
                vm.rightAllowed = false;
                vm.straightAllowed = false;
                vm.uAllowed = false;
            }
        }
    }
}
