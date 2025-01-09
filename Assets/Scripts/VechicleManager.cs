using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VechicleManager : MonoBehaviour
{
    public VehicleControllerWithGears vc;
    public PlayerManager pm;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI speedtext;

    public string _declaredDirection = "empty";
    public Vector3 _lastForewardVector = Vector3.zero;

    public bool leftAllowed = false;
    public bool rightAllowed = false;
    public bool straightAllowed = false;
    public bool uAllowed = false;

    public int currentTileX = 0;
    public int currentTileY = 0;

    public Color vehicleColor;

    private void Update()
    {
        gearText.text = "Bieg: " + (vc.GetCurrentGear() + 1).ToString();
        speedtext.text = "Prêdkoœæ: " + (vc.GetCurrentSpeed() * pm._speedMultiplier).ToString("F1");
    }

    public bool GetIsLeftBlinkerOn()
    {
        return vc.GetIsLeftBlinkerOn();
    }

    public bool GetIsRightBlinkerOn()
    {
        return vc.GetIsRightBlinkerOn();
    }

    public bool GetIsAnyBlinkerOn()
    {
        if(GetIsLeftBlinkerOn() || GetIsRightBlinkerOn()) return true;
        else return false;
    }

    public float GetAcceleration()
    {
        return vc.GetAcceleration();
    }

    public float GetMaxSpeed()
    {
        return vc.maxSpeed;
    }

    public void SetCurrentTile(int x, int y)
    {
        currentTileX = x;
        currentTileY = y;
    }

    public int GetCurrentTileX()
    {
        return currentTileX;
    }

    public int GetCurrentTileY()
    {
        return currentTileY;
    }
}
