using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveGameObject : MonoBehaviour
{
    // The GameObject to move
    public GameObject targetObject;
    public VechicleManager vm;
    public Minimap mm;
    public PlayerManager pm;

    // The amount to subtract from the x position
    private const float OffsetX = 13.62f;
    private const float OffsetZ = 2.9942f;

    private bool isInitialized = false;


    private void Start()
    {

        if (pm == null)
        {
            pm = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        }
        // Mark as initialized after the first frame
        //StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();
        isInitialized = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (pm._justSpawned)    
        {
            if (mm.gameObject.active)
            {
                Debug.Log("What");
                mm.StartBlinkingTile(vm.GetCurrentTileX(), vm.GetCurrentTileY(), vm.vehicleColor, 0.5f);
                pm._justSpawned = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (pm._justSpawned)
            {
                MoveTargetObject();
                vm.SetCurrentTile(gameObject.name[0] - '0', gameObject.name[1] - '0');
            }
            else
            {
                MoveTargetObject();
                mm.StopBlinkingTile(vm.GetCurrentTileX(), vm.GetCurrentTileY());

                if (!pm._dead)
                {
                    mm.SetTileColor(vm.GetCurrentTileX(), vm.GetCurrentTileY(), vm.vehicleColor);
                    pm.AssignTile(vm.GetCurrentTileX(), vm.GetCurrentTileY());
                }
                vm.SetCurrentTile(gameObject.name[0] - '0', gameObject.name[1] - '0');
                mm.StartBlinkingTile(vm.GetCurrentTileX(), vm.GetCurrentTileY(), vm.vehicleColor, 0.5f);
                pm._dead = false;
            }
        }
    }

    // Function to move the target GameObject
    private void MoveTargetObject()
    {
        if (targetObject != null)
        {
            // Get the current position of the target object
            Vector3 currentPosition = transform.position;
            currentPosition.y = targetObject.transform.position.y;

            // Update the x position by subtracting 13.62
           // currentPosition.x -= OffsetX;
            currentPosition.z -= OffsetZ;

            // Apply the updated position back to the target object
            targetObject.transform.position = currentPosition;
        }
        else
        {
            Debug.LogWarning("Target object is not assigned.");
        }
    }
}