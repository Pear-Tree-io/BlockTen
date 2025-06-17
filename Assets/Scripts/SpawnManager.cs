using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> compositePrefabs;
    public Transform[] spawnPoints;

    private int placedCount;
    private List<DraggableCompositeBlock> currentBlocks = new();

    private void Start()
    {
        placedCount = 0;
        SpawnAll();
    }

    public void SpawnAll()
    {
        // 1) If there's not room for a full wave, game over
        if (!GridManager.Instance.HasFreeSlots(spawnPoints.Length))
        {
            Debug.Log("Game Over: Not enough space for a full wave!");
            return;
        }

        // 2) Clear any unplaced leftovers
        currentBlocks.ForEach(c => { if (c) Destroy(c.gameObject); });
        currentBlocks.Clear();

        bool gridEmpty = !GridManager.Instance.HasPlacedBlocks();
        DraggableCompositeBlock guaranteed = null;

        if (gridEmpty)
        {
            // First wave: only require that it fits somewhere
            do
            {
                currentBlocks.ForEach(c => { if (c) Destroy(c.gameObject); });
                currentBlocks.Clear();

                var prefab = compositePrefabs[Random.Range(0, compositePrefabs.Count)];
                var go = Instantiate(prefab, spawnPoints[0].position, Quaternion.identity);
                go.transform.localScale = Vector3.one * 0.8f;

                var comp = go.GetComponent<DraggableCompositeBlock>();
                comp.spawnManager = this;
                comp.startPosition = spawnPoints[0].position;
                currentBlocks.Add(comp);
                guaranteed = comp;

                foreach (var nb in comp.GetComponentsInChildren<NumberBlock>())
                    nb.AssignRandom();

            } while (!GridManager.Instance.CanPlaceCompositeBlockAnywhere(guaranteed));
        }
        else
        {
            // Subsequent waves: require both fit + at least one sum-10 match
            do
            {
                currentBlocks.ForEach(c => { if (c) Destroy(c.gameObject); });
                currentBlocks.Clear();

                var prefab = compositePrefabs[Random.Range(0, compositePrefabs.Count)];
                var go = Instantiate(prefab, spawnPoints[0].position, Quaternion.identity);
                go.transform.localScale = Vector3.one * 0.8f;

                var comp = go.GetComponent<DraggableCompositeBlock>();
                comp.spawnManager = this;
                comp.startPosition = spawnPoints[0].position;
                currentBlocks.Add(comp);
                guaranteed = comp;

                foreach (var nb in comp.GetComponentsInChildren<NumberBlock>())
                    nb.AssignRandom();

            } while (!GridManager.Instance.TryFindPlacementThatMatchesSum10(guaranteed));
        }

        // 3) Spawn the rest of the wave normally
        for (int i = 1; i < spawnPoints.Length; i++)
        {
            var prefab = compositePrefabs[Random.Range(0, compositePrefabs.Count)];
            var go = Instantiate(prefab, spawnPoints[i].position, Quaternion.identity);
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
        CheckRemainingSpace();
    }

    /// <summary>
    /// Called by each DraggableCompositeBlock when it successfully lands.
    /// </summary>
    public void NotifyBlockPlaced()
    {
        placedCount++;

        int remaining = spawnPoints.Length - placedCount;
        if (remaining > 0)
        {
            // Of the blocks still un-placed, is there at least one that could fit?
            bool anyFit = currentBlocks
                .Where(c => c != null && !c.placed)
                .Any(c => GridManager.Instance.CanPlaceCompositeBlockAnywhere(c));

            if (!anyFit)
            {
                Debug.Log("Game Over: No more possible moves!");
                // TODO: hook in your Game Over UI/scene logic here
                return;
            }
        }
        else
        {
            // All in this wave placed → spawn next wave
            SpawnAll();
        }
    }


    /// <summary>
    /// Called any time a placement *might* have freed or consumed space.
    /// Logs Game Over if *none* of the un‐placed composites can fit anywhere.
    /// </summary>
    public void CheckRemainingSpace()
    {
        bool anyFit = currentBlocks
            .Where(c => c != null && !c.placed)
            .Any(c => GridManager.Instance.CanPlaceCompositeBlockAnywhere(c));

        if (!anyFit)
        {
            Debug.Log("Game Over: No more possible moves!");
            // TODO: Hook in your Game Over UI / scene here
        }
    }
}