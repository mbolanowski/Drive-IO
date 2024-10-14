using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;

public class ColyseusClientCode : MonoBehaviour
{
    private static ColyseusClient _client = null;
    private static MenuManager _menuManager = null;
    private static ColyseusRoom<MyRoomState> _room = null;

    public Vector2 playerPosition;
    public GameObject playerPrefab; // Reference to the player prefab

    private Dictionary<string, GameObject> playerInstances = new Dictionary<string, GameObject>(); // Store player instances by ID
    private Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>(); // Store previous positions
    private Dictionary<string, float> previousRotations = new Dictionary<string, float>(); // Store previous rotations
    private float lerpDuration = 0.1f; // Duration for interpolation

    private async void Start()
    {
        // Automatically join or create a game when the game starts
        await JoinOrCreateGame();
    }

    // Initialize the Colyseus Client and MenuManager
    public void Initialize()
    {
        if (_menuManager == null)
        {
            _menuManager = gameObject.AddComponent<MenuManager>();
        }

        _client = new ColyseusClient(_menuManager.HostAddress);
    }

    // Method to join or create a room
    public async Task JoinOrCreateGame()
    {
        // Create or join a room on the server
        _room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);

        // Register message handlers to update player positions
        _room.OnMessage<PlayerPositionMessage>("player_position", message =>
        {
            // Update the position of the player identified by message.id
            UpdatePlayerPosition(message);
        });

        // Register to handle player joining the room
        _room.OnMessage<PlayerJoinMessage>("player_join", message =>
        {
            // Instantiate a player prefab only if the joined player is not the local player
            if (message.id != GameRoom.SessionId) // Check if the joining player is not the local player
            {
                InstantiatePlayer(message.id);
            }
        });
    }

    // Method to instantiate the player prefab
    private void InstantiatePlayer(string playerId)
    {
        // Instantiate the player prefab at the default position (0, 0, 0)
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstances[playerId] = playerInstance; // Store the instance by player ID

        // Initialize previous position and rotation
        previousPositions[playerId] = Vector3.zero; // Default starting position
        previousRotations[playerId] = 0f; // Default starting rotation
    }

    private void UpdatePlayerPosition(PlayerPositionMessage message)
    {
        // Check if the player already exists in the playerInstances dictionary
        if (playerInstances.TryGetValue(message.id, out GameObject playerInstance))
        {
            // Get the target position from the message
            Vector3 targetPosition = new Vector3(message.x, 0, message.z);
            float targetRotationY = message.rotationY;

            // Lerp from the current position of the player instance to the target position
            playerInstance.transform.position = Vector3.Lerp(playerInstance.transform.position, targetPosition, Time.deltaTime / lerpDuration);

            // Lerp from the current rotation to the target rotation
            playerInstance.transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(playerInstance.transform.rotation.eulerAngles.y, targetRotationY, Time.deltaTime / lerpDuration), 0);
        }
        else
        {
            // If the player doesn't exist, instantiate a new player prefab
            if (message.id != GameRoom.SessionId) // Check if the joining player is not the local player
            {
                InstantiatePlayer(message.id);
                // Retrieve the instantiated player instance to update its position and rotation
                playerInstance = playerInstances[message.id]; // Retrieve the newly instantiated player instance
            }
        }

        // If playerInstance is not null, update its position and rotation
        if (playerInstance != null)
        {
            Vector3 targetPosition = new Vector3(message.x, 0, message.z);
            float targetRotationY = message.rotationY;

            // Lerp from the current position of the player instance to the target position
            playerInstance.transform.position = Vector3.Lerp(playerInstance.transform.position, targetPosition, Time.deltaTime / lerpDuration);

            // Lerp from the current rotation to the target rotation
            playerInstance.transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(playerInstance.transform.rotation.eulerAngles.y, targetRotationY, Time.deltaTime / lerpDuration), 0);
        }
    }

    // Get or initialize the Colyseus Client
    public ColyseusClient Client
    {
        get
        {
            if (_client == null || !_client.Settings.WebRequestEndpoint.Contains(_menuManager.HostAddress))
            {
                Initialize();
            }
            return _client;
        }
    }

    // Get the current game room
    public ColyseusRoom<MyRoomState> GameRoom
    {
        get
        {
            if (_room == null)
            {
                Debug.LogError("Room hasn't been initialized yet!");
            }
            return _room;
        }
    }

    // Method to send player position and rotation to the server
    public void PlayerPosition(Vector2 position, float rotationY)
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("position", new { id = GameRoom.SessionId, x = position.x, z = position.y, rotationY });
        }
    }

    // Update loop to check for inputs and send position data
    private void Update()
    {
        // Send player position to the server every tick
        if (_room != null)
        {
            playerPosition = new Vector2(transform.position.x, transform.position.z);
            float rotationY = transform.rotation.eulerAngles.y; // Get the Y-axis rotation
            PlayerPosition(playerPosition, rotationY);
        }
    }
}

// A simple class to represent player position message structure
[System.Serializable]
public class PlayerPositionMessage
{
    public string id; // Unique ID of the player
    public float x;
    public float z;
    public float rotationY; // Y-axis rotation of the player
}

// Class to represent player join message
[System.Serializable]
public class PlayerJoinMessage
{
    public string id; // Unique ID of the newly joined player
}
