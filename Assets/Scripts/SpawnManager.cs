// SpawnManager.cs
using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> compositePrefabs;
    public Transform[] spawnPoints;

    private int placedCount;

    private void Start()
    {
        placedCount = 0;
        SpawnAll();
    }

    private void SpawnAll()
    {
        // Check if there's room for all pending blocks
        if (!GridManager.Instance.HasFreeSlots(spawnPoints.Length))
        {
            Debug.Log("Game Over: Not enough space for a full wave!");
            // TODO: Game‐Over UI/logic here
            return;
        }

        // Spawn the next batch and reset counter
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var go = Instantiate(
                compositePrefabs[Random.Range(0, compositePrefabs.Count)],
                spawnPoints[i].position,
                Quaternion.identity
            );
            go.transform.localScale = Vector3.one * 0.6f;

            var comp = go.GetComponent<DraggableCompositeBlock>();
            comp.spawnManager = this;
            comp.startPosition = spawnPoints[i].position;
        }
        placedCount = 0;
    }

    public void CheckRemainingSpace()
    {
        int remaining = spawnPoints.Length - placedCount;
        if (remaining > 0 && !GridManager.Instance.HasFreeSlots(remaining))
        {
            Debug.Log($"Game Over: No space for the remaining {remaining} block(s).");
            // TODO: fire your Game Over UI/scene logic here
        }
    }

    public void NotifyBlockPlaced()
    {
        placedCount++;
        CheckRemainingSpace();    // <-- also check *after* every successful placement

        if (placedCount >= spawnPoints.Length)
            SpawnAll();
    }
}
