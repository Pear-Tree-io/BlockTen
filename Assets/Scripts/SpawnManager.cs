using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections;
using Transform = UnityEngine.Transform;
using ManagersSpace;

public class SpawnManager : MonoBehaviour
{
	[Header("Spawn Settings")]
	public List<GameObject> compositePrefabs;
	public Transform[] spawnPoints;

	private int placedCount;
	private List<DraggableCompositeBlock> currentBlocks = new();

	public ModeManager modeManager;

	public float scaleAtSpawn = 0.7f;

	public GameObject revivePanel;
    public bool isRevive = false;

	public bool isTimeLimit = false;

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
			if (old)
				Destroy(old.gameObject);
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

	public void SpawnAllMatch()
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
			if (old)
				Destroy(old.gameObject);
		currentBlocks.Clear();

		bool gridEmpty = !GridManager.Instance.HasPlacedBlocks();

		// 3) Spawn exactly three composites with guaranteed chain‐match
		for (int i = 0; i < spawnPoints.Length; i++)
		{
			DraggableCompositeBlock chosen = null;
			Vector3 pos = spawnPoints[i].position;

			// A) Try to find a fitting + matching block
			foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
			{
				var go = Instantiate(prefab, pos, Quaternion.identity);
				go.transform.localScale = Vector3.one * scaleAtSpawn;
				var comp = go.GetComponent<DraggableCompositeBlock>();
				comp.spawnManager = this;
				comp.startPosition = pos;

				// must fit somewhere
				if (!GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
				{
					Destroy(go);
					continue;
				}

				// roll numbers up to 20 times
				bool ok = false;
				for (int attempt = 0; attempt < 20; attempt++)
				{
					comp.AssignValidRandomNumbers();

					if (gridEmpty && i == 0)
					{
						// first wave: use your original clear‐match check
						if (GridManager.Instance.TryFindPlacementThatMatchesSum10(comp))
						{
							ok = true;
							break;
						}
					}
					else if (i > 0)
					{
						// chain‐match: at least one NumberBlock in this comp
						// sums to 10 with one in the previous slot
						var prev = currentBlocks[i - 1];
						if (prev.children.Any(a =>
							    comp.children.Any(b => a.Value + b.Value == 10)))
						{
							ok = true;
							break;
						}
					}
				}

				if (ok)
				{
					chosen = comp;
					break;
				}

				Destroy(go);
			}

			// B) Fallback: spawn any fitting block
			if (chosen == null)
			{
				Debug.Log("Fallback: No clearable/chainable composite—spawning any that fits.");
				foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
				{
					var go = Instantiate(prefab, pos, Quaternion.identity);
					go.transform.localScale = Vector3.one * scaleAtSpawn;
					var comp = go.GetComponent<DraggableCompositeBlock>();
					comp.spawnManager = this;
					comp.startPosition = pos;

					if (GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
					{
						comp.AssignValidRandomNumbers(); // at least randomize
						chosen = comp;
						break;
					}
					Destroy(go);
				}

				// C) If STILL nothing fits, Game Over
				if (chosen == null)
				{
					Debug.Log("Game Over: No composite can fit in the remaining space!");
					SetGameOver();
					return;
				}
			}

			currentBlocks.Add(chosen);
		}

		// 4) Reset for this wave
		placedCount = 0;
	}

    /// <summary>
    /// Spawns a single new composite into the first empty spawn-point slot.
    /// An “empty” slot is one where currentBlocks[i] is null or has already been placed.
    /// </summary>
    public void SpawnNew()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            // empty if we've destroyed it (== null) or it’s already been placed
            if (currentBlocks[i] == null || currentBlocks[i].placed)
            {
                Vector3 pos = spawnPoints[i].position;

                // pick a random prefab
                var prefab = compositePrefabs[Random.Range(0, compositePrefabs.Count)];
                var go = Instantiate(prefab, pos, Quaternion.identity);
                go.transform.localScale = Vector3.one * scaleAtSpawn;

                // hook up the block
                var comp = go.GetComponent<DraggableCompositeBlock>();
                comp.spawnManager = this;
                comp.startPosition = pos;
                comp.AssignValidRandomNumbers();

                // replace the empty slot
                currentBlocks[i] = comp;
                break; // only spawn one
            }
        }
    }

    public void NotifyBlockPlaced()
	{
        if (isTimeLimit)
		{
			SpawnNew();

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
        }
		else
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

	public void SetGameOver()
	{
		if (!isRevive)
		{
			StartCoroutine(AskRevive());
		}
		else
		{
			GridManager.Instance.InitializeEndGrid();
			modeManager.GameOver();
		}
	}

	public IEnumerator AskRevive()
	{
		isRevive = true;

		modeManager.SetNoSpaceLeftMessage(true);

		yield return new WaitForSeconds(1.5f);

		revivePanel.SetActive(true);
	}

	public void ReviveAd() => AdManager.Get.ShowAd(Revive);

    /// <summary>
    /// Call this from your UI Button's OnClick.
    /// Destroys any unplaced composites in the spawn points and refills them.
    /// </summary>
    public void Revive()
    {
        modeManager.SetNoSpaceLeftMessage(false);
        revivePanel.SetActive(false);

        foreach (var comp in currentBlocks)
        {
            if (comp != null)
                Destroy(comp.gameObject);
        }

        currentBlocks.Clear();
        placedCount = 0;
        SpawnAllMatch();
    }

	public void SkipRevive()
	{
		revivePanel.SetActive(false);
		CheckRemainingSpace();
	}

	[Header("Tutorial Hand Settings")]
	[SerializeField]
	private GameObject handPrefab;
	[SerializeField]
	private Transform tutorialStartPoint;
	[SerializeField]
	private float handMoveDuration = 1f;

	/// <summary>
	/// Animates a hand from a start to end position based on grid state.
	/// </summary>
	public void PlayTutorialHandAnimation()
	{
		if (handPrefab == null) return;
		StartCoroutine(PlayTutorialHandRoutine());
	}

	private IEnumerator PlayTutorialHandRoutine()
	{
		var gm = GridManager.Instance;
		Vector3 startPos = Vector3.zero;
		Vector3 endPos = Vector3.zero;

		if (gm.HasPlacedBlocks())
		{
			bool matchFound = false;
			// Find a composite and grid block pair summing to 10
			foreach (var comp in currentBlocks.Where(c => c != null && !c.placed))
			{
				if (!gm.CanPlaceCompositeBlockAnywhere(comp)) continue;

				// look through each child block of the composite
				var childBlocks = comp.GetComponentsInChildren<NumberBlock>();
				foreach (var nb in childBlocks)
				{
					for (int x = 0; x < gm.columns; x++)
					{
						for (int y = 0; y < gm.rows; y++)
						{
							var gridBlock = gm.GetBlockAt(x, y);
							if (gridBlock != null && nb.Value + gridBlock.Value == 10)
							{
								startPos = nb.transform.position;
								endPos = gridBlock.transform.position;
								matchFound = true;
								break;
							}
						}
						if (matchFound) break;
					}
					if (matchFound) break;
				}
				if (matchFound) break;
			}

			if (!matchFound)
			{
				// No valid match: if there are still unplaced spawn blocks, point from first to grid center
				var remaining = currentBlocks.Where(c => c != null && !c.placed).ToList();
				if (remaining.Any())
				{
					var comp = remaining.First();
					startPos = comp.startPosition;
					endPos = gm.transform.position;
				}
				else
				{
					// No spawn blocks left: fallback from tutorial start or center spawn to grid center
					startPos = tutorialStartPoint != null
						? tutorialStartPoint.position
						: spawnPoints[spawnPoints.Length / 2].position;
					endPos = gm.transform.position;
				}
			}
		}
		else
		{
			// Grid empty: point from center spawn to grid center
			startPos = tutorialStartPoint != null
				? tutorialStartPoint.position
				: spawnPoints[spawnPoints.Length / 2].position;
			endPos = gm.transform.position;
		}

		// Animate hand
		var hand = Instantiate(handPrefab, startPos, Quaternion.identity, transform);
		float elapsed = 0f;
		while (elapsed < handMoveDuration)
		{
			elapsed += Time.deltaTime;
			hand.transform.position = Vector3.Lerp(startPos, endPos, elapsed / handMoveDuration);
			yield return null;
		}
		hand.transform.position = endPos;
		Destroy(hand);
	}
}