using UnityEngine;

[System.Serializable]
public class SerializableNumberBlock
{
	public int x;
	public int y;
	public int value;
}

public class MapData : ScriptableObject
{
    public SerializableNumberBlock[] blocks;
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
				blocks[i * height + j] = new()
				{
					x = i,
					y = j,
					value = values[i, j] != null ? values[i, j].Value : 0
				};
			}
		}
	}
}