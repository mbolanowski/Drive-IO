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

    private void Update()
    {
        gearText.text = (vc.GetCurrentGear() + 1).ToString();
        speedtext.text = (vc.GetCurrentSpeed() * pm._speedMultiplier).ToString("F1");
    }
}
