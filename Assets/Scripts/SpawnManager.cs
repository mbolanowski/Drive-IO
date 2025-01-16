using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] spawnPoints; // Array of spawn point GameObjects
    public GameObject playerObject;  // The player object that will spawn
    public float spawnHeight = 1.0f; // Height offset for spawning (optional)

    private int chosenSpawnIndex = -1; // Store the index of the chosen spawn point

    public VehicleControllerWithGears vc;

    private void Start()
    {
        ChooseInitialSpawnPoint();
        //RespawnPlayer(); // Call RespawnPlayer at the start
    }

    // Choose a random spawn point once at the start
    private void ChooseInitialSpawnPoint()
    {
        if (spawnPoints.Length > 0)
        {
            // Pick a random spawn point from the array
            chosenSpawnIndex = Random.Range(0, spawnPoints.Length);
            Debug.Log("Chosen spawn point at index: " + chosenSpawnIndex);
        }
        else
        {
            Debug.LogError("No spawn points assigned!");
        }
    }

    // Respawn the player at the chosen spawn point
    public void RespawnPlayer()
    {
        if (chosenSpawnIndex >= 0 && chosenSpawnIndex < spawnPoints.Length)
        {
            // Get the chosen spawn point from the array
            GameObject selectedSpawnPoint = spawnPoints[chosenSpawnIndex];

            // Set the player's position to the spawn point's position
            Vector3 spawnPosition = selectedSpawnPoint.transform.position;
            spawnPosition.y += spawnHeight; // Add height offset if needed

            // Set the player's rotation to the spawn point's forward direction
            Quaternion spawnRotation = selectedSpawnPoint.transform.rotation;

            // Set the player's position and rotation
            playerObject.transform.position = spawnPosition;
            playerObject.transform.rotation = spawnRotation;

            vc.currentSpeed = 0.0f;
            vc.currentAcceleration = 0.0f;
            playerObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            playerObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            Debug.Log("Player respawned at: " + selectedSpawnPoint.name);
        }
        else
        {
            Debug.LogError("Chosen spawn index is invalid!");
        }
    }
}
