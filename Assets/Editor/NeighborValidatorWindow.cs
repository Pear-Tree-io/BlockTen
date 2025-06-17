// Assets/Editor/NeighborValidatorWindow.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NeighborValidatorWindow : EditorWindow
{
    private DraggableCompositeBlock compositeRoot;
    private float cellSize = 1f;
    private const float tolerance = 0.01f;

    [MenuItem("Tools/Neighbor Validator")]
    public static void ShowWindow()
        => GetWindow<NeighborValidatorWindow>("Neighbor Validator");

    void OnGUI()
    {
        GUILayout.Label("Validate NumberBlock Neighbors", EditorStyles.boldLabel);
        compositeRoot = (DraggableCompositeBlock)EditorGUILayout.ObjectField(
            "Composite Root", compositeRoot, typeof(DraggableCompositeBlock), true);
        cellSize = EditorGUILayout.FloatField("Grid Cell Size", cellSize);

        if (compositeRoot != null && GUILayout.Button("Validate Neighbors"))
        {
            Validate(compositeRoot);
        }
    }

    private void Validate(DraggableCompositeBlock comp)
    {
        var blocks = comp.GetComponentsInChildren<NumberBlock>();
        int errorCount = 0;

        foreach (var b in blocks)
        {
            errorCount += CheckDir(b, b.neighborLeft, Vector2Int.left);
            errorCount += CheckDir(b, b.neighborRight, Vector2Int.right);
            errorCount += CheckDir(b, b.neighborUp, Vector2Int.up);
            errorCount += CheckDir(b, b.neighborDown, Vector2Int.down);
        }

        if (errorCount == 0)
            Debug.Log($"✔ All {blocks.Length} blocks have correct neighbor links.", comp);
        else
            Debug.LogError($"❌ Found {errorCount} neighbor-link errors in {blocks.Length} blocks.", comp);
    }

    private int CheckDir(NumberBlock b, NumberBlock n, Vector2Int dir)
    {
        if (n == null) return 0; // no neighbor assigned = OK

        Vector3 delta = n.transform.localPosition - b.transform.localPosition;
        Vector3 expected = new Vector3(dir.x * cellSize, dir.y * cellSize, 0f);

        if (Mathf.Abs(delta.x - expected.x) > tolerance ||
            Mathf.Abs(delta.y - expected.y) > tolerance)
        {
            Debug.LogError(
                $"{b.name}: neighbor{dir} → {n.name}, but actual offset = {delta:F3}, expected = {expected:F3}",
                b);
            return 1;
        }
        return 0;
    }
}
#endif
