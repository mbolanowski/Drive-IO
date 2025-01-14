using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int _incidents = 0;
    public TextMeshProUGUI incidentCount;
    public TextMeshPro points;

    public TextMeshPro myPoints;

    public GameObject Life1;
    public GameObject Life2;
    public GameObject Life3;

    public int _lives = 3;

    public VechicleManager vm;
    public SpawnManager sm;
    public VehicleControllerWithGears vc;

    public float _speedMultiplier = 12f;

    public string lanePosition;

    public bool _hasRightOfWay = false;

    public bool _dead = false;

    public bool _justSpawned = true;

    // Store held tiles as a set of (x, y) coordinates
    private HashSet<(int, int)> heldTiles = new HashSet<(int, int)>();

    private void Start()
    {
        Life1.GetComponent<Renderer>().material.color = vm.vehicleColor;
        Life2.GetComponent<Renderer>().material.color = vm.vehicleColor;
        Life3.GetComponent<Renderer>().material.color = vm.vehicleColor;
    }
    public void AddIncident()
    {
        _incidents++;
        _lives--;
        if (Life3.activeSelf)
        {
            Life3.SetActive(false);
        }
        else if (Life2.activeSelf)
        {
            Life2.SetActive(false);
        }
        else if(Life1.activeSelf)
        {
            Life1.SetActive(false);
        }

        if(_lives == 0)
        {
            Life3.SetActive(true);
            Life2.SetActive(true);
            Life1.SetActive(true);
            sm.RespawnPlayer();
            _lives = 3;
        }
    }
    
    public void Die()
    {
        _dead = true;
        _lives = 3;
        sm.RespawnPlayer();
        Life3.SetActive(true);
        Life2.SetActive(true);
        Life1.SetActive(true);

        vc.TurnOffBlinker();
    }

    public int GetIncidents()
    {
        return _incidents;
    }

    private void Update()
    {
        //incidentCount.text = "Wykroczenia: " + _incidents.ToString();
        //Debug.Log(GetAssignedTileCount());
    }

    public string GetCurrentLane()
    {
        return lanePosition;
    }

    public void SetCurrentLane(string set)
    {
        lanePosition = set;
    }

    public void SetHasRightOfWay(bool set)
    {
        _hasRightOfWay = set;
    }

    public bool GetHasRightOfWay()
    {
        return _hasRightOfWay;
    }

    // Assign a tile to the player
    public void AssignTile(int x, int y)
    {
        if (x >= 0 && x < 5 && y >= 0 && y < 4) // Ensure the tile is within grid bounds
        {
            heldTiles.Add((x, y));
            myPoints.text = GetAssignedTileCount().ToString();
        }
        else
        {
            Debug.LogWarning($"Tile coordinates ({x}, {y}) are out of bounds.");
        }
    }

    // Check if the tile is owned by this player
    public bool GetTileOwner(int x, int y)
    {
        return heldTiles.Contains((x, y));
    }

    // Get the total count of assigned tiles
    public int GetAssignedTileCount()
    {
        return heldTiles.Count;
    }

    // Optionally, you can clear all held tiles
    public void ClearHeldTiles()
    {
        heldTiles.Clear();
    }
}
