using System.Linq;
using UnityEngine;

[System.Serializable]
public class SerializableNumberBlock
{
	public int x;
	public int y;
	public int value;
	public string blockName;
	public int[] values;

	public SerializableNumberBlock(int x, int y, int value)
	{
		this.x = x;
		this.y = y;
		this.value = value;
	}

	public SerializableNumberBlock(DraggableCompositeBlock draggableCompositeBlock)
	{
		blockName = draggableCompositeBlock.blockName;
		values = draggableCompositeBlock.children.Select(i => i.TryGetComponent<StageBlockSetter>(out var setter) ? setter.value : i.Value).ToArray();
	}
}

public class MapData : ScriptableObject
{
	public SerializableNumberBlock[] blocks;
	public SerializableNumberBlock[] upcomingBlocks;

	public int width;
	public int height;

	public void SetBlocks(NumberBlock[,] values)
	{
		width = values.GetLength(0);
		height = values.GetLength(1);
		blocks = new SerializableNumberBlock[width * height];
		for (var i = 0; i < values.GetLength(0); i++)
		{
			for (var j = 0; j < values.GetLength(1); j++)
			{
				blocks[i * height + j] = new(i, j, values[i, j] != null ? values[i, j].Value : 0);
			}
		}
	}

	public void SetUpcomingBlocks(DraggableCompositeBlock[] values)
	{
		if (values == null || values.Length == 0)
			return;
		
		upcomingBlocks = values.Select(i => new SerializableNumberBlock(i)).ToArray();
	}
}