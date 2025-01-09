using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Minimap : MonoBehaviour
{
    public GameObject tilePrefab; // Prefab for the minimap tile
    public int gridSizeX = 5; // Size of the grid (4x4)
    public int gridSizeY = 4;
    public float tileSpacingX = 1.1f; // Spacing between tiles
    public float tileSpacingY = 1.1f; // Spacing between tiles

    private GameObject[,] tiles; // Array to hold the tile GameObjects

    private Dictionary<(int, int), Coroutine> blinkingTiles = new Dictionary<(int, int), Coroutine>();

    private void Start()
    {
        InitializeMinimap();
    }

    // Initialize the minimap grid
    private void InitializeMinimap()
    {
        tiles = new GameObject[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Instantiate tile prefab
                GameObject tile = Instantiate(tilePrefab, transform);

                // Position the tile
                tile.transform.localPosition = new Vector3(x * tileSpacingX, 0, y * tileSpacingY);

                // Store the tile in the array
                tiles[x, y] = tile;
            }
        }
    }

    // Change the color of a specific tile
    public void SetTileColor(int x, int y, Color color)
    {
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            // Stop blinking and reset the tile's color
            StopBlinkingTile(x, y);

            Renderer tileRenderer = tiles[x, y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = color;
            }
            else
            {
                Debug.LogWarning($"Tile at ({x}, {y}) does not have a Renderer component.");
            }

            // Start the grow/shrink animation
            StartCoroutine(AnimateTileSize(x, y));
        }
        else
        {
            Debug.LogWarning($"Tile coordinates ({x}, {y}) are out of bounds.");
        }
    }

    private IEnumerator AnimateTileSize(int x, int y)
    {
        GameObject tile = tiles[x, y];

        // Get the actual original scale of the tile (not Vector3.one)
        Vector3 originalScale = tile.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f; // Grow by 20%
        float animationSpeed = 0.15f; // Duration of the animation

        // Grow the tile
        float elapsedTime = 0f;
        while (elapsedTime < animationSpeed)
        {
            tile.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / animationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        tile.transform.localScale = targetScale;

        // Shrink the tile back to its original size
        elapsedTime = 0f;
        while (elapsedTime < animationSpeed)
        {
            tile.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / animationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        tile.transform.localScale = originalScale;
    }

    // Start blinking a tile
    public void StartBlinkingTile(int x, int y, Color blinkColor, float blinkInterval = 0.5f)
    {
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            // Stop blinking if already blinking
            StopBlinkingTile(x, y);

            // Start the blinking coroutine
            Coroutine blinkCoroutine = StartCoroutine(BlinkTile(x, y, blinkColor, blinkInterval));
            blinkingTiles[(x, y)] = blinkCoroutine;
        }
        else
        {
            Debug.LogWarning($"Tile coordinates ({x}, {y}) are out of bounds.");
        }
    }

    // Stop blinking a tile
    public void StopBlinkingTile(int x, int y)
    {
        if (blinkingTiles.TryGetValue((x, y), out Coroutine blinkCoroutine))
        {
            StopCoroutine(blinkCoroutine);
            blinkingTiles.Remove((x, y));

            // Reset the tile's color to its default (assuming white)
            SetTileColor(x, y, Color.white);
        }
    }

    // Coroutine to handle blinking
    private IEnumerator BlinkTile(int x, int y, Color blinkColor, float blinkInterval)
    {
        Renderer tileRenderer = tiles[x, y].GetComponent<Renderer>();
        if (tileRenderer == null)
        {
            Debug.LogWarning($"Tile at ({x}, {y}) does not have a Renderer component.");
            yield break;
        }

        Color originalColor = tileRenderer.material.color;
        bool isBlinking = true;

        while (true)
        {
            tileRenderer.material.color = isBlinking ? blinkColor : originalColor;
            isBlinking = !isBlinking;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
