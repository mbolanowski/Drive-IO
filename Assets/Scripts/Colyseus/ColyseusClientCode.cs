using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;
using Unity.VisualScripting;
using TMPro;
using static TileOwnershipMessage;

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
    public WarningSystemController wsc;
    public TrafficLightManager tm;
    public Minimap mm;
    public SpawnManager sm;

    public TextMeshPro nameText;
    public TextMeshPro leaderboardText;
    public TextMeshPro leaderboardPoints;

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
        Application.quitting += OnApplicationQuit;
    }

    public void Initialize()
    {
        if (_menuManager == null)
        {
            _menuManager = gameObject.AddComponent<MenuManager>();
        }
        _client = new ColyseusClient("https://pl-waw-3e5aa817.colyseus.cloud");
        //_client = new ColyseusClient(_menuManager.HostAddress);
    }

    public async Task JoinOrCreateGame()
    {
        try
        {
            if (_menuManager == null || string.IsNullOrEmpty(_menuManager.HostAddress) || string.IsNullOrEmpty(_menuManager.GameName))
            {
                //Debug.LogError("MenuManager not properly initialized!");
                return;
            }

            _room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);

            if (_room == null)
            {
                //Debug.LogError("Failed to create or join room!");
                return;
            }

            //sm.ChooseInitialSpawnPoint();

            _room.OnMessage<PlayerJoinMessage>("player_leave", message =>
            {
                RemovePlayer(message.id);
            });


            //_room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);

            // Handle player position updates
            _room.OnMessage<PlayerPositionMessage>("player_position", UpdatePlayerPosition);

            // Handle car position updates
            _room.OnMessage<CarPositionMessage>("car_position", message =>
            {
                UpdateCarPosition(message);
                //Debug.Log(message.carID);
            });

            // Handle player joining
            _room.OnMessage<PlayerJoinMessage>("player_join", message =>
            {
            });

            _room.OnMessage<PlayerJoinMessage>("player_id", message =>
            {
                myPlayerId = message.id;
                getSpawn();
                //Debug.Log($"Assigned Player ID: {myPlayerId}");
            });

            _room.OnMessage<PlayerJoinMessage>("intersection", message =>
            {
                if (message.id == myPlayerId)
                {
                    pm.AddIncident();
                    pm.AddIncident();
                    wsc.SetInfoText("You had no right of way");
                    wsc.SetPenaltyText("-2 Life");
                    //Debug.Log("Wykroczenie");
                }
            });

            _room.OnMessage<LightsMessage>("light", message =>
            {
                tm.CommunicateWithServer(message.id, message.prev);

            });

            _room.OnMessage<SpawningMessage>("spawning", message =>
            {
                if (message.playerID == myPlayerId)
                {
                    sm.availableSpawnSpots = message.id;
                    sm.ChooseInitialSpawnPoint();
                }
            });

            _room.OnMessage<PlayerJoinMessage>("death", message =>
            {

                if(mm.isActiveAndEnabled) mm.RefreshMinimap();

            });

            _room.OnMessage<TileMessage>("tileTaken", message =>
            {
                if (message.id != myPlayerId)
                {
                    //pm.RemoveTile(message.xx, message.yy);
                }

            });

            _room.OnMessage<TileOwnershipMessage>("current_tiles", (message) =>
            {
                pm.ownerships = message.ownerships;
                // Clear OtherHeldTiles to start fresh with server state
                pm.OtherHeldTiles.Clear();

                foreach (var playerOwnership in message.ownerships)
                {
                    string playerId = playerOwnership.Key;
                    TilePosition[] tiles = playerOwnership.Value;

                    foreach (var tile in tiles)
                    {
                        if (playerId == myPlayerId)
                        {
                            // Add to heldTiles if we don't already have it
                            if (!pm.heldTiles.Contains((tile.x, tile.y)))
                            {
                                pm.AssignTile(tile.x, tile.y);
                            }
                        }
                        else
                        {
                            // Add to OtherHeldTiles and mark as taken
                            pm.OtherHeldTiles.Add((tile.x, tile.y));
                            pm.RemoveTile(tile.x, tile.y);  // This sets the visual state
                        }
                    }
                }
            });

        }
        catch (Exception e)
        {
            //Debug.LogError($"Error joining/creating game: {e.Message}");
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
            if (message.id != myPlayerId)
            {
                InstantiatePlayer(message.id);
                playerInstance = playerInstances[message.id];
            }
        }

        if ((playerInstance != null) && (message.id != myPlayerId))
        {
            CarsController cnt = playerInstance.GetComponent<CarsController>();
            cnt.hasPriority = message.hasPriority;
            cnt.isHorizontal = message.isHorizontal;
            cnt.rightBlinker = message.rightBlinker;
            cnt.leftBlinker = message.leftBlinker;
            cnt.speed = message.speed;
            cnt.intersectionEntranceDirection = message.entrance;
            cnt.turning = message.turning;

            UpdateObjectPosition(playerInstance, message.id, new Vector3(message.x, 0, message.z), message.rotationY);
            FloatingTextNPC ftn = playerInstance.GetComponentInChildren<FloatingTextNPC>();

            playerInstance.GetComponentInChildren<FloatingTextNPC>().displayText = message.name;

            if(message.name != "input nickname here...") leaderboardText.text = message.name;
            else leaderboardText.text = "default (" + message.id + ")";

            leaderboardPoints.text = pm.GetTileCountByPlayerID(message.id).ToString();
        }
    }

    private void UpdateCarPosition(CarPositionMessage message)
    {
        if (string.IsNullOrEmpty(message.carID))
        {
            //Debug.LogError("Received CarPositionMessage with a null or empty carID.");
            return;
        }

        if (_room != null)
        {
            // Check if we have a car with this specific ID (e.g., "C0", "C1", etc.)
            if (carInstances.ContainsKey(message.carID))
            {
                GameObject carToUpdate = carInstances[message.carID];
                if (carToUpdate != null)
                {


                    UpdateObjectPosition(carToUpdate, message.carID, new Vector3(message.x, 0, message.z), message.rotationY);
                    carToUpdate.GetComponent<AISimpleController>().rightBlinker = message.rightBlinker;
                    carToUpdate.GetComponent<AISimpleController>().leftBlinker = message.leftBlinker;

                    //Debug.Log($"Updated car {message.carID} position to: {message.x}, {message.z}");
                }
                else
                {
                    //Debug.LogWarning($"Car with ID {message.carID} exists in dictionary but GameObject is null");
                }
            }
            else
            {
                // Debug.LogWarning($"Received position update for unknown car ID: {message.carID}");
            }
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
                //Debug.LogError("Room hasn't been initialized yet!");
            }
            return _room;
        }
    }

    // Send player position to server
    public void PlayerPosition(Vector2 position, float rotationY)
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("position", new { id = myPlayerId, x = position.x, z = position.y, rotationY, rightBlinker = vc.GetIsRightBlinkerOn(), leftBlinker = vc.GetIsLeftBlinkerOn(), isHorizontal = vc._isHorizontal, hasPriority = pm._hasRightOfWay, turning = vm._declaredDirection, speed = vc.currentSpeed, entrance = vc.intersectionEntranceDirection, name = nameText.text });
            //Debug.Log(myPlayerId);
        }
    }

    private void Update()
    {
        if (!isConnected) return;

        // Update player position
        if (_room != null)
        {
            //Debug.Log(myPlayerId);
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

            // Calculate the distance between previous and current positions
            float distance = Vector3.Distance(data.previousPosition, data.targetPosition);

            // Skip interpolation if the distance is too large (e.g., greater than 10 units)
            if (distance > 5f)
            {
                // Directly set the object's position and rotation without interpolation
                data.currentObject.transform.position = data.targetPosition;
                data.currentObject.transform.rotation = data.targetRotation;

                // Reset interpolation data to prevent further interpolation for this update
                data.previousPosition = data.targetPosition;
                data.previousRotation = data.targetRotation;
                data.interpolationTime = 0f;
                continue;
            }

            // Proceed with normal interpolation if movement is not too large
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

    public void notifyTookTile(int x, int y)
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("tileTaken", new {id = myPlayerId, xx = x, yy = y });
        }
    }

    public void notifyViolation()
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("position", new { id = myPlayerId});
        }
    }

    public void notifyDeath()
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("death", new { id = myPlayerId });
        }
    }

    public void setSpawn(int spawns)
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("spawn", new { id = myPlayerId, spawn = spawns });
        }
    }

    public void getSpawn()
    {
        if (GameRoom != null)
        {
            _ = GameRoom.Send("spawning", new { playerID = myPlayerId });
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterPlayModeStateChanged()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            //Debug.Log("Exiting Play Mode: Leaving Colyseus room...");

            // Example: Get the room instance and call Leave (you'll need to adjust based on your setup)
            var handler = FindObjectOfType<ColyseusClientCode>();
            if (handler != null && handler.GameRoom != null)
            {
                _ = handler.GameRoom.Send("player_leave", new { id = handler.myPlayerId });
                handler.GameRoom.Leave();
                //Debug.Log("Colyseus room left successfully.");
            }
        }
    }
#endif

    private void OnApplicationQuit()
    {
        //Debug.Log("Application is quitting: Leaving Colyseus room...");
        if (GameRoom != null)
        {
            // Notify the server before leaving
            _ = GameRoom.Send("player_leave", new { id = myPlayerId });

            GameRoom.Leave();
            //Debug.Log("Colyseus room left successfully on quit.");
        }
    }

    private void OnDestroy()
    {
        if (GameRoom != null)
        {
            // Notify the server before leaving
            _ = GameRoom.Send("player_leave", new { id = myPlayerId });

            GameRoom.Leave();
            //Debug.Log("Colyseus room left on destroy.");
        }
    }

    private void RemovePlayer(string playerId)
    {
        if (playerInstances.TryGetValue(playerId, out GameObject playerObject))
        {
            Destroy(playerObject); // Remove the player's GameObject from the scene
            playerInstances.Remove(playerId); // Remove the reference from the dictionary
            interpolationData.Remove(playerId); // Clean up interpolation data if it exists

            //Debug.Log($"Player {playerId} has been removed from the game.");
        }
        else
        {
            //Debug.LogWarning($"Player {playerId} not found in playerInstances.");
        }
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
    public int entrance;
    public string name;
}

[System.Serializable]
public class CarPositionMessage
{
    public string carID;
    public float x;
    public float z;
    public float rotationY;
    public bool rightBlinker;
    public bool leftBlinker;
}

[System.Serializable]
public class PlayerJoinMessage
{
    public string id;
}

[System.Serializable]
public class LightsMessage
{
    public int id;
    public int prev;
}

[System.Serializable]
public class TileMessage
{
    public string id;
    public int xx;
    public int yy;
}

[System.Serializable]
public class SpawningMessage
{
    public List<int> id;
    public string playerID;
}

[System.Serializable]
public class TileOwnershipMessage
{
    [System.Serializable]
    public class TilePosition
    {
        public int x;
        public int y;
    }

    public Dictionary<string, TilePosition[]> ownerships;
}
