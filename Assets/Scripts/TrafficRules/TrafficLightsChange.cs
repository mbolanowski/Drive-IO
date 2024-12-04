using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightsChange : MonoBehaviour
{
    // This will define the angle tolerance to consider the directions "roughly the same"
    public float angleTolerance = 15f; // in degrees

    public PlayerManager pm;
    private void Start()
    {
        if (pm == null)
        {
            pm = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player"))
        {
            // Get the forward vector of the trigger (this object)
            Vector3 triggerForward = transform.forward;

            // Get the forward vector of the object that entered the trigger
            Vector3 objectForward = other.transform.forward;

            // Calculate the angle between the two forward vectors
            float angle = Vector3.Angle(triggerForward, objectForward);

            // Check if the angle is within the defined tolerance
            if (angle <= angleTolerance)
            {
                LightColor lightColor = transform.parent.GetComponent<TrafficLights>().activeLight;
                if (lightColor != null)
                {
                    if (lightColor == LightColor.Red)
                    {
                        Debug.Log("You crossed during a red light!");
                        pm.AddIncident();
                    }
                }
            }
        }
    }
}
