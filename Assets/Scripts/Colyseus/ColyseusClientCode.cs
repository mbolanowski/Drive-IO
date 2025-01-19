using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;
using System.Linq;
using System;

public class ColyseusClientCode : MonoBehaviour
{
    private static ColyseusClient _client = null;
    private static MenuManager _menuManager = null;
    private static ColyseusRoom<MyRoomState> _room = null;

    public Vector2 playerPosition;
    public GameObject playerPrefab;
    public List<GameObject> carObjects = new List<GameObject>(); // List of car GameObjects to track

    public PlayerManager pm;
    public VehicleControllerWithGears vc;
    public VechicleManager vm;

    // Dictionaries to store instances and interpolation data
    private Dictionary<string, GameObject> playerInstances = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> carInstances = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, float> previousRotations = new Dictionary<string, float>();
    private Dictionary<string, InterpolationData> interpolationData = new Dictionary<string, InterpolationData>();

    // Networking settings
    private float lerpDuration = 0.1f;
    private float networkTickRate = 0.05f; // 20 updates per second
    private float nextNetworkTick = 0f;
    private const float PREDICTION_THRESHOLD = 0.9f; // Maximum prediction time in seconds

    private bool isConnected = false;
    private string myPlayerId;

    private async void Start()
    {
        Initialize();
        await JoinOrCreateGame();
        InitializeCarIDs();
        isConnected = true;
    }

    public void Initialize()
    {
        if (_menuManager == null)
        {
            _menuManager = gameObject.AddComponent<MenuManager>();
        }
        _client = new ColyseusClient(_menuManager.HostAddress);
    }

    public async Task JoinOrCreateGame()
    {
        try
        {
            if (_menuManager == null || string.IsNullOrEmpty(_menuManager.HostAddress) || string.IsNullOrEmpty(_menuManager.GameName))
            {
                Debug.LogError("MenuManager not properly initialized!");
                return;
            }

            _room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);

            if (_room == null)
            {
                Debug.LogError("Failed to create or join room!");
                return;
            }
            _room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);

            int currentPlayers = _room.State.players.Count; // Assuming your room state tracks players
            myPlayerId = (currentPlayers + 1).ToString();

            // Handle player position updates
            //_room.OnMessage<PlayerPositionMessage>("player_position", UpdatePlayerPosition);

            // Handle car position updates
            _room.OnMessage<CarPositionMessage>("car_position", message =>
            {
                UpdateCarPosition(message);
                Debug.Log(message.carID);
            });

            // Handle player joining
            _room.OnMessage<PlayerJoinMessage>("player_join", message =>
            {
                if (message.id != GameRoom.SessionId)
                {
                    InstantiatePlayer(message.id);
                }
            });


        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining/creating game: {e.Message}");
        }
    }

    private void InstantiatePlayer(string playerId)
    {
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstances[playerId] = playerInstance;

        interpolationData[playerId] = new InterpolationData
        {
            currentObject = playerInstance,
            previousPosition = playerInstance.transform.position,
            previousRotation = playerInstance.transform.rotation
        };
    }

    private void UpdatePlayerPosition(PlayerPositionMessage message)
    {
        if (!playerInstances.TryGetValue(message.id, out GameObject playerInstance))
        {
            if (message.id != GameRoom.SessionId)
            {
                InstantiatePlayer(message.id);
                playerInstance = playerInstances[message.id];
            }
        }

        if (playerInstance != null)
        {
            UpdateObjectPosition(playerInstance, message.id, new Vector3(message.x, 0, message.z), message.rotationY);
        }
    }

    private void UpdateCarPosition(CarPositionMessage message)
    {
        if (string.IsNullOrEmpty(message.carID))
        {
            Debug.LogError("Received CarPositionMessage with a null or empty carID.");
            return;
        }

        // Check if we have a car with this specific ID (e.g., "C0", "C1", etc.)
        if (carInstances.ContainsKey(message.carID))
        {
            GameObject carToUpdate = carInstances[message.carID];
            if (carToUpdate != null)
            {
                UpdateObjectPosition(carToUpdate, message.carID, new Vector3(message.x, 0, message.z), message.rotationY);
                Debug.Log($"Updated car {message.carID} position to: {message.x}, {message.z}");
            }
            else
            {
                Debug.LogWarning($"Car with ID {message.carID} exists in dictionary but GameObject is null");
            }
        }
        else
        {
            Debug.LogWarning($"Received position update for unknown car ID: {message.carID}");
        }
    }

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

    // Send player position to server
    public void PlayerPosition(Vector2 position, float rotationY)
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("position", new { id = myPlayerId, x = position.x, z = position.y, rotationY, rightBlinker = vc.GetIsRightBlinkerOn(), leftBlinker = vc.GetIsLeftBlinkerOn(), isHorizontal = vc._isHorizontal, hasPriority = pm._hasRightOfWay, turning = vm._declaredDirection, speed = vc.currentSpeed });
            //Debug.Log(myPlayerId);
        }
    }

    private void Update()
    {
        if (!isConnected) return;

        // Update player position
        if (_room != null)
        {
            // Send car positions at fixed network tick rate
            if (Time.time >= nextNetworkTick)
            {
                playerPosition = new Vector2(transform.position.x, transform.position.z);
                float rotationY = transform.rotation.eulerAngles.y;
                PlayerPosition(playerPosition, rotationY);
                nextNetworkTick = Time.time + networkTickRate;
            }

            // Interpolate all objects
            InterpolateObjects();
        }
    }

    private void SendCarPositions()
    {
        if (GameRoom != null)
        {
            foreach (var carPair in carInstances)
            {
                string carId = carPair.Key;
                GameObject car = carPair.Value;

                var positionData = new
                {
                    carID = carId,
                    x = car.transform.position.x,
                    z = car.transform.position.z,
                    rotationY = car.transform.rotation.eulerAngles.y
                };

                _ = GameRoom.Send("car_position", positionData);
            }
        }
    }

    private void InterpolateObjects()
    {
        if (!isConnected) return;

        foreach (var kvp in interpolationData.ToList())
        {
            if (kvp.Value == null || kvp.Value.currentObject == null) continue;

            var data = kvp.Value;
            if (data.interpolationTime <= lerpDuration)
            {
                float t = data.interpolationTime / lerpDuration;

                // Use smoothstep for more natural movement
                t = t * t * (3f - 2f * t);

                // Lerp to the target position most of the time, only use prediction for fast movements
                Vector3 targetPos = Vector3.Distance(data.previousPosition, data.targetPosition) > 1f ?
                    data.predictedPosition : data.targetPosition;

                data.currentObject.transform.position = Vector3.Lerp(data.previousPosition, targetPos, t);
                data.currentObject.transform.rotation = Quaternion.Lerp(data.previousRotation, data.targetRotation, t);
                data.interpolationTime += Time.deltaTime;
            }
        }
    }

    private void InitializeCarIDs()
    {
        for (int i = 0; i < carObjects.Count; i++)
        {
            if (carObjects[i] == null) continue;

            string carId = $"C{i}";
            carInstances[carId] = carObjects[i];
            interpolationData[carId] = new InterpolationData
            {
                currentObject = carObjects[i],
                previousPosition = carObjects[i].transform.position,
                previousRotation = carObjects[i].transform.rotation,
                targetPosition = carObjects[i].transform.position,  // Initialize these too
                targetRotation = carObjects[i].transform.rotation,
                predictedPosition = carObjects[i].transform.position
            };
        }
    }

    private void UpdateObjectPosition(GameObject obj, string id, Vector3 targetPosition, float targetRotation)
    {
        if (string.IsNullOrEmpty(id) || obj == null) return;

        InterpolationData data;
        if (!interpolationData.TryGetValue(id, out data))
        {
            data = new InterpolationData
            {
                currentObject = obj,
                previousPosition = obj.transform.position,
                previousRotation = obj.transform.rotation,
                targetPosition = targetPosition,
                targetRotation = Quaternion.Euler(0, targetRotation, 0),
                predictedPosition = targetPosition
            };
            interpolationData[id] = data;
            return;
        }

        // Store the current position as previous
        data.previousPosition = data.currentObject.transform.position;
        data.previousRotation = data.currentObject.transform.rotation;

        // Set the new target
        data.targetPosition = targetPosition;
        data.targetRotation = Quaternion.Euler(0, targetRotation, 0);

        // Calculate velocity based on actual movement, not predictions
        Vector3 velocity = (targetPosition - data.previousPosition) / networkTickRate;

        // Only apply a small amount of prediction to smooth movement
        data.predictedPosition = targetPosition + (velocity * (PREDICTION_THRESHOLD * 0.1f));

        // Reset interpolation time
        data.interpolationTime = 0f;
    }
}

// Class to store interpolation data for smooth movement
public class InterpolationData
{
    public GameObject currentObject;
    public Vector3 previousPosition;
    public Vector3 targetPosition;
    public Vector3 predictedPosition;
    public Quaternion previousRotation;
    public Quaternion targetRotation;
    public float interpolationTime;
}

[System.Serializable]
public class PlayerPositionMessage
{
    public string id;
    public float x;
    public float z;
    public float rotationY;
    public bool rightBlinker;
    public bool leftBlinker;
    public bool isHorizontal;
    public bool hasPriority;
    public string turning;
    public float speed;
}

[System.Serializable]
public class CarPositionMessage
{
    public string carID;
    public float x;
    public float z;
    public float rotationY;
}

[System.Serializable]
public class PlayerJoinMessage
{
    public string id;
}