using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> compositePrefabs;
    public Transform[] spawnPoints;

    private int placedCount;
    private List<DraggableCompositeBlock> currentBlocks = new();

    public GameObject modeManager;

    public float scaleAtSpawn = 0.7f;



    private void Start()
    {
        placedCount = 0;
        SpawnAll();
    }

    public void SpawnAll()
    {
        // 1) If there's not room for a full wave, Game Over
        if (!GridManager.Instance.HasFreeSlots(spawnPoints.Length))
        {
            Debug.Log("Game Over: Not enough space for a full wave!");
            SetGameOver();
            return;
        }

        // 2) Tear down leftovers
        foreach (var old in currentBlocks)
            if (old) Destroy(old.gameObject);
        currentBlocks.Clear();

        bool gridEmpty = !GridManager.Instance.HasPlacedBlocks();

        // 3) Spawn exactly three composites
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            DraggableCompositeBlock compInstance = null;
            Vector3 pos = spawnPoints[i].position;

            // Wave-2+: try to fit + match
            if (i == 0 && !gridEmpty)
            {
                // A) Find a prefab that fits & can clear
                foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
                {
                    var go = Instantiate(prefab, pos, Quaternion.identity);
                    go.transform.localScale = Vector3.one * scaleAtSpawn;
                    var comp = go.GetComponent<DraggableCompositeBlock>();
                    comp.spawnManager = this;
                    comp.startPosition = pos;

                    // must fit
                    if (!GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
                    {
                        Destroy(go);
                        continue;
                    }

                    // reroll numbers up to 20 times until it WILL clear
                    bool success = false;
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        comp.AssignValidRandomNumbers();
                        if (GridManager.Instance.TryFindPlacementThatMatchesSum10(comp))
                        {
                            success = true;
                            break;
                        }
                    }

                    if (success)
                    {
                        compInstance = comp;
                        break;
                    }
                    Destroy(go);
                }

                // B) Fallback: if none both fit+match, pick any *fitting* prefab
                if (compInstance == null)
                {
                    Debug.Log("Fallback: No matchable composite—spawning any that fits.");
                    foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
                    {
                        var go = Instantiate(prefab, pos, Quaternion.identity);
                        go.transform.localScale = Vector3.one * scaleAtSpawn;
                        var comp = go.GetComponent<DraggableCompositeBlock>();
                        comp.spawnManager = this;
                        comp.startPosition = pos;

                        if (GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
                        {
                            compInstance = comp;
                            comp.AssignValidRandomNumbers();
                            break;
                        }
                        Destroy(go);
                    }

                    // C) If STILL nothing fits, that truly is Game Over
                    if (compInstance == null)
                    {
                        Debug.Log("Game Over: No composite can fit in the remaining space!");
                        SetGameOver();
                        return;
                    }
                }
            }
            else
            {
                // First wave or slots 2+3: pure random
                var prefab = compositePrefabs[Random.Range(0, compositePrefabs.Count)];
                var go = Instantiate(prefab, pos, Quaternion.identity);
                go.transform.localScale = Vector3.one * scaleAtSpawn;

                var comp = go.GetComponent<DraggableCompositeBlock>();
                comp.spawnManager = this;
                comp.startPosition = pos;

                comp.AssignValidRandomNumbers();

                compInstance = comp;
            }

            currentBlocks.Add(compInstance);
        }

        // 4) Reset for this wave
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

    public void NotifyBlockPlaced()
    {
        placedCount++;
        int remaining = spawnPoints.Length - placedCount;

        if (remaining > 0)
        {
            // Of the *un-placed* composites, is there at least one that fits?
            bool anyFit = currentBlocks
                .Where(c => c != null && !c.placed)
                .Any(c => GridManager.Instance.CanPlaceCompositeBlockAnywhere(c));

            if (!anyFit)
            {
                Debug.Log("Game Over: No more possible moves!");
                SetGameOver();
                // TODO: your Game Over UI here
                return;
            }
            // otherwise, just wait for the next placement
        }
        else
        {
            // wave done → spawn three fresh composites
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
            SetGameOver();
        }
    }

    private void SetGameOver()
    {
        modeManager.GetComponent<ModeManager>().GameOver();
    }
}