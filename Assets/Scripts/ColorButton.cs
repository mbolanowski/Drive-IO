using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorButton : MonoBehaviour
{
    public VechicleManager vm;

    Color originalColor;
    Color newColor;

    private void Start()
    {
       originalColor = GetComponent<Renderer>().material.color;
       newColor = GetComponent<Renderer>().material.color *= 0.3f;
    }

    public void OnButtonPress()
    {

        vm.vehicleColor = originalColor;

        GetComponent<Renderer>().material.color = newColor;

    }

    private void Update()
    {
        if(vm.vehicleColor != originalColor)
        {
            GetComponent<Renderer>().material.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        OnButtonPress();
    }
}
