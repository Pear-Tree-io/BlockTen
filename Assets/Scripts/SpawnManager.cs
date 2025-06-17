using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

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

		if (placedCount >= spawnPoints.Length)
			SpawnAll();

		CheckRemainingSpace();
	}

	public void CheckRemainingSpace()
	{
		foreach (var block in currentBlocks)
		{
			if (block == null || block.placed)
				continue;

			if (GridManager.Instance.CanPlaceCompositeBlockAnywhere(block))
				return;
		}

		foreach (var block in currentBlocks)
		{
			if (block == null || block.placed)
				continue;

			block.gameObject.SetActive(false);
		}

		Debug.Log("Game Over: Not enough space for a full wave!");
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
			CheckRemainingSpace();
	}
}