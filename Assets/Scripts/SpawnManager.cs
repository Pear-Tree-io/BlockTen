using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> compositePrefabs;
    public Transform[] spawnPoints;

    private int placedCount;
    private List<DraggableCompositeBlock> currentBlocks = new List<DraggableCompositeBlock>();

    private void Start()
    {
        placedCount = 0;
        SpawnAll();
    }

    private void SpawnAll()
    {
        // game‐over check (if you want to stop spawning when grid is full)
        if (!GridManager.Instance.HasFreeSlots(spawnPoints.Length))
        {
            Debug.Log("Game Over: Not enough space for a full wave!");
            return;
        }

        // clear any old references (should be none on first call)
        currentBlocks.Clear();

        // spawn & track
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var go = Instantiate(
                compositePrefabs[Random.Range(0, compositePrefabs.Count)],
                spawnPoints[i].position,
                Quaternion.identity
            );
            go.transform.localScale = Vector3.one * 0.8f;

            var comp = go.GetComponent<DraggableCompositeBlock>();
            comp.spawnManager = this;
            comp.startPosition = spawnPoints[i].position;

            currentBlocks.Add(comp);
        }

        placedCount = 0;
    }

    /// <summary>
    /// Call this from your UI Button's OnClick.
    /// Destroys any unplaced composites in the spawn points and refills them.
    /// </summary>
    public void RerollSpawnBlocks()
    {
        foreach (var comp in currentBlocks)
        {
            if (comp != null)
                Destroy(comp.gameObject);
        }
        currentBlocks.Clear();
        placedCount = 0;
        SpawnAll();
    }

    /// <summary>
    /// Called by each DraggableCompositeBlock when it successfully lands.
    /// </summary>
    public void NotifyBlockPlaced()
    {
        placedCount++;

        // check if the remaining spawn‐point blocks can still fit
        int remaining = spawnPoints.Length - placedCount;
        if (remaining > 0 && !GridManager.Instance.HasFreeSlots(remaining))
        {
            Debug.Log($"Game Over: No space for the remaining {remaining} block(s).");
            return;
        }

        // once all in this wave are placed, spawn the next wave
        if (placedCount >= spawnPoints.Length)
            SpawnAll();
    }
}
