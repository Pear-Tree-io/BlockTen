using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ManagersSpace;
using Random = UnityEngine.Random;
using Transform = UnityEngine.Transform;

public class SpawnManager : MonoBehaviour
{
	[Header("Spawn Settings")]
	public List<GameObject> compositePrefabs;
	private Queue<SerializableNumberBlock> _upcomingBlocks;
	public Transform[] spawnPoints;
	[SerializeField]
	private Transform traSpawnTarget;
	[HideInInspector]
	public bool isInfinityMode = true;

	private int placedCount;
	private List<DraggableCompositeBlock> _currentBlocks = new();

	private ModeManager _modeManager;

	public float scaleAtSpawn = 0.7f;

	public GameObject revivePanel;

	public bool isTimeLimit = false;

	public void Init(ModeManager modeManager)
	{
		_modeManager = modeManager;
		InitSpawn();
	}

	private void InitSpawn()
	{
		SpawnFull();
	}

	private bool CheckGameOver()
	{
		if (_upcomingBlocks is { Count: 0 })
		{
			Debug.Log("End of game: No more uncompleted blocks!");
			SetGameOver();
			return false;
		}

		if (_modeManager.CheckGameOver(spawnPoints.Length))
		{
			Debug.Log("Game Over: No more possible moves!");
			SetGameOver();
			return true;
		}

		return false;
	}

	private void SpawnFull()
	{
		if (CheckGameOver())
			return;

		ClearBlocks();

		var gridEmpty = !GridManager.Instance.HasPlacedBlocks();

		for (var i = 0; i < spawnPoints.Length; i++)
		{
			if (_upcomingBlocks is { Count: 0 })
			{
				Debug.Log("No more upcoming blocks to spawn!");
				return;
			}

			DraggableCompositeBlock compInstance = null;
			var pos = spawnPoints[i].position;

			// Wave-2+: try to fit + match
			if (i == 0 && !gridEmpty)
			{
				// A) Find a prefab that fits & can clear
				foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
				{
					if (_upcomingBlocks is { Count: <= 0 })
						return;

					var comp = SpawnBlock(prefab, pos);

					if (comp.valueInitialized)
					{
						compInstance = comp;
						break;
					}
					
					// must fit
					if (!GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
					{
						Destroy(comp.gameObject);
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

					Destroy(comp.gameObject);
				}

				// B) Fallback: if none both fit+match, pick any *fitting* prefab
				if (compInstance == null)
				{
					Debug.Log("Fallback: No matchable composite—spawning any that fits.");
					foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
					{
						SpawnBlock(prefab, pos);
						var go = Instantiate(prefab, pos, Quaternion.identity);
						go.transform.localScale = Vector3.one * scaleAtSpawn;
						var comp = go.GetComponent<DraggableCompositeBlock>();
						comp.Init(pos);

						if (comp.valueInitialized)
						{
							compInstance = comp;
							break;
						}

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
				compInstance = SpawnBlock(compositePrefabs[Random.Range(0, compositePrefabs.Count)], pos);
			}

			_currentBlocks.Add(compInstance);
		}
	}

	private DraggableCompositeBlock SpawnBlock(GameObject prefab, Vector3 pos)
	{
		SerializableNumberBlock blockData = null;
		if (_upcomingBlocks != null && _upcomingBlocks.TryDequeue(out blockData))
		{
			prefab = compositePrefabs.FirstOrDefault(i => i.name == blockData.blockName) ?? prefab;

			if (isInfinityMode && _upcomingBlocks.Count == 0)
				_upcomingBlocks = null;
		}

		var comp = Instantiate(prefab, pos, Quaternion.identity, traSpawnTarget).GetComponent<DraggableCompositeBlock>();
		comp.transform.localScale = Vector3.one * scaleAtSpawn;
		comp.Init(pos, blockData?.values);

		return comp;
	}

	public void SpawnAllMatch()
	{
		if (CheckGameOver())
			return;

		ClearBlocks();

		var gridEmpty = !GridManager.Instance.HasPlacedBlocks();

		// 3) Spawn exactly three composites with guaranteed chain‐match
		for (var i = 0; i < spawnPoints.Length; i++)
		{
			DraggableCompositeBlock chosen = null;
			var pos = spawnPoints[i].position;

			// A) Try to find a fitting + matching block
			foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
			{
				var comp = SpawnBlock(prefab, pos);

				if (comp.valueInitialized)
				{
					chosen = comp;
					break;
				}

				// must fit somewhere
				if (!GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
				{
					Destroy(comp.gameObject);
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
						var prev = _currentBlocks[i - 1];
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

				Destroy(comp.gameObject);
			}

			// B) Fallback: spawn any fitting block
			if (chosen == null)
			{
				Debug.Log("Fallback: No clearable/chainable composite—spawning any that fits.");
				foreach (var prefab in compositePrefabs.OrderBy(_ => Random.value))
				{
					var comp = SpawnBlock(prefab, pos);

					if (comp.valueInitialized)
					{
						chosen = comp;
						break;
					}
					
					if (GridManager.Instance.CanPlaceCompositeBlockAnywhere(comp))
					{
						comp.AssignValidRandomNumbers(); // at least randomize
						chosen = comp;
						break;
					}

					Destroy(comp.gameObject);
				}

				// C) If STILL nothing fits, Game Over
				if (chosen == null)
				{
					Debug.Log("Game Over: No composite can fit in the remaining space!");
					SetGameOver();
					return;
				}
			}

			_currentBlocks.Add(chosen);
		}
	}

	/// <summary>
	/// Spawns a single new composite into the first empty spawn-point slot.
	/// An “empty” slot is one where currentBlocks[i] is null or has already been placed.
	/// </summary>
	public void SpawnNew()
	{
		for (var i = 0; i < spawnPoints.Length; i++)
		{
			// empty if we've destroyed it (== null) or it’s already been placed
			if (_currentBlocks[i] == null || _currentBlocks[i].placed)
			{
				_currentBlocks[i] = SpawnBlock(compositePrefabs[Random.Range(0, compositePrefabs.Count)], spawnPoints[i].position);
				return;
			}
		}
	}

	public void NotifyBlockPlaced()
	{
		if (isTimeLimit)
		{
			SpawnNew();

			// Of the *un-placed* composites, is there at least one that fits?
			var anyFit = _currentBlocks.Where(c => c != null && !c.placed).Any(c => GridManager.Instance.CanPlaceCompositeBlockAnywhere(c));

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
				bool anyFit = _currentBlocks
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
				SpawnFull();
			}
		}
	}

	public void SetUpcomingBlocks(SerializableNumberBlock[] dataUpcomingBlocks)
	{
		if (dataUpcomingBlocks == null || dataUpcomingBlocks.Length == 0)
			return;

		_upcomingBlocks = new();
		foreach (var serializableNumberBlock in dataUpcomingBlocks)
		{
			_upcomingBlocks.Enqueue(serializableNumberBlock);
		}
	}

	/// <summary>
	/// Called any time a placement *might* have freed or consumed space.
	/// Logs Game Over if *none* of the un‐placed composites can fit anywhere.
	/// </summary>
	public void CheckRemainingSpace()
	{
		bool anyFit = _currentBlocks
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
		if (_modeManager.ConsumeRevivableState())
			StartCoroutine(AskRevive());
		else
			_modeManager.GameOver();
	}

	public void ClearBlocks()
	{
		foreach (Transform child in traSpawnTarget)
		{
			Destroy(child.gameObject);
		}

		_currentBlocks.Clear();
		placedCount = 0;
	}

	#region Revive

	public IEnumerator AskRevive()
	{
		_modeManager.SetNoSpaceLeftMessage(true);
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
		_modeManager.SetNoSpaceLeftMessage(false);
		revivePanel.SetActive(false);

		ClearBlocks();
		SpawnAllMatch();
	}

	public void SkipRevive()
	{
		revivePanel.SetActive(false);
		CheckRemainingSpace();
	}

	#endregion

}