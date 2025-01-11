using Cinemachine;
using System.Data.Common;
using TMPro;
using UnityEngine;

public class Button3D : MonoBehaviour
{
    public GameObject ElementsUI;
    public GameObject MainMenu;
    public CinemachineVirtualCamera virtualCamera;
    public GameObject Car;
    public SpawnManager sm;
    public Minimap mm;
    public VechicleManager vm;

    public TextMeshPro leaderboardName;
    public TextMeshPro nameInput;

    public void OnButtonPress()
    {
        Debug.Log("Button Pressed!");
        leaderboardName.text = nameInput.text + " (you)";
        if (nameInput.text == "input nickname here...") leaderboardName.text = "default (you)";
        StartCoroutine(LerpOrthoSize(virtualCamera, virtualCamera.m_Lens.OrthographicSize, 5.41f, 2.0f));
        MainMenu.SetActive(false);
        this.GetComponent<Renderer>().enabled = false;
        sm.RespawnPlayer();
        transform.GetChild(0).gameObject.SetActive(false);
    }

    private void OnMouseDown()
    {
        OnButtonPress();
    }

    private System.Collections.IEnumerator LerpOrthoSize(CinemachineVirtualCamera cam, float startSize, float targetSize, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newSize = Mathf.Lerp(startSize, targetSize, elapsedTime / duration);
            cam.m_Lens.OrthographicSize = newSize;
            yield return null;
        }

        // Ensure the final size is set precisely
        cam.m_Lens.OrthographicSize = targetSize;
        ElementsUI.SetActive(true);
        //mm.StopBlinkingTile(vm.GetCurrentTileX(), vm.GetCurrentTileY());
        //mm.StartBlinkingTile(vm.GetCurrentTileX(), vm.GetCurrentTileY(), vm.vehicleColor, 0.5f);
    }
}
