using System;
using UnityEngine;

public class StageBlockSetter : MonoBehaviour
{
	[SerializeField]
	public int value;

#if UNITY_EDITOR
	public NumberBlock blockPrefab;
	
	private void OnValidate()
	{
		blockPrefab ??= GetComponent<NumberBlock>();
		blockPrefab.EditorValue = value;
	}
#endif
}